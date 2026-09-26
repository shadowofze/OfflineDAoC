using System;
using DOL.Database;

namespace DOL.GS;

/// <summary>
/// Pure policy seams for Darkness Falls. Live entrance authority remains in
/// DFEnterJumpPoint; these helpers make its keep-control and local-PvP rules
/// independently testable.
/// </summary>
public static class AutonomousDarknessFallsPolicy
{
    public const ushort RegionId = 249;

    public static ushort HomeRegion(eRealm realm) => realm switch
    {
        eRealm.Albion => 1,
        eRealm.Midgard => 100,
        eRealm.Hibernia => 200,
        _ => 0,
    };

    // Each physical DF exit has a DB row for every realm. Matching the row's
    // Realm and TargetRegion alone would still let a bot leave via another
    // faction's corridor. These are the exits below each home entrance.
    public static ushort HomeExitZonePointId(eRealm realm) => realm switch
    {
        eRealm.Albion => 74,
        eRealm.Midgard => 70,
        eRealm.Hibernia => 72,
        _ => 0,
    };

    public static bool CanEnter(
        eRealm realm,
        eRealm currentOwner,
        eRealm previousOwner,
        long nowTick,
        long lastSwapTick,
        long gracePeriod,
        bool allowAllRealms,
        bool normalServer)
    {
        if (!normalServer || allowAllRealms)
            return true;
        if (realm == eRealm.None)
            return false;
        if (realm == previousOwner && lastSwapTick + Math.Max(0, gracePeriod) >= nowTick)
            return true;
        return realm == currentOwner;
    }

    public static bool CanUseRegionEdge(eRealm realm, ushort sourceRegion, ushort targetRegion, Func<eRealm, bool> canEnter)
    {
        if (sourceRegion == RegionId && targetRegion != RegionId)
            return HomeRegion(realm) != 0 && targetRegion == HomeRegion(realm);
        if (targetRegion != RegionId || sourceRegion == RegionId)
            return true;
        // Autonomous travel enters through its own realm's home-side tunnel.
        // Relic-keep portal rows for other realms are valid player content,
        // but must not become a shortcut to another realm's DF wing for bots.
        ushort homeRegion = HomeRegion(realm);
        return homeRegion != 0 && sourceRegion == homeRegion && canEnter?.Invoke(realm) == true;
    }

    /// <summary>DF has realm-specific DB rows at all six physical exits. A bot
    /// may use only the physical portal below its own entrance, its own row,
    /// and its own home destination. Deep shared exits remain closed to bots
    /// until their physical approach is independently certified.</summary>
    public static bool CanUsePortalRow(eRealm realm, DbZonePoint point) =>
        point != null && (point.SourceRegion != RegionId || point.TargetRegion == RegionId ||
            HomeRegion(realm) != 0 && point.TargetRegion == HomeRegion(realm) &&
            point.Realm == (ushort)realm && point.Id == HomeExitZonePointId(realm));

    /// <summary>An already-inside bot may still select its own physical exit
    /// after ordinary DF goals are closed by a stale or absent certificate.
    /// This never admits a new entrance or another faction's exit.</summary>
    public static bool CanEvacuateThroughHomeExit(eRealm realm, ushort currentRegion, DbZonePoint point) =>
        currentRegion == RegionId && point?.SourceRegion == RegionId &&
        point.TargetRegion != RegionId && CanUsePortalRow(realm, point);

    public static bool MustRetireOrdinaryGoal(ushort goalRegion, bool ordinaryCatalogReady) =>
        goalRegion == RegionId && !ordinaryCatalogReady;

    public static bool CanEngageLocalOpponent(
        eRealm attackerRealm,
        eRealm targetRealm,
        ushort attackerRegion,
        ushort targetRegion,
        bool targetAlive,
        bool allowedByServerRules) =>
        attackerRegion == RegionId && targetRegion == RegionId && targetAlive && allowedByServerRules &&
        attackerRealm != eRealm.None && targetRealm != eRealm.None && attackerRealm != targetRealm;
}
