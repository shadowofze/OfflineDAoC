using System;
using System.Collections.Generic;
using System.Linq;
using DOL.Database;

namespace DOL.GS
{
    /// <summary>A repeatable bounty targets a home-realm species, or one named epic spawn.</summary>
    public sealed class BountyTargetCandidate
    {
        public string Name { get; init; }
        public byte Level { get; init; }
        public ushort RegionId { get; init; }
        public ushort ZoneId { get; init; }
        public string ZoneName { get; init; }
        public int X { get; init; }
        public int Y { get; init; }
        public int Z { get; init; }
        public string RepresentativeMobId { get; init; }
        public int SpawnCount { get; init; }
        public bool IsDungeon { get; init; }
        public bool IsEpic { get; init; }

        public bool Matches(GameNPC npc)
        {
            if (npc == null)
                return false;

            if (IsEpic)
                return npc.Realm == eRealm.None && npc.CurrentRegionID == RegionId &&
                       string.Equals(npc.InternalID, RepresentativeMobId, StringComparison.Ordinal);

            return MatchesOrdinaryMonster(npc.Name, npc.Realm, npc.CurrentRegionID,
                npc.CurrentZone?.ID, npc.CurrentZone?.IsDungeon == true);
        }

        /// <summary>
        /// A normal outdoor bounty is for a named monster species anywhere in
        /// the player's home realm, not just the representative camp or level.
        /// Dungeon contracts stay within their assigned dungeon zone so a
        /// same-named outdoor or different-dungeon mob cannot replace the hunt.
        /// </summary>
        public bool MatchesOrdinaryMonster(string monsterName, eRealm monsterRealm,
            ushort monsterRegionId, ushort? monsterZoneId, bool monsterIsDungeon = false) =>
            !IsEpic && monsterRealm == eRealm.None &&
            monsterZoneId.HasValue &&
            string.Equals(monsterName, Name, StringComparison.OrdinalIgnoreCase) &&
            (IsDungeon
                ? monsterIsDungeon && monsterRegionId == RegionId && monsterZoneId == ZoneId
                : !monsterIsDungeon && BountyTargetCatalog.AreRegionsInSameHomeRealm(RegionId, monsterRegionId));

        /// <summary>Checks whether a matching spawn is currently alive in the world.</summary>
        public bool IsCurrentlySpawned() => WorldMgr.GetNPCsByNameFromRegion(Name, RegionId, eRealm.None)
            .Any(npc => npc.IsAlive && Matches(npc));
    }

    public static class BountyTargetCatalog
    {
        // Classic and Shrouded Isles home regions only. These are zone/region IDs,
        // not the NPC Realm field (ordinary hostile monsters have Realm.None).
        private static readonly ushort[] AlbionRegions = { 1, 20, 21, 22, 23, 24, 50, 51, 60, 61 };
        private static readonly ushort[] MidgardRegions = { 100, 125, 126, 127, 128, 129, 150, 151, 160, 161 };
        private static readonly ushort[] HiberniaRegions = { 180, 181, 190, 191, 200, 220, 221, 222, 223, 224 };

        // Match only within one faction's known Classic/SI home regions. The
        // monster's Realm is normally None, so that field cannot distinguish
        // Albion, Midgard, and Hibernia hostile spawn catalogs by itself.
        internal static bool AreRegionsInSameHomeRealm(ushort assignedRegion, ushort killedRegion) =>
            (AlbionRegions.Contains(assignedRegion) && AlbionRegions.Contains(killedRegion)) ||
            (MidgardRegions.Contains(assignedRegion) && MidgardRegions.Contains(killedRegion)) ||
            (HiberniaRegions.Contains(assignedRegion) && HiberniaRegions.Contains(killedRegion));

