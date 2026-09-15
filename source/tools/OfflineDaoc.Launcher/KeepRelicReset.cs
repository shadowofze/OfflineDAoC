using System.Data.SQLite;
using System.Text.Json;

namespace OfflineDaoc.Launcher;

public static class KeepRelicReset
{
    public const string ResetKey = "KeepRelicResetUtc";
    public sealed record Result(int Keeps, int Relics, string Backup, DateTime UpdatedUtc);

    public static Result Apply(string database, string backupDirectory, Func<bool> serverStopped)
    {
        void RequireStopped()
        {
            if (!serverStopped()) throw new InvalidOperationException("Stop the server completely before resetting keeps and relics.");
        }
        RequireStopped();
        if (!File.Exists(database)) throw new FileNotFoundException("World database is missing.", database);
        using var connection = new SQLiteConnection(new SQLiteConnectionStringBuilder
            { DataSource = database, Pooling = false, FailIfMissing = true, DefaultTimeout = 5 }.ConnectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction(System.Data.IsolationLevel.Serializable);
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        List<Dictionary<string, object?>> Read(string sql)
        {
            command.CommandText = sql;
            using var reader = command.ExecuteReader();
            var rows = new List<Dictionary<string, object?>>();
            while (reader.Read())
            {
                var row = new Dictionary<string, object?>();
                for (int i = 0; i < reader.FieldCount; i++) row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                rows.Add(row);
            }
            return rows;
        }
        var keeps = Read("SELECT * FROM [Keep]");
        var relics = Read("SELECT * FROM Relic");
        var pads = Read("SELECT Region,X,Y,Z,Heading,Emblem FROM WorldObject WHERE ClassType='DOL.GS.GameRelicPad'");
        int Number(Dictionary<string, object?> row, string key) => Convert.ToInt32(row[key]);
        if (keeps.Count == 0 || keeps.Any(k => Number(k, "OriginalRealm") is < 0 or > 3))
            throw new InvalidOperationException("Keep defaults are missing or invalid. Nothing was reset.");
        // Match the same emblem/type rules used by GameRelicPad, not today's captured location.
        if (relics.Count != 6 || relics.GroupBy(r => (Number(r, "OriginalRealm"), Number(r, "relicType"))).Count() != 6 ||
            relics.Any(r => Number(r, "OriginalRealm") is < 1 or > 3 || Number(r, "relicType") is < 0 or > 1))
            throw new InvalidOperationException("Expected the six classic realm relics. Nothing was reset.");
        foreach (var relic in relics)
        {
            int emblem = Number(relic, "OriginalRealm") + 10 * Number(relic, "relicType");
            var homes = pads.Where(p => Number(p, "Emblem") == emblem).ToArray();
            if (homes.Length != 1 || Number(homes[0], "Region") <= 0 || Number(homes[0], "X") <= 0 || Number(homes[0], "Y") <= 0)
                throw new InvalidOperationException("A relic home shrine is missing or ambiguous. Nothing was reset.");
        }
        RequireStopped();
        DateTime now = DateTime.UtcNow;
        Directory.CreateDirectory(backupDirectory);
        string backup = Path.Combine(backupDirectory, $"keeps-relics-{now:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.json");
        // A small, exact row backup, never a copy of accounts or the whole installation.
        File.WriteAllText(backup, JsonSerializer.Serialize(new { UpdatedUtc = now, Keeps = keeps, Relics = relics }, new JsonSerializerOptions { WriteIndented = true }));
        command.CommandText = "UPDATE [Keep] SET Realm=OriginalRealm, ClaimedGuildName=''";
        int count = command.ExecuteNonQuery();
        foreach (var relic in relics)
        {
            var home = pads.Single(p => Number(p, "Emblem") == Number(relic, "OriginalRealm") + 10 * Number(relic, "relicType"));
            command.CommandText = "UPDATE Relic SET Realm=OriginalRealm,LastRealm=OriginalRealm,Region=@region,X=@x,Y=@y,Z=@z,Heading=@heading WHERE RelicID=@id";
            command.Parameters.Clear();
            foreach (string column in new[] { "Region", "X", "Y", "Z", "Heading" }) command.Parameters.AddWithValue("@" + column.ToLowerInvariant(), home[column]);
            command.Parameters.AddWithValue("@id", relic["RelicID"]);
            if (command.ExecuteNonQuery() != 1) throw new InvalidOperationException("A relic changed during reset. Reset cancelled.");
        }
        command.Parameters.Clear();
        command.CommandText = "CREATE TABLE IF NOT EXISTS offline_local_options (Key TEXT PRIMARY KEY,Value TEXT NOT NULL)";
        command.ExecuteNonQuery();
        command.CommandText = "INSERT INTO offline_local_options(Key,Value) VALUES (@key,@value) ON CONFLICT(Key) DO UPDATE SET Value=excluded.Value";
        command.Parameters.AddWithValue("@key", ResetKey);
        command.Parameters.AddWithValue("@value", now.ToString("O"));
        command.ExecuteNonQuery();
        RequireStopped();
        transaction.Commit();
        return new Result(count, relics.Count, backup, now);
    }

    public static DateTime ReadResetUtc(string database)
    {
        if (!File.Exists(database)) return DateTime.MinValue;
        using var connection = new SQLiteConnection(new SQLiteConnectionStringBuilder
            { DataSource = database, ReadOnly = true, Pooling = false, FailIfMissing = true, DefaultTimeout = 5 }.ConnectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT count(*) FROM sqlite_master WHERE type='table' AND name='offline_local_options'";
        if (Convert.ToInt32(command.ExecuteScalar()) == 0) return DateTime.MinValue;
        command.CommandText = "SELECT Value FROM offline_local_options WHERE Key=@key";
        command.Parameters.AddWithValue("@key", ResetKey);
        return DateTime.TryParse(command.ExecuteScalar() as string, null, System.Globalization.DateTimeStyles.RoundtripKind, out var value) ? value.ToUniversalTime() : DateTime.MinValue;
    }
}
