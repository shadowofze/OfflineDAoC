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
    public void OrdinaryBountyAcceptsSameNamedOutdoorMonsterAcrossHomeRealmZones()
    {
        var bounty = new BountyTargetCandidate
        {
            Name = "large frog",
            Level = 31,
            RegionId = 200,
            ZoneId = 207
        };

        Assert.Multiple(() =>
        {
            // Connacht and Lough Derg are different zones in the same region.
            Assert.That(bounty.MatchesOrdinaryMonster("Large Frog", eRealm.None, 200, 207), Is.True);
            Assert.That(bounty.MatchesOrdinaryMonster("large frog", eRealm.None, 200, 200), Is.True);
            // Classic and SI Hibernia also share one home-realm hunt pool.
            Assert.That(bounty.MatchesOrdinaryMonster("large frog", eRealm.None, 220, 220), Is.True);
        });
    }

    [Test]
    public void OrdinaryBountyStillRejectsOtherNamesRealmsDungeonMobsAndMissingZones()
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
            Assert.That(bounty.MatchesOrdinaryMonster("strapper vine", eRealm.None, 1, 186), Is.False);
            Assert.That(bounty.MatchesOrdinaryMonster("strapper vine", eRealm.None, 100, 186), Is.False);
            Assert.That(bounty.MatchesOrdinaryMonster("strapper vine", eRealm.None, 999, 186), Is.False);
            Assert.That(bounty.MatchesOrdinaryMonster("strapper vine", eRealm.Hibernia, 181, 186), Is.False);
            Assert.That(bounty.MatchesOrdinaryMonster("strapper vine", eRealm.None, 181, 185, monsterIsDungeon: true), Is.False);
            Assert.That(bounty.MatchesOrdinaryMonster("strapper vine", eRealm.None, 181, null), Is.False);
        });
    }

    [Test]
    public void DungeonBountyRemainsBoundToAssignedDungeonZone()
    {
        var bounty = new BountyTargetCandidate
        {
            Name = "cave spider",
            RegionId = 221,
            ZoneId = 223,
            IsDungeon = true
        };

        Assert.Multiple(() =>
        {
            Assert.That(bounty.MatchesOrdinaryMonster("cave spider", eRealm.None, 221, 223, true), Is.True);
            Assert.That(bounty.MatchesOrdinaryMonster("cave spider", eRealm.None, 221, 224, true), Is.False);
            Assert.That(bounty.MatchesOrdinaryMonster("cave spider", eRealm.None, 220, 223, true), Is.False);
            Assert.That(bounty.MatchesOrdinaryMonster("cave spider", eRealm.None, 221, 223, false), Is.False);
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
