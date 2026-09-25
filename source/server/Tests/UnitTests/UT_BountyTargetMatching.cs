using DOL.GS;
using NUnit.Framework;

namespace DOL.UnitTests;

[TestFixture]
public sealed class UT_BountyTargetMatching
{
    [Test]
    public void OrdinaryBountyMatchesSpeciesAcrossAssignedLevelVariants()
    {
        // A Vigilant Rock strapper-vine camp has levels 28-33. Its assigned
        // level selects the bounty, but must not narrow which vines count.
        foreach (byte assignedLevel in new byte[] { 28, 29, 30, 31, 32, 33 })
        {
            var bounty = new BountyTargetCandidate
            {
                Name = "strapper vine",
                Level = assignedLevel,
                RegionId = 181,
                ZoneId = 186
            };

            Assert.That(bounty.MatchesOrdinaryMonster("Strapper Vine", eRealm.None, 181, 186),
                Is.True, $"Assigned level {assignedLevel} must not limit same-name kills.");
        }
    }

    [Test]
    public void OrdinaryBountyStillRejectsOtherNamesZonesRegionsAndRealmNpcs()
    {
        var bounty = new BountyTargetCandidate
        {
            Name = "strapper vine",
            Level = 31,
            RegionId = 181,
            ZoneId = 186
        };

        Assert.Multiple(() =>
        {
            Assert.That(bounty.MatchesOrdinaryMonster("strangler vine", eRealm.None, 181, 186), Is.False);
            Assert.That(bounty.MatchesOrdinaryMonster("strapper vine", eRealm.None, 181, 185), Is.False);
            Assert.That(bounty.MatchesOrdinaryMonster("strapper vine", eRealm.None, 180, 186), Is.False);
            Assert.That(bounty.MatchesOrdinaryMonster("strapper vine", eRealm.Hibernia, 181, 186), Is.False);
            Assert.That(bounty.MatchesOrdinaryMonster("strapper vine", eRealm.None, 181, null), Is.False);
        });
    }

    [Test]
    public void EpicBountiesRemainExactSpawnOnly()
    {
        var bounty = new BountyTargetCandidate
        {
            Name = "King Tuscar",
            RegionId = 160,
            ZoneId = 160,
            IsEpic = true,
            RepresentativeMobId = "boss-id"
        };

        Assert.That(bounty.MatchesOrdinaryMonster("King Tuscar", eRealm.None, 160, 160), Is.False);
    }
}
