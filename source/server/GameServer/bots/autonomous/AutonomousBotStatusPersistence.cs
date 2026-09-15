using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DOL.Database;
using DOL.Logging;

namespace DOL.GS;

/// <summary>
/// Coalesces launcher/status persistence away from NPC think threads. Hundreds
/// of bots can change their in-memory activity every second, but SQLite sees a
/// compact batch instead of one synchronous transaction per AI turn.
/// </summary>
public static class AutonomousBotStatusPersistence
{
    private const int WriteBatchSize = 32;
    private static readonly Logger Log = LoggerManager.Create(typeof(AutonomousBotStatusPersistence));
    private static readonly object PendingGate = new();
    private static readonly Dictionary<long, GameBot> Pending = new();
    private static readonly Queue<long> PendingOrder = new();
    private static readonly HashSet<long> PendingPriority = new();
    private static readonly Queue<long> PendingPriorityOrder = new();
    private static readonly HashSet<long> PendingInventory = new();
    private static readonly Queue<long> PendingInventoryOrder = new();
    private static readonly Timer FlushTimer = new(_ => Flush(), null, 2_000, 2_000);
    private static int _flushing;

    public static object DatabaseWriteLock { get; } = new();

    public static void Queue(GameBot bot, bool includeInventory = false)
    {
        if (bot?.IsAutonomousWorldBot == true && bot.DatabaseID > 0 && bot.PersistentRecord != null)
        {
            lock (PendingGate)
            {
                if (!Pending.ContainsKey(bot.DatabaseID))
                    PendingOrder.Enqueue(bot.DatabaseID);
                Pending[bot.DatabaseID] = bot;
                if (includeInventory && PendingInventory.Add(bot.DatabaseID))
                    PendingInventoryOrder.Enqueue(bot.DatabaseID);
            }
        }
    }

    /// <summary>
    /// Group membership is shown as one launcher card and must not sit behind
    /// thousands of routine activity updates. Priority affects only status-row
    /// ordering; it does not bypass the same bounded batch or database lock.
    /// </summary>
    public static void QueueGroupMetadata(GameBot bot)
    {
        if (bot?.IsAutonomousWorldBot != true || bot.DatabaseID <= 0 || bot.PersistentRecord == null)
            return;
        lock (PendingGate)
        {
            if (!Pending.ContainsKey(bot.DatabaseID))
                PendingOrder.Enqueue(bot.DatabaseID);
            Pending[bot.DatabaseID] = bot;
            if (PendingPriority.Add(bot.DatabaseID))
                PendingPriorityOrder.Enqueue(bot.DatabaseID);
        }
    }

    private static int PendingCount
    {
        get { lock (PendingGate) return Pending.Count; }
    }

    public static int Flush()
    {
        if (Interlocked.Exchange(ref _flushing, 1) != 0)
            return 0;

        var drained = new Dictionary<long, GameBot>(WriteBatchSize);
        var inventoryIds = new HashSet<long>();
        var priorityIds = new HashSet<long>();
        try
        {
            lock (PendingGate)
            {
                // Inventory/equipment changes are durable first. Stale ids are
                // harmless: they remain in the order queue only until dequeued.
                while (drained.Count < WriteBatchSize && PendingInventoryOrder.Count > 0)
                {
                    long botId = PendingInventoryOrder.Dequeue();
                    PendingInventory.Remove(botId);
                    if (Pending.Remove(botId, out GameBot bot))
                    {
                        drained[botId] = bot;
                        inventoryIds.Add(botId);
                    }
                }


                while (drained.Count < WriteBatchSize && PendingPriorityOrder.Count > 0)
                {
                    long botId = PendingPriorityOrder.Dequeue();
                    PendingPriority.Remove(botId);
                    if (Pending.Remove(botId, out GameBot bot))
                    {
                        drained[botId] = bot;
                        priorityIds.Add(botId);
                        if (PendingInventory.Remove(botId))
                            inventoryIds.Add(botId);
                    }
                }

                while (drained.Count < WriteBatchSize && PendingOrder.Count > 0)
                {
                    long botId = PendingOrder.Dequeue();
                    if (!Pending.Remove(botId, out GameBot bot))
                        continue;
                    drained[botId] = bot;
                    PendingPriority.Remove(botId);
                    if (PendingInventory.Remove(botId))
                        inventoryIds.Add(botId);
                }
            }

            GameBot[] bots = drained.Values.ToArray();
            if (bots.Length == 0)
                return 0;

            (GameBot Bot, OfflineWorldBotRecord Record)[] snapshots = bots
                .Where(bot => bot?.PersistentRecord?.IsPersisted == true)
                .Select(bot => (bot, bot.PrepareAutonomousStateSnapshot()))
                .Where(entry => entry.Item2 != null)
                .ToArray();
            if (snapshots.Length == 0)
                return 0;

            int savedCount = 0;
            foreach ((GameBot Bot, OfflineWorldBotRecord Record)[] batch in snapshots.Chunk(WriteBatchSize))
            {
                bool saved;
                lock (DatabaseWriteLock)
                {
                    saved = GameServer.Database.SaveObject(batch.Select(entry => (DataObject)entry.Record));
                    if (saved)
                    {
                        saved = BotInventory.SaveManyIntoDatabase(batch
                            .Where(entry => inventoryIds.Contains(entry.Bot.DatabaseID) && entry.Bot.Inventory is BotInventory)
                            .Select(entry => ((BotInventory)entry.Bot.Inventory, entry.Bot.InternalID)));
                    }
                }

                if (!saved)
                {
                    foreach ((GameBot bot, _) in batch)
                    {
                        Queue(bot, inventoryIds.Contains(bot.DatabaseID));
                        if (priorityIds.Contains(bot.DatabaseID))
                            QueueGroupMetadata(bot);
                    }
                    continue;
                }

                foreach ((GameBot bot, _) in batch)
                    bot.MarkAutonomousStateSaved();
                savedCount += batch.Length;
            }

            return savedCount;
        }
        catch (Exception exception)
        {
            foreach ((long botId, GameBot bot) in drained)
            {
                Queue(bot, inventoryIds.Contains(botId));
                if (priorityIds.Contains(botId))
                    QueueGroupMetadata(bot);
            }
            Log.Error("Autonomous status batch persistence failed", exception);
            return 0;
        }
        finally
        {
            Volatile.Write(ref _flushing, 0);
        }
    }

    /// <summary>
    /// Server shutdown is the one place where durability outranks pacing. Wait
    /// for an in-flight timer flush and drain every remaining batch before the
    /// database is closed. Normal runtime flushes remain bounded to one batch.
    /// </summary>
    public static int FlushAll()
    {
        int saved = 0;
        int stalledAttempts = 0;
        while (Volatile.Read(ref _flushing) != 0 || PendingCount > 0)
        {
            if (Volatile.Read(ref _flushing) != 0)
            {
                Thread.Sleep(10);
                continue;
            }

            int before = PendingCount;
            saved += Flush();
            int after = PendingCount;
            if (after >= before && after > 0)
            {
                if (++stalledAttempts >= 3)
                {
                    Log.Error($"Autonomous shutdown persistence stopped after three failed batches; {after} bots remain queued.");
                    break;
                }
                Thread.Sleep(50);
            }
            else
            {
                stalledAttempts = 0;
            }
        }

        return saved;
    }
}
