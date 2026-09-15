using System.Collections.Generic;
using NUnit.Framework;

namespace DOL.GS.Tests;

[TestFixture]
public sealed class UT_AutonomousStableRoutePlanner
{
    [Test]
    public void ChoosesStableInsteadOfLongDirectWalk()
    {
        List<AutonomousStableRoutePlanner.LegMetric> legs =
        [
            new(0, 100, 0, 9000, 0, 10, 0)
        ];

        AutonomousStableRoutePlanner.RouteDecision? route =
            AutonomousStableRoutePlanner.ChooseFirstLeg(0, 0, 10000, 0, 100, 0, legs);

        Assert.That(route, Is.Not.Null);
        Assert.That(route!.Value.FirstLegIndex, Is.EqualTo(0));
        Assert.That(route.Value.EstimatedSeconds, Is.LessThan(route.Value.DirectWalkSeconds));
    }

    [Test]
    public void ChainsMultipleTicketLegsWhenThatIsFastest()
    {
        List<AutonomousStableRoutePlanner.LegMetric> legs =
        [
            new(0, 0, 0, 4000, 0, 5, 0),
            new(1, 4000, 0, 9000, 0, 5, 0)
        ];

        AutonomousStableRoutePlanner.RouteDecision? route =
            AutonomousStableRoutePlanner.ChooseFirstLeg(0, 0, 10000, 0, 100, 0, legs);

        Assert.That(route, Is.Not.Null);
        Assert.That(route!.Value.FirstLegIndex, Is.EqualTo(0));
        Assert.That(route.Value.HopCount, Is.EqualTo(2));
    }

    [Test]
    public void IgnoresPaidTicketThatBotCannotAfford()
    {
        List<AutonomousStableRoutePlanner.LegMetric> legs =
        [
            new(0, 0, 0, 9500, 0, 1, 50),
            new(1, 0, 0, 7000, 0, 5, 0)
        ];

        AutonomousStableRoutePlanner.RouteDecision? route =
            AutonomousStableRoutePlanner.ChooseFirstLeg(0, 0, 10000, 0, 100, 10, legs);

        Assert.That(route, Is.Not.Null);
        Assert.That(route!.Value.FirstLegIndex, Is.EqualTo(1));
        Assert.That(route.Value.PlannedPrice, Is.Zero);
    }

    [Test]
    public void NeverPlansAChainWhoseCombinedTicketsExceedFunds()
    {
        List<AutonomousStableRoutePlanner.LegMetric> legs =
        [
            new(0, 0, 0, 4000, 0, 5, 3),
            new(1, 4000, 0, 9000, 0, 5, 3)
        ];

        AutonomousStableRoutePlanner.RouteDecision? route =
            AutonomousStableRoutePlanner.ChooseFirstLeg(0, 0, 10000, 0, 100, 5, legs);

        Assert.That(route, Is.Not.Null);
        Assert.That(route!.Value.HopCount, Is.EqualTo(1));
        Assert.That(route.Value.PlannedPrice, Is.LessThanOrEqualTo(5));
    }

    [Test]
    public void AcceptsAnyMeaningfullyFasterTicketWithoutFifteenPercentThreshold()
    {
        List<AutonomousStableRoutePlanner.LegMetric> legs =
        [
            new(0, 0, 0, 9000, 0, 89, 0)
        ];

        AutonomousStableRoutePlanner.RouteDecision? route =
            AutonomousStableRoutePlanner.ChooseFirstLeg(0, 0, 10000, 0, 100, 0, legs);

        Assert.That(route, Is.Not.Null);
        Assert.That(route!.Value.EstimatedSeconds, Is.EqualTo(99).Within(0.001));
        Assert.That(route.Value.DirectWalkSeconds, Is.EqualTo(100).Within(0.001));
    }

    [Test]
    public void StableRideCannotDismountOnTransientMovementFlagLoss()
    {
        Assert.Multiple(() =>
        {
            Assert.That(AutonomousStableRouteLifecycle.ShouldComplete(false, false, false, false), Is.False);
            Assert.That(AutonomousStableRouteLifecycle.ShouldComplete(true, false, true, false), Is.False);
            Assert.That(AutonomousStableRouteLifecycle.ShouldComplete(false, true, false, true), Is.False);
            Assert.That(AutonomousStableRouteLifecycle.ShouldComplete(false, false, false, true), Is.True);
        });
    }

    [Test]
    public void DisconnectedSameZoneStableCannotBecomeTheFirstBoardingLeg()
    {
        Assert.Multiple(() =>
        {
            Assert.That(AutonomousStableRoutePlanner.CanUseAsFirstBoardingLeg(true, false), Is.False);
            Assert.That(AutonomousStableRoutePlanner.CanUseAsFirstBoardingLeg(true, true), Is.True);
            Assert.That(AutonomousStableRoutePlanner.CanUseAsFirstBoardingLeg(false, false), Is.True);
        });
    }
}
