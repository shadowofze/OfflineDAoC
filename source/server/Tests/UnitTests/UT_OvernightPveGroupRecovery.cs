using DOL.GS;
using NUnit.Framework;
using Member = DOL.GS.AutonomousGroupRecoveryState.Member;

namespace DOL.GS.Tests;

[TestFixture]
public sealed class UT_OvernightPveGroupRecovery
{
    [Test]
    public void MatchmakingSkipsActiveSoloTravelCombatAndLongCommutes()
    {
        Assert.That(AutonomousBotGroupCoordinator.CanRecruitPveMember(false, false, false, false, false, 30_000), Is.True);
        Assert.That(AutonomousBotGroupCoordinator.CanRecruitPveMember(false, false, false, false, false, 30_001), Is.False);
        Assert.That(AutonomousBotGroupCoordinator.CanRecruitPveMember(true, false, false, false, false, 0), Is.False);
        Assert.That(AutonomousBotGroupCoordinator.CanRecruitPveMember(false, true, false, false, false, 0), Is.False);
        Assert.That(AutonomousBotGroupCoordinator.CanRecruitPveMember(false, false, true, false, false, 0), Is.False);
        Assert.That(AutonomousBotGroupCoordinator.CanRecruitPveMember(false, false, false, true, false, 0), Is.False);
        Assert.That(AutonomousBotGroupCoordinator.CanRecruitPveMember(false, false, false, false, true, 0), Is.False);
    }

    [Test]
    public void ReducedPartyCanFightOnlyWithItsLockedViableCore()
    {
        Assert.That(AutonomousBotGroupCoordinator.CanUsePveCombatRoster(8, 8, 2, 2, 3), Is.True);
        Assert.That(AutonomousBotGroupCoordinator.CanUsePveCombatRoster(7, 7, 1, 1, 3), Is.True);
        Assert.That(AutonomousBotGroupCoordinator.CanUsePveCombatRoster(8, 7, 1, 1, 3), Is.False);
        Assert.That(AutonomousBotGroupCoordinator.CanUsePveCombatRoster(7, 7, 0, 1, 3), Is.False);
        Assert.That(AutonomousBotGroupCoordinator.CanUsePveCombatRoster(3, 3, 1, 1, 1), Is.True);
        Assert.That(AutonomousBotGroupCoordinator.CanUsePveCombatRoster(2, 2, 1, 1, 0), Is.False);
    }

    [Test]
    public void NearbyResurrectionResumesTheExistingCampWithoutARegroupEpisode()
    {
        var recovery = new AutonomousGroupRecoveryState();
        Member[] members = [new(1, 0, true), new(2, 0, true), new(3, 0, true)];
        recovery.Observe(members, false);
        members[1] = members[1] with { Alive = false, Deaths = 1 };
        members[1] = members[1] with { Alive = true };
        Assert.That(recovery.TryResumeLocally(members, together: true), Is.True);
        Assert.That(recovery.Observe(members, taskStarted: true), Is.False);
        Assert.That(recovery.IsRegrouping, Is.False);
    }

    [Test]
    public void DistantOrReleasedCasualtyStillStartsRegroup()
    {
        var recovery = new AutonomousGroupRecoveryState();
        Member[] members = [new(1, 0, true), new(2, 0, true), new(3, 0, true)];
        recovery.Observe(members, false);
        members[1] = members[1] with { Deaths = 1, Returning = true };
        Assert.That(recovery.TryResumeLocally(members, together: true), Is.False);
        Assert.That(recovery.Observe(members, taskStarted: true), Is.True);
        Assert.That(recovery.IsRegrouping, Is.True);
    }

    [Test]
    public void AnotherCasualtyDuringRegroupReturnsToRegroupPhase()
    {
        Assert.That(AutonomousBotGroupCoordinator.PhaseAfterCasualty("Regrouping", true),
            Is.EqualTo("Regrouping"));
        Assert.That(AutonomousBotGroupCoordinator.PhaseAfterCasualty("Grinding", false),
            Is.EqualTo("Grinding"));
    }
}
