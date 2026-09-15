using System;
using System.Linq;
using NUnit.Framework;

namespace DOL.GS.Tests;

[TestFixture]
public class UT_AutonomousDarknessFallsPolicy
{
    [Test]
    public void EntranceAuthority_UsesOwnerAndFifteenMinutePreviousOwnerGrace()
    {
        const long grace = 900_000;
        Assert.Multiple(() =>
        {
            Assert.That(AutonomousDarknessFallsPolicy.CanEnter(eRealm.Albion, eRealm.Albion, eRealm.Midgard,
                1_000_000, 500_000, grace, false, true), Is.True);
            Assert.That(AutonomousDarknessFallsPolicy.CanEnter(eRealm.Midgard, eRealm.Albion, eRealm.Midgard,
                1_000_000, 500_000, grace, false, true), Is.True);
            Assert.That(AutonomousDarknessFallsPolicy.CanEnter(eRealm.Midgard, eRealm.Albion, eRealm.Midgard,
                1_400_001, 500_000, grace, false, true), Is.False);
            Assert.That(AutonomousDarknessFallsPolicy.CanEnter(eRealm.Hibernia, eRealm.Albion, eRealm.Midgard,
                1_000_000, 500_000, grace, false, true), Is.False);
            Assert.That(AutonomousDarknessFallsPolicy.CanEnter(eRealm.Hibernia, eRealm.None, eRealm.None,
                1_000_000, 0, grace, true, true), Is.True);
        });
    }

    [Test]
    public void RegionEdges_BlockClosedEntryButNeverTrapCharactersAlreadyInside()
    {
        Assert.Multiple(() =>
        {
            Assert.That(AutonomousDarknessFallsPolicy.CanUseRegionEdge(eRealm.Albion, 1, 249, _ => false), Is.False);
            Assert.That(AutonomousDarknessFallsPolicy.CanUseRegionEdge(eRealm.Albion, 249, 1, _ => false), Is.True);
            Assert.That(AutonomousDarknessFallsPolicy.CanUseRegionEdge(eRealm.Albion, 1, 249, _ => true), Is.True);
        });
    }

    [Test]
    public void LocalPvp_RequiresTwoLiveOpposingRealmsInsideDarknessFalls()
    {
        Assert.Multiple(() =>
        {
            Assert.That(AutonomousDarknessFallsPolicy.CanEngageLocalOpponent(eRealm.Albion, eRealm.Midgard, 249, 249, true, true), Is.True);
            Assert.That(AutonomousDarknessFallsPolicy.CanEngageLocalOpponent(eRealm.Albion, eRealm.Albion, 249, 249, true, true), Is.False);
            Assert.That(AutonomousDarknessFallsPolicy.CanEngageLocalOpponent(eRealm.Albion, eRealm.Midgard, 249, 1, true, true), Is.False);
            Assert.That(AutonomousDarknessFallsPolicy.CanEngageLocalOpponent(eRealm.Albion, eRealm.Midgard, 249, 249, false, true), Is.False);
        });
    }

    [Test]
    public void PveContracts_AreTimeOnlyEvenForLegacyKillRecords()
    {
        DateTime now = DateTime.UtcNow;
        var group = new OfflineWorldBotRecord
        {
            ObjectiveKind = "GroupPve",
            ObjectiveAssignmentId = "group-1",
            ObjectivePveMode = "Time",
            ObjectiveExpiresUtc = now.AddMinutes(45).ToString("O"),
        };
        var soloKills = new OfflineWorldBotRecord
        {
            ObjectiveKind = "SoloPve",
            ObjectiveAssignmentId = "solo-1",
            ObjectivePveMode = "Kills",
            ObjectivePveKillTarget = 20,
            ObjectivePveKills = 19,
            ObjectiveExpiresUtc = now.AddMinutes(90).ToString("O"),
        };

        Assert.Multiple(() =>
        {
            Assert.That(AutonomousObjectiveAssignments.HasActivePveAssignment(group, now), Is.True);
            Assert.That(AutonomousObjectiveAssignments.HasActivePveAssignment(group, now.AddMinutes(46)), Is.False);
            Assert.That(AutonomousObjectiveAssignments.HasActivePveAssignment(soloKills, now), Is.True);
            Assert.That(AutonomousObjectiveAssignments.HasActivePveAssignment(soloKills, now.AddMinutes(91)), Is.False);
            soloKills.ObjectivePveKills = 20;
            Assert.That(AutonomousObjectiveAssignments.HasActivePveAssignment(soloKills, now), Is.True);
            Assert.That(Enumerable.Range(0, 200).Select(seed => AutonomousObjectiveAssignments.RollPveTenure(new Random(seed))),
                Has.All.InRange(AutonomousObjectiveAssignments.MinimumPveTenure, AutonomousObjectiveAssignments.MaximumPveTenure));
            Assert.That(AutonomousObjectiveAssignments.MinimumPveTenure, Is.EqualTo(TimeSpan.FromMinutes(45)));
            Assert.That(AutonomousObjectiveAssignments.MaximumPveTenure, Is.EqualTo(TimeSpan.FromMinutes(120)));
            Assert.That(Enumerable.Range(0, 200).Select(seed => AutonomousObjectiveAssignments.RollSoloPveCompletionMode(new Random(seed))).Distinct().Count(),
                Is.EqualTo(1));
            Assert.That(AutonomousObjectiveAssignments.RollSoloPveCompletionMode(), Is.EqualTo(eAutonomousPveCompletionMode.Time));
        });
    }
}
