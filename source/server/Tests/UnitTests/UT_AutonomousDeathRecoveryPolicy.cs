using DOL.GS;
using NUnit.Framework;

namespace DOL.UnitTests
{
    [TestFixture]
    public class UT_AutonomousDeathRecoveryPolicy
    {
        [Test]
        public void YellowFallbackCanActuallyBePulledAfterGreenWasUnavailable()
        {
            var maximum = AutonomousDeathRecoveryPolicy.EncounterMaximum(
                ConColor.GREEN, ConColor.YELLOW, ConColor.YELLOW, false);
            Assert.That(AutonomousDeathRecoveryPolicy.IsEligible(ConColor.YELLOW, maximum), Is.True);
            Assert.That(AutonomousDeathRecoveryPolicy.IsEligible(ConColor.ORANGE, maximum), Is.False);
            Assert.That(AutonomousDeathRecoveryPolicy.IsEligible(ConColor.GREY, maximum), Is.False);
        }

        [Test]
        public void ANewOrdinaryCampRetainsItsLowerCeiling()
        {
            var maximum = AutonomousDeathRecoveryPolicy.EncounterMaximum(ConColor.GREEN, ConColor.YELLOW, null, false);
            Assert.That(maximum, Is.EqualTo(ConColor.GREEN));
            Assert.That(AutonomousDeathRecoveryPolicy.IsEligible(ConColor.YELLOW, maximum), Is.False);
        }

        [Test]
        public void BlueFallbackDoesNotPermitYellowAndDoesNotChangeDynamicGroupPolicy()
        {
            Assert.That(AutonomousDeathRecoveryPolicy.EncounterMaximum(ConColor.GREEN, ConColor.YELLOW,
                ConColor.BLUE, false), Is.EqualTo(ConColor.BLUE));
            Assert.That(AutonomousDeathRecoveryPolicy.EncounterMaximum(ConColor.ORANGE, ConColor.PURPLE,
                ConColor.YELLOW, true), Is.EqualTo(ConColor.ORANGE));
        }

        [Test]
        public void FallbackCannotExceedNaturalCeilingOrChooseGrey()
        {
            Assert.That(AutonomousDeathRecoveryPolicy.EncounterMaximum(ConColor.GREEN, ConColor.YELLOW,
                ConColor.PURPLE, false), Is.EqualTo(ConColor.YELLOW));
            Assert.That(AutonomousDeathRecoveryPolicy.EncounterMaximum(ConColor.GREEN, ConColor.YELLOW,
                ConColor.GREY, false), Is.EqualTo(ConColor.GREEN));
        }
    }
}
