using DOL.GS;
using DOL.GS.Styles;
using DOL.Database;
using NUnit.Framework;
using System.Numerics;

namespace DOL.UnitTests.Gameserver;

[TestFixture]
public class UT_SavageBotCombat
{
    [Test]
    public void InstantSavageBuffContinuesMeleeButARealCastDoesNot()
    {
        var instant = new Spell(new DbSpell
        {
            Type = nameof(eSpellType.SavageDPSBuff),
            Target = "Self",
            CastTime = 0,
        }, 1);
        var castTime = new Spell(new DbSpell
        {
            Type = nameof(eSpellType.SavageDPSBuff),
            Target = "Self",
            CastTime = 2,
        }, 1);

        Assert.Multiple(() =>
        {
            Assert.That(SavageBotCombatPolicy.MayAttackDuringActiveCast(eCharacterClass.Savage, instant), Is.True,
                "The early combat cast guard must permit the native instant buff to reach melee");
            Assert.That(SavageBotCombatPolicy.MayAttackDuringActiveCast(eCharacterClass.Savage, castTime), Is.False);
            Assert.That(SavageBotCombatPolicy.MayAttackDuringActiveCast(eCharacterClass.Berserker, instant), Is.False);
            Assert.That(SavageBotCombatPolicy.ContinueMeleeAfterSpell(
                eCharacterClass.Savage, true, false, null, true, instant), Is.True,
                "A queued instant health-cost buff must not cancel the pull or swing");
            Assert.That(SavageBotCombatPolicy.ContinueMeleeAfterSpell(
                eCharacterClass.Savage, true, true, instant, false, null), Is.True,
                "The casting service's brief active-handler phase is still instant");
            Assert.That(SavageBotCombatPolicy.ContinueMeleeAfterSpell(
                eCharacterClass.Savage, true, true, castTime, false, null), Is.False);
            Assert.That(SavageBotCombatPolicy.ContinueMeleeAfterSpell(
                eCharacterClass.Savage, true, false, null, true, null), Is.False,
                "An unknown queued ability must retain the safe generic behavior");
            Assert.That(SavageBotCombatPolicy.ContinueMeleeAfterSpell(
                eCharacterClass.Berserker, true, false, null, true, instant), Is.False);
        });
    }

    [Test]
    public void SavageUsesOffensiveStaplesBeforeDefensiveHealthTrades()
    {
        Assert.That(SavageBotCombatPolicy.BuffPriority(eSpellType.SavageDPSBuff), Is.EqualTo(0));
        Assert.That(SavageBotCombatPolicy.BuffPriority(eSpellType.SavageCombatSpeedBuff), Is.EqualTo(1));
        Assert.That(SavageBotCombatPolicy.ShouldUseBuff(eSpellType.SavageDPSBuff, 70, 0), Is.True);
        Assert.That(SavageBotCombatPolicy.ShouldUseBuff(eSpellType.SavageEvadeBuff, 70, 1), Is.False);
        Assert.That(SavageBotCombatPolicy.ShouldUseBuff(eSpellType.SavageEvadeBuff, 90, 2), Is.True);
        Assert.That(SavageBotCombatPolicy.ShouldUseBuff(eSpellType.SavageDPSBuff, 50, 0), Is.False);
    }

    [Test]
    public void SavageTradesHealthForEnduranceOnlyWithASafeReserve()
    {
        Assert.Multiple(() =>
        {
            Assert.That(SavageBotCombatPolicy.ShouldUseEnduranceHeal(90, 20), Is.True);
            Assert.That(SavageBotCombatPolicy.ShouldUseEnduranceHeal(75, 30), Is.True);
            Assert.That(SavageBotCombatPolicy.ShouldUseEnduranceHeal(74, 20), Is.False,
                "The bot must not compound a dangerous health deficit");
            Assert.That(SavageBotCombatPolicy.ShouldUseEnduranceHeal(90, 31), Is.False,
                "Ordinary rest recovery remains preferable when endurance is not critically low");
        });
    }

    [Test]
    public void PersistedWeaponLineDoesNotRerollOnRestart()
    {
        Assert.That(SavageBotSpec.WeaponFromPersistedSpecs(
            $"{Specs.Savagery}|12;{Specs.Sword}|9;{Specs.Parry}|2"), Is.EqualTo(eObjectType.Sword));
        Assert.That(SavageBotSpec.WeaponFromPersistedSpecs(
            $"{Specs.Axe}|3;{Specs.HandToHand}|11"), Is.EqualTo(eObjectType.HandToHand));
    }

