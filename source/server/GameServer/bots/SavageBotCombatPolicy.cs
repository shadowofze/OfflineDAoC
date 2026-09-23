using DOL.GS.Styles;
using System.Numerics;

namespace DOL.GS;

/// <summary>
/// Small, deterministic policy for the Classic Savage's instant, health-cost
/// self buffs. It does not alter the native effects, values, durations, costs,
/// styles, abilities, or player controls.
/// </summary>
public static class SavageBotCombatPolicy
{
    public const int AssignedCampRadius = 2600;
    public const int FailedSoloPullRouteRetryMilliseconds = 6_000;
    // A normal short patrol step must not force the same failed native path
    // probe on the next brain tick. A materially new approach still retries.
    private const int FailedSoloPullOriginMovement = 512;
    private const int FailedSoloPullTargetMovement = 64;

    public readonly record struct FailedSoloPullRoute(long FailedAtTick, ushort RegionId,
        int OriginZoneId, int TargetZoneId, Vector3 Origin, Vector3 Target);

    // Only cache a failed native corridor. A reachable target is always
    // rechecked before a new pull, and movement or a zone change retries a
    // failed path immediately instead of trusting an old negative answer.
    public static bool ShouldDelayFailedSoloPullRetry(FailedSoloPullRoute failure,
        long nowTick, ushort regionId, int originZoneId, int targetZoneId,
        Vector3 origin, Vector3 target) =>
        nowTick >= failure.FailedAtTick &&
        nowTick - failure.FailedAtTick < FailedSoloPullRouteRetryMilliseconds &&
        failure.RegionId == regionId &&
        failure.OriginZoneId == originZoneId &&
        failure.TargetZoneId == targetZoneId &&
        Vector3.DistanceSquared(failure.Origin, origin) <=
            FailedSoloPullOriginMovement * FailedSoloPullOriginMovement &&
        Vector3.DistanceSquared(failure.Target, target) <=
            FailedSoloPullTargetMovement * FailedSoloPullTargetMovement;

    public static bool NeedsVerifiedSoloPullRoute(eCharacterClass characterClass, bool dynamicGroup) =>
        characterClass == eCharacterClass.Savage && !dynamicGroup;

    public static bool IsWithinAssignedCamp(int campX, int campY, int targetX, int targetY)
    {
        long dx = (long)campX - targetX;
        long dy = (long)campY - targetY;
        return dx * dx + dy * dy <= (long)AssignedCampRadius * AssignedCampRadius;
    }

    public static bool IsInstantCombatAction(Spell spell) => spell != null && spell.CastTime == 0 &&
        spell.SpellType is eSpellType.SavageEnduranceHeal or
            eSpellType.SavageEvadeBuff or eSpellType.SavageParryBuff or
            eSpellType.SavageCombatSpeedBuff or eSpellType.SavageDPSBuff or
            eSpellType.SavageSlashResistanceBuff or eSpellType.SavageCrushResistanceBuff or
            eSpellType.SavageThrustResistanceBuff;

    public static bool MayAttackDuringActiveCast(eCharacterClass characterClass, Spell activeSpell) =>
        characterClass == eCharacterClass.Savage && IsInstantCombatAction(activeSpell);

    /// <summary>
    /// Savage self-buffs are native instant, health-cost combat actions.  The
    /// asynchronous casting queue briefly reports them as pending/active even
    /// though they must not cancel a weapon swing or melee approach.  Unknown
    /// requests and real cast-time spells retain the normal stop-attack rule.
    /// </summary>
    public static bool ContinueMeleeAfterSpell(eCharacterClass characterClass, bool spellAction,
        bool isCasting, Spell activeSpell, bool hasPendingCast, Spell pendingSpell)
    {
        if (!spellAction || characterClass != eCharacterClass.Savage)
            return false;
        if (isCasting && !MayAttackDuringActiveCast(characterClass, activeSpell))
            return false;
        if (hasPendingCast && !IsInstantCombatAction(pendingSpell))
            return false;
        return true;
    }

    public static int BuffPriority(eSpellType type) => type switch
    {
        eSpellType.SavageDPSBuff => 0,
        eSpellType.SavageCombatSpeedBuff => 1,
        eSpellType.SavageEvadeBuff => 2,
        eSpellType.SavageParryBuff => 3,
        eSpellType.SavageCrushResistanceBuff => 4,
        eSpellType.SavageSlashResistanceBuff => 5,
        eSpellType.SavageThrustResistanceBuff => 6,
        _ => 100,
    };

    // Native Savages can trade health for endurance. Use it only when the
    // endurance shortage is material and there is a wide health reserve; the
    // spell handler retains the authored level-scaled value and health cost.
    public static bool ShouldUseEnduranceHeal(int healthPercent, int endurancePercent) =>
        healthPercent >= 75 && endurancePercent <= 30;

    // A controlled player can choose exactly how much health to trade. Bots use
    // the two offensive staples, then defensive buffs only with a healthy reserve.
    public static bool ShouldUseBuff(eSpellType type, int healthPercent, int activeBuffs)
    {
        int limit = healthPercent >= 85 ? 4 : 2;
        if (healthPercent < 55 || activeBuffs >= limit)
            return false;

        return BuffPriority(type) < limit;
    }

    public static bool SameBuffFamily(Spell requested, Spell active) =>
        requested != null && active != null && requested.SpellType == active.SpellType;

    public static bool IsReliableAnytimeStyle(Style style) => style != null &&
        style.OpeningRequirementType == Style.eOpening.Offensive &&
        style.OpeningRequirementValue == 0 &&
        style.AttackResultRequirement == Style.eAttackResultRequirement.Any;

    // Savage lines contain low- or zero-growth utility styles above stronger
    // anytime attacks. "Highest level" alone repeatedly selected those weak
    // attacks. Native growth, endurance, procs and hit resolution remain intact.
    public static double StylePriority(Style style) => style?.GrowthRate ?? double.MinValue;

    public static bool NeedsWeaponTrainingRepair(int weaponLevel, int savageryLevel) =>
        weaponLevel < savageryLevel;
}
