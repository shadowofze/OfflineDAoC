using System;

namespace DOL.GS;

/// <summary>
/// Pure policy seams for Darkness Falls. Live entrance authority remains in
/// DFEnterJumpPoint; these helpers make its keep-control and local-PvP rules
/// independently testable.
/// </summary>
public static class AutonomousDarknessFallsPolicy
{
    public const ushort RegionId = 249;

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
        if (targetRegion != RegionId || sourceRegion == RegionId)
            return true;
        return canEnter?.Invoke(realm) == true;
    }

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
