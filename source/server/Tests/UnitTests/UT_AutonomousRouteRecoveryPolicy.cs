using NUnit.Framework;

namespace DOL.GS.Tests;

[TestFixture]
public sealed class UT_AutonomousRouteRecoveryPolicy
{
    [Test]
    public void SideStepAloneDoesNotResetTheRecoveryBudget()
    {
        Assert.That(AutonomousRouteRecoveryPolicy.HasMeaningfulForwardProgress(5_000, 4_900), Is.False);
        Assert.That(AutonomousRouteRecoveryPolicy.HasMeaningfulForwardProgress(5_000, 4_760), Is.True);
    }

    [Test]
    public void ExactlyThreeLocalRecoveriesAreAllowedBeforeGoalSwap()
    {
        Assert.That(AutonomousRouteRecoveryPolicy.ShouldAbandon(1), Is.False);
        Assert.That(AutonomousRouteRecoveryPolicy.ShouldAbandon(3), Is.False);
        Assert.That(AutonomousRouteRecoveryPolicy.ShouldAbandon(4), Is.True);
    }

    [Test]
    public void ContinuousMovementKeepsOwnershipOfTheExistingOrder()
    {
        Assert.That(AutonomousRouteRecoveryPolicy.ShouldRetainMovementOrder(true, 50_000, 0), Is.True);
        Assert.That(AutonomousRouteRecoveryPolicy.ShouldRetainMovementOrder(false, 1_000, 1_500), Is.True);
        Assert.That(AutonomousRouteRecoveryPolicy.ShouldRetainMovementOrder(false, 1_500, 1_500), Is.False);
        Assert.That(AutonomousRouteRecoveryPolicy.ShouldRecoverActiveRouteStall(1_000, 6_000, 5_000), Is.True,
            "a collision-stopped route must enter bounded recovery even when IsMoving is false");
        Assert.That(AutonomousRouteRecoveryPolicy.ShouldRecoverActiveRouteStall(1_000, 5_999, 5_000), Is.False);
    }

    [Test]
    public void ImmediateRouteFailureGetsAQuietCooldownBeforeAnotherGoal()
    {
        Assert.Multiple(() =>
        {
            Assert.That(AutonomousRouteRecoveryPolicy.ReplanDelayMilliseconds(false, 17),
                Is.EqualTo(AutonomousRouteRecoveryPolicy.ImmediateRouteFailureCooldownMilliseconds + 17));
            Assert.That(AutonomousRouteRecoveryPolicy.ReplanDelayMilliseconds(true, 17), Is.EqualTo(2_517));
        });
    }

    [Test]
    public void SafeRelocationRequiresRepeatedTerminalFailuresInTheSamePocket()
    {
        var origin = new System.Numerics.Vector3(1000, 1000, 100);
        Assert.Multiple(() =>
        {
            Assert.That(AutonomousRouteRecoveryPolicy.IsSameRepeatedFailurePocket(
                1, 1, origin, new(1200, 1100, 900), 30_000), Is.True,
                "Z differences do not hide an isolated XY navigation component");
            Assert.That(AutonomousRouteRecoveryPolicy.IsSameRepeatedFailurePocket(
                1, 1, origin, new(1500, 1000, 100), 30_000), Is.False);
            Assert.That(AutonomousRouteRecoveryPolicy.IsSameRepeatedFailurePocket(
                1, 200, origin, origin, 30_000), Is.False);
            Assert.That(AutonomousRouteRecoveryPolicy.IsSameRepeatedFailurePocket(
                1, 1, origin, origin, AutonomousRouteRecoveryPolicy.RepeatedFailureWindowMilliseconds + 1), Is.False);
            Assert.That(AutonomousRouteRecoveryPolicy.FailuresBeforeSafeRelocation, Is.EqualTo(3));
        });
    }
}