        private static readonly (eRealm Realm, ushort Region, string Name, string MobId)[] EpicTargets =
        {
            (eRealm.Albion, 1, "Golestandt", "400d6dd2-f619-4d23-85be-08161f0f57eb"),
            (eRealm.Albion, 60, "Crypt Lord", "99996536"),
            (eRealm.Albion, 60, "Warlord Dorinakka", "99996564"),
            (eRealm.Midgard, 100, "Gjalpinulva", "100005133"),
            (eRealm.Midgard, 160, "King Tuscar", "100022285"),
            (eRealm.Midgard, 160, "Queen Kula", "100022302"),
            (eRealm.Hibernia, 200, "Cuuldurach the Glimmer King", "100030567"),
            (eRealm.Hibernia, 191, "Olcasgean", "33a5deb2-89f8-4309-a111-9b057f7b6db2"),
            (eRealm.Hibernia, 191, "Xaga", "100029184")
        };

        private static readonly object CacheLock = new();
        private static readonly Dictionary<eRealm, IReadOnlyList<BountyTargetCandidate>> CachedSpawns = new();

        public static IReadOnlyList<BountyTargetCandidate> GetEligible(eRealm realm, byte level)
        {
            if (level is < 1 or > 49)
                return Array.Empty<BountyTargetCandidate>();

            var candidates = GetCachedSpawns(realm);
            int preferredSpawns = level >= 40 ? 5 : 3;
            var exact = candidates.Where(target => target.Level == level).ToArray();
            var pool = exact.Where(target => target.SpawnCount >= preferredSpawns).ToArray();
            if (pool.Length == 0)
                pool = exact.Where(target => target.SpawnCount >= 2).ToArray();
            if (pool.Length == 0)
                pool = candidates.Where(target => target.SpawnCount >= 2 &&
                    ConLevels.GetConLevel(level, target.Level) == (int)ConColor.YELLOW).ToArray();
            if (pool.Length == 0)
                pool = candidates.Where(target =>
                    ConLevels.GetConLevel(level, target.Level) == (int)ConColor.YELLOW).ToArray();

            // Prefer camps with a real, currently alive example at the requested
            // level. The database pool remains available when every matching camp
            // is briefly dead and waiting for its normal respawn.
            var liveKeys = GetRegions(realm)
                .SelectMany(WorldMgr.GetNPCsFromRegion)
                .Where(npc => npc != null && npc.GetType() == typeof(GameNPC) &&
                              npc.IsAlive && npc.Realm == eRealm.None &&
                              npc.CurrentZone != null)
                .Select(npc => (npc.CurrentRegionID, npc.CurrentZone.ID, npc.Level, npc.Name))
                .ToHashSet();
            var currentlySpawned = pool.Where(target => liveKeys.Contains(
                (target.RegionId, target.ZoneId, target.Level, target.Name))).ToArray();
            return currentlySpawned.Length > 0 ? currentlySpawned : pool;
        }

        public static IReadOnlyList<BountyTargetCandidate> GetEpicCandidates(eRealm realm)
        {
            var result = new List<BountyTargetCandidate>();
            foreach (var target in EpicTargets.Where(target => target.Realm == realm))
            {
                DbMob mob = GameServer.Database.FindObjectByKey<DbMob>(target.MobId);
                Zone zone = WorldMgr.GetRegion(target.Region)?.GetZone(mob?.X ?? 0, mob?.Y ?? 0);
                if (mob != null && zone != null && mob.Region == target.Region &&
                    mob.Realm == (byte)eRealm.None && mob.RespawnInterval > 0 &&
                    mob.Level >= 50 && string.Equals(mob.Name, target.Name, StringComparison.Ordinal))
                    result.Add(MakeCandidate(mob, zone, mob.Level, 1, true));
            }
            return result;
        }

        /// <summary>Restores a saved assignment by its database-backed spawn identity.</summary>
        public static BountyTargetCandidate Resolve(eRealm realm, byte assignedLevel, string representativeMobId)
        {
            if (string.IsNullOrWhiteSpace(representativeMobId))
                return null;

            // Resolve against the permanent spawn catalog, never the live-only
            // assignment preference: an existing bounty survives a camp wipe.
            var pool = assignedLevel == 50 ? GetEpicCandidates(realm) : GetCachedSpawns(realm);
            // A ranged template can make one Mob_ID represent several possible
            // levels. The quest saves the exact assigned level/zone/name; return
            // null in that ambiguous case so it restores that saved snapshot.
            var matches = pool.Where(candidate =>
                string.Equals(candidate.RepresentativeMobId, representativeMobId, StringComparison.Ordinal)).ToArray();
            return matches.Length == 1 ? matches[0] : null;
        }

