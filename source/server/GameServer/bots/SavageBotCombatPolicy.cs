using DOL.GS.Styles;

namespace DOL.GS;

/// <summary>
/// Small, deterministic policy for the Classic Savage's instant, health-cost
/// self buffs. It does not alter the native effects, values, durations, costs,
/// styles, abilities, or player controls.
/// </summary>
public static class SavageBotCombatPolicy
{
    public static bool IsInstantCombatAction(Spell spell) => spell != null && spell.CastTime == 0 &&
        spell.SpellType is eSpellType.SavageEnduranceHeal or
            eSpellType.SavageEvadeBuff or eSpellType.SavageParryBuff or
            eSpellType.SavageCombatSpeedBuff or eSpellType.SavageDPSBuff or
            eSpellType.SavageSlashResistanceBuff or eSpellType.SavageCrushResistanceBuff or
            eSpellType.SavageThrustResistanceBuff;

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
        if (isCasting && !IsInstantCombatAction(activeSpell))
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