    [Test]
    public void SavageStylePolicyPrefersRealDamageAndRecognizesAnytimeFallbacks()
    {
        var utility = new Style(new DbStyle { OpeningRequirementType = 0, OpeningRequirementValue = 0,
            AttackResultRequirement = 0, GrowthRate = 0, SpecLevelRequirement = 6 }, null);
        var damaging = new Style(new DbStyle { OpeningRequirementType = 0, OpeningRequirementValue = 0,
            AttackResultRequirement = 0, GrowthRate = 0.57, SpecLevelRequirement = 2 }, null);
        var positional = new Style(new DbStyle { OpeningRequirementType = 2, OpeningRequirementValue = 0,
            AttackResultRequirement = 0, GrowthRate = 1.1, SpecLevelRequirement = 8 }, null);

        Assert.Multiple(() =>
        {
            Assert.That(SavageBotCombatPolicy.StylePriority(damaging), Is.GreaterThan(SavageBotCombatPolicy.StylePriority(utility)));
            Assert.That(SavageBotCombatPolicy.IsReliableAnytimeStyle(damaging), Is.True);
            Assert.That(SavageBotCombatPolicy.IsReliableAnytimeStyle(positional), Is.False);
            Assert.That(SavageBotCombatPolicy.NeedsWeaponTrainingRepair(5, 9), Is.True);
            Assert.That(SavageBotCombatPolicy.NeedsWeaponTrainingRepair(9, 7), Is.False);
        });
    }

    [Test]
    public void SavageNeverSelectsTheAutomaticRangedCombatPath()
    {
        Assert.Multiple(() =>
        {
            Assert.That(BotRangedCombat.IsDedicatedArcher(eCharacterClass.Savage), Is.False);
            Assert.That(BotRangedCombat.ShouldUseRangedWeapon(eCharacterClass.Savage,
                eBotStance.Auto, false, true), Is.False);
            Assert.That(BotRangedCombat.AllowsAutomaticNpcRangedSwitch(
                true, eCharacterClass.Savage, true), Is.False);
        });
    }

    [Test]
    public void SoloSavageOnlyPullsReachableTargetsInsideTheAssignedCampCell()
    {
        Assert.Multiple(() =>
        {
            Assert.That(SavageBotCombatPolicy.NeedsVerifiedSoloPullRoute(eCharacterClass.Savage, false), Is.True);
            Assert.That(SavageBotCombatPolicy.NeedsVerifiedSoloPullRoute(eCharacterClass.Savage, true), Is.False);
            Assert.That(SavageBotCombatPolicy.NeedsVerifiedSoloPullRoute(eCharacterClass.Berserker, false), Is.False);
            Assert.That(SavageBotCombatPolicy.IsWithinAssignedCamp(554470, 562177, 557070, 562177), Is.True);
            Assert.That(SavageBotCombatPolicy.IsWithinAssignedCamp(554470, 562177, 557071, 562177), Is.False);
        });
    }

    [Test]
    public void FailedSoloPullRouteIsCachedOnlyBrieflyWhileBothActorsStayPut()
    {
        Vector3 origin = new(554470, 562177, 5100);
        Vector3 target = new(555000, 562600, 5100);
        var failed = new SavageBotCombatPolicy.FailedSoloPullRoute(
            1_000, 100, 5, 5, origin, target);

        bool Delayed(long tick, ushort region, int originZone, int targetZone,
            Vector3 from, Vector3 to) =>
            SavageBotCombatPolicy.ShouldDelayFailedSoloPullRetry(
                failed, tick, region, originZone, targetZone, from, to);

        Assert.Multiple(() =>
        {
            Assert.That(Delayed(1_000, 100, 5, 5, origin, target), Is.True);
            Assert.That(Delayed(6_999, 100, 5, 5, origin, target), Is.True);
            Assert.That(Delayed(7_000, 100, 5, 5, origin, target), Is.False,
                "A stationary unreachable NPC gets a fresh route check after six seconds");
            Assert.That(Delayed(999, 100, 5, 5, origin, target), Is.False,
                "A clock reset cannot preserve stale failures");
            Assert.That(Delayed(2_000, 100, 5, 5, origin + new Vector3(500, 0, 0), target), Is.True,
                "An ordinary short camp patrol should not re-probe the same failed path each scan");
            Assert.That(Delayed(2_000, 100, 5, 5, origin + new Vector3(513, 0, 0), target), Is.False);
            Assert.That(Delayed(2_000, 100, 5, 5, origin, target + new Vector3(65, 0, 0)), Is.False);
            Assert.That(Delayed(2_000, 101, 5, 5, origin, target), Is.False);
            Assert.That(Delayed(2_000, 100, 6, 5, origin, target), Is.False);
            Assert.That(Delayed(2_000, 100, 5, 6, origin, target), Is.False);
        });
    }

    [Test]
    public void OnlyDanglingGameBotGeneratedUniqueRelationsAreReplaceable()
    {
        var broken = new DbInventoryItem
        {
            Creator = nameof(GameBot),
            UTemplate_Id = "missing_gamebot_unique",
            ITemplate_Id = null,
        };
        var valid = new DbInventoryItem
        {
            Creator = nameof(GameBot),
            UTemplate_Id = "valid_gamebot_unique",
            Template = new DbItemUnique { Id_nb = "valid_gamebot_unique" },
        };
        var loot = new DbInventoryItem
        {
            Creator = "loot",
            UTemplate_Id = "missing_loot_unique",
        };

        Assert.Multiple(() =>
        {
            Assert.That(BotWeaponStats.IsBrokenGeneratedFallback(broken), Is.True);
            Assert.That(BotWeaponStats.IsBrokenGeneratedFallback(valid), Is.False);
            Assert.That(BotWeaponStats.IsBrokenGeneratedFallback(loot), Is.False);
        });
    }
}
