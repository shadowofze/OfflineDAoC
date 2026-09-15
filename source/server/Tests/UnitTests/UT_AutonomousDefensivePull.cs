using System.Numerics;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using DOL.AI.Brain;
using DOL.Database;
using DOL.GS;
using NUnit.Framework;

namespace DOL.UnitTests
{
    [NonParallelizable]
    public class UT_AutonomousDefensivePull
    {
        private sealed class Actor : GameBot
        {
            private Actor() : base((OfflineWorldBotRecord)null) { }
            public bool Alive;
            public int PositionX;
            public int Stops, Switches;
            public override bool IsAlive => Alive;
            public override int X => PositionX;
            public override int Y => 0;
            public override int Z => 0;
            public override void StopAttack() => Stops++;
            public override void SwitchWeapon(eActiveWeaponSlot slot) => Switches++;
        }
        private sealed class Enemy : GameNPC
        {
            public bool Alive;
            public int PositionX;
            public override bool IsAlive => Alive;
            public override int X => PositionX;
            public override int Y => 0;
            public override int Z => 0;
        }
        private sealed class Pet : IControlledBrain
        {
            public eWalkState WalkState => eWalkState.Follow;
            public eAggressionState AggressionState { get; set; }
            public GameNPC Body => null;
            public GameLiving Owner => null;
            public bool IsMainPet { get; set; }
            public void Attack(GameObject target) { }
            public void Disengage() { }
            public void CheckAggressionStateOnPlayerOrder() { }
            public void Follow(GameObject target) { }
            public void FollowOwner() { }
            public void Stay() { }
            public void ComeHere() { }
            public void Goto(GameObject target) { }
            public void UpdatePetWindow() { }
            public GamePlayer GetPlayerOwner() => null;
            public GameNPC GetNPCOwner() => null;
            public GameLiving GetLivingOwner() => null;
            public void SetAggressionState(eAggressionState state) => AggressionState = state;
        }

        [TestCase("near")]
        [TestCase("dead-member")]
        [TestCase("departed-member")]
        [TestCase("dead-target")]
        [TestCase("timeout")]
        [TestCase("disband")]
        [TestCase("other-attacker")]
        public void ActualWaitGateReleasesAndRestoresPetsAndWeapon(string reason)
        {
            using var server = new EpicTestServerScope();
            const BindingFlags hidden = BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
            Actor[] members = Enumerable.Range(0, 8).Select(_ =>
            {
                var actor = (Actor)RuntimeHelpers.GetUninitializedObject(typeof(Actor));
                actor.Alive = true;
                typeof(GameNPC).GetField("m_brains", hidden).SetValue(actor, new ArrayList());
                return actor;
            }).ToArray();
            var group = new Group(members[0]);
            foreach (Actor member in members) member.Group = group;
            var enemy = (Enemy)RuntimeHelpers.GetUninitializedObject(typeof(Enemy));
            enemy.Alive = true; enemy.PositionX = 1500; enemy.ObjectState = GameObject.eObjectState.Active;
            Type coordinator = typeof(AutonomousDefensivePull);
            object state = Activator.CreateInstance(coordinator.GetNestedType("State", BindingFlags.NonPublic));
            void Set(string name, object value) => state.GetType().GetField(name).SetValue(state, value);
            Set("Active", true); Set("Members", members); Set("Shooter", members[0]); Set("Target", enemy);
            Set("Until", GameLoop.GameLoopTime + 30_000);
            var pet = new Pet { AggressionState = eAggressionState.Passive };
            ((Dictionary<IControlledBrain, eAggressionState>)state.GetType().GetField("Pets").GetValue(state))
                .Add(pet, eAggressionState.Defensive);
            object states = coordinator.GetField("States", hidden).GetValue(null);
            object holding = coordinator.GetField("Holding", hidden).GetValue(null);
            states.GetType().GetMethod("Add").Invoke(states, new[] { group, state });
            foreach (Actor member in members) holding.GetType().GetMethod("Add").Invoke(holding, new[] { member, state });
            try
            {
                Assert.That(AutonomousDefensivePull.Hold(members[1]), Is.True, "Distant pull does not release followers");
                Assert.That(AutonomousDefensivePull.OwnsRangedPosition(members[0]), Is.True);
                switch (reason)
                {
                    case "near": enemy.PositionX = 399; break;
                    case "dead-member": members[5].Alive = false; break;
                    case "departed-member": members[5].Group = null; break;
                    case "dead-target": enemy.Alive = false; break;
                    case "timeout": Set("Until", GameLoop.GameLoopTime); break;
                    case "disband": AutonomousDefensivePull.Cancel(group); break;
                    case "other-attacker": AutonomousDefensivePull.OnThreat(members[1], members[7]); break;
                }
                Assert.That(AutonomousDefensivePull.Hold(members[1]), Is.False);
                Assert.That(AutonomousDefensivePull.OwnsRangedPosition(members[0]), Is.False);
                Assert.That(pet.AggressionState, Is.EqualTo(eAggressionState.Defensive));
                Assert.That(members[0].Switches, Is.EqualTo(1));
                AutonomousDefensivePull.Cancel(group);
                Assert.That(members[0].Switches, Is.EqualTo(1), "Cleanup is idempotent");
            }
            finally
            {
                AutonomousDefensivePull.Cancel(group);
                states.GetType().GetMethod("Remove", new[] { typeof(Group) }).Invoke(states, new[] { group });
            }
        }