        private static IReadOnlyList<BountyTargetCandidate> GetCachedSpawns(eRealm realm)
        {
            lock (CacheLock)
            {
                if (!CachedSpawns.TryGetValue(realm, out var candidates))
                {
                    candidates = BuildCandidates(realm);
                    CachedSpawns.Add(realm, candidates);
                }
                return candidates;
            }
        }

        private static IReadOnlyList<BountyTargetCandidate> BuildCandidates(eRealm realm)
        {
            var regions = GetRegions(realm).ToHashSet();
            if (regions.Count == 0)
                return Array.Empty<BountyTargetCandidate>();

            var templates = GameServer.Database.SelectAllObjects<DbNpcTemplate>()
                .GroupBy(template => template.TemplateId)
                .ToDictionary(group => group.Key, group => group.ToArray());

            var spawns = new List<(DbMob Mob, Zone Zone, byte Level)>();
            foreach (DbMob mob in GameServer.Database.SelectAllObjects<DbMob>())
            {
                if (!regions.Contains(mob.Region) || mob.ClassType != DbMob.DEFAULT_NPC_CLASSTYPE ||
                    mob.Realm != (byte)eRealm.None || mob.RespawnInterval <= 0 ||
                    string.IsNullOrWhiteSpace(mob.Name) || string.IsNullOrWhiteSpace(mob.ObjectId))
                    continue;

                Zone zone = WorldMgr.GetRegion(mob.Region)?.GetZone(mob.X, mob.Y);
                if (zone == null)
                    continue;

                foreach (byte effectiveLevel in EffectiveLevels(mob, templates))
                {
                    if (effectiveLevel is >= 1 and <= 49)
                        spawns.Add((mob, zone, effectiveLevel));
                }
            }

            return spawns.GroupBy(spawn => (spawn.Mob.Region, spawn.Zone.ID, spawn.Level, spawn.Mob.Name))
                .Select(group =>
                {
                    var representative = group.OrderBy(spawn => spawn.Mob.ObjectId, StringComparer.Ordinal).First();
                    return MakeCandidate(representative.Mob, representative.Zone, representative.Level,
                        group.Count(), false);
                })
                .ToArray();
        }

        private static IEnumerable<byte> EffectiveLevels(DbMob mob,
            Dictionary<int, DbNpcTemplate[]> templates)
        {
            if (!templates.TryGetValue(mob.NPCTemplateID, out var matching))
                return new[] { mob.Level };

            var result = new HashSet<byte>();
            foreach (DbNpcTemplate template in matching)
            {
                if (!template.ReplaceMobValues || string.IsNullOrWhiteSpace(template.Level))
                {
                    result.Add(mob.Level);
                    continue;
                }

                foreach (string value in Util.SplitCSV(template.Level, true))
                {
                    if (byte.TryParse(value, out byte level))
                        result.Add(level);
                }
            }
            return result;
        }

        private static ushort[] GetRegions(eRealm realm) => realm switch
        {
            eRealm.Albion => AlbionRegions,
            eRealm.Midgard => MidgardRegions,
            eRealm.Hibernia => HiberniaRegions,
            _ => Array.Empty<ushort>()
        };

        private static BountyTargetCandidate MakeCandidate(DbMob mob, Zone zone, byte level,
            int spawnCount, bool epic) => new()
        {
            Name = mob.Name,
            Level = level,
            RegionId = mob.Region,
            ZoneId = zone.ID,
            ZoneName = zone.Description,
            X = mob.X,
            Y = mob.Y,
            Z = mob.Z,
            RepresentativeMobId = mob.ObjectId,
            SpawnCount = spawnCount,
            IsDungeon = zone.IsDungeon,
            IsEpic = epic
        };
    }
}