        [TestCase(true, 8, true, true)]
        [TestCase(false, 8, true, false)]
        [TestCase(true, 7, true, false)]
        [TestCase(true, 8, false, false)]
        public void OnlyFullLevelFiftyPvePartiesChange(bool pve, int size, bool fifty, bool expected) =>
            Assert.That(AutonomousDefensivePull.UsesDefensivePull(pve, size, fifty), Is.EqualTo(expected));

        [TestCase("DirectDamage", 0, 1500, 50, true)]
        [TestCase("Lifedrain", 0, 1500, 50, true)]
        [TestCase("DamageOverTime", 0, 1500, 50, true)]
        [TestCase("DirectDamage", 350, 1500, 50, false)]
        [TestCase("DirectDamage", 0, 500, 50, false)]
        [TestCase("DirectDamage", 0, 1500, 51, false)]
        [TestCase("PetSpell", 0, 1500, 50, false)]
        [TestCase("Summon", 0, 1500, 50, false)]
        public void PullUsesALearnedLongRangeSingleTargetDamageAbility(string type, int radius, int range, int level, bool expected)
        {
            var spell = new Spell(new DbSpell { Type = type, Target = "Enemy", Damage = 100, Radius = radius, Range = range }, level);
            Assert.That(AutonomousDefensivePull.IsPullSpell(spell, 50), Is.EqualTo(expected));
        }

        [TestCase(1000, 0)]
        [TestCase(1600, 200)]
        [TestCase(1800, 400)]
        public void ShooterUsesOnlyShortConnectedApproachAndHasReturnRoute(int targetDistance, int advance)
        {
            var nav = new UT_RealmRaidFormation.Mesh();
            Vector3 origin = new(10000, 10000, 1000);
            Assert.That(AutonomousDefensivePull.TryFiringPoint(nav, UT_RealmRaidFormation.Zone(), origin,
                origin + new Vector3(targetDistance, 0, 0), 1500, out Vector3 point), Is.True);
            Assert.That(point.X - origin.X, Is.EqualTo(advance).Within(1));
        }

        [TestCase("wall")]
        [TestCase("island")]
        [TestCase("floor")]
        [TestCase("too-far")]
        public void UnsafeApproachNeverFallsBackToRunningIntoPack(string failure)
        {
            var nav = new UT_RealmRaidFormation.Mesh { Visible = failure != "wall", Connected = failure != "island",
                HeightOffset = failure == "floor" ? 500 : 0 };
            Vector3 origin = new(10000, 10000, 1000);
            Assert.That(AutonomousDefensivePull.TryFiringPoint(nav, UT_RealmRaidFormation.Zone(), origin,
                origin + new Vector3(failure == "too-far" ? 2000 : 1600, 0, 0), 1500, out _), Is.False);
        }
    }
}
