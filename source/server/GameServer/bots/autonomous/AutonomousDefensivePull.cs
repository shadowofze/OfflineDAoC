using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using DOL.AI.Brain;
using DOL.Logging;

namespace DOL.GS
{
    /// <summary>One bounded, real ranged pull per level-50 PvE party. No new timers or world scans.</summary>
    public static class AutonomousDefensivePull
    {
        public const int ContactRadius = 400;
        public const int TimeoutMilliseconds = 30_000;
        private sealed class State
        {
            public GameBot[] Members;
            public GameBot Shooter;
            public GameLiving Target;
            public Vector3 Anchor, FiringPoint;
            public Spell Spell;
            public eActiveWeaponSlot PreviousWeapon;
            public readonly Dictionary<IControlledBrain, eAggressionState> Pets = new();
            public long Until, RetryAfter, NextMove;
            public bool Shot, Active;
        }
        private static readonly ConditionalWeakTable<Group, State> States = new();
        private static readonly ConditionalWeakTable<GameBot, State> Holding = new();

        public static bool UsesDefensivePull(bool groupPve, int memberCount, bool allLevelFifty) =>
            groupPve && memberCount == 8 && allLevelFifty;

        public static bool IsPullSpell(Spell spell, int level) => spell != null && spell.Level <= level &&
            spell.Target == eSpellTarget.ENEMY && spell.Range >= 1000 && spell.Radius == 0 && spell.Damage > 0 && !spell.NeedInstrument &&
            spell.SpellType is eSpellType.DirectDamage or eSpellType.Lifedrain or eSpellType.DamageOverTime;

        private static Spell PullSpell(GameBot bot) => bot.Spells?.Where(spell => IsPullSpell(spell, bot.Level))
            .OrderByDescending(spell => spell.Range).ThenByDescending(spell => spell.Level).FirstOrDefault();

        public static bool HasRangedPull(GameBot bot) => bot != null &&
            (PullSpell(bot) != null || BotRangedCombat.CanUse(bot, bot.Inventory?.GetItem(eInventorySlot.DistanceWeapon)));

        public static bool OwnsRangedPosition(GameNPC npc) => npc is GameBot bot && bot.Group != null &&
            States.TryGetValue(bot.Group, out State state) && state.Active && state.Shooter == bot;

        // True means this party's pull is handled, including a failed approach.
        // Never fall through into a melee charge merely because a ranged pull failed.
        public static bool TryBegin(GameBot shooter, GameLiving target)
        {
            // Suppress only a new scripted pull of this expedition's dragon.
            // Attacked-by-enemy and shared defense paths are unchanged.
            if (AutonomousRealmRaid.IsPendingDragonTarget(shooter,target)) return true;
            if (!AutonomousBotGroupCoordinator.IsLevelFiftyPveGroup(shooter?.Group)) return false;
            State state = States.GetOrCreateValue(shooter.Group);
            lock (state)
            {
                if (state.Active || GameLoop.GameLoopTime < state.RetryAfter) return true;
                GameBot[] members = shooter.Group.GetMembersInTheGroup().OfType<GameBot>().ToArray();
                if (members.Length != 8 || target?.IsAlive != true || shooter.CurrentRegion != target.CurrentRegion)
                    return true;
                // An enemy already in the party is a real defensive fight, not a ranged pull.
                if (members.Any(member => member.IsWithinRadius(target, ContactRadius))) return false;
                state.RetryAfter = GameLoop.GameLoopTime + TimeoutMilliseconds;
                Spell spell = PullSpell(shooter);
                eActiveWeaponSlot previous = shooter.ActiveWeaponSlot;
                if (spell == null)
                {
                    if (!HasRangedPull(shooter)) return true;
                    shooter.SwitchWeapon(eActiveWeaponSlot.Distance);
                }
                int range = spell?.Range ?? shooter.attackComponent.AttackRange;
                Vector3 origin = new(shooter.X, shooter.Y, shooter.Z), enemy = new(target.X, target.Y, target.Z);
                if (!TryFiringPoint(PathfindingProvider.Instance, shooter.CurrentZone, origin, enemy, range, out Vector3 point))
                {
                    shooter.SwitchWeapon(previous);
                    LoggerManager.Create(typeof(AutonomousDefensivePull)).Info(
                        $"AUTONOMOUS_DEFENSIVE_PULL_UNREACHABLE bot={shooter.Name} target={target.Name} region={shooter.CurrentRegionID} x={shooter.X} y={shooter.Y} z={shooter.Z}");
                    return true;
                }
                state.Members = members;
                state.Shooter = shooter;
                state.Target = target;
                state.Anchor = origin;
                state.FiringPoint = point;
                state.Spell = spell;
                state.PreviousWeapon = previous;
                state.Shot = false;
                state.Until = GameLoop.GameLoopTime + TimeoutMilliseconds;
                state.NextMove = 0;
                state.Active = true;
                foreach (GameBot member in members)
                {
                    member.WakeRecoveryRest();
                    Holding.Remove(member);
                    Holding.Add(member, state);
                    member.StopAttack();
                    member.StopFollowing();
                    member.StopMovingOnPath();
                    member.StopMoving();
                    var visited = new HashSet<IControlledBrain>();
                    foreach (GameNPC pet in BotGroupPetBuffTargets.AttachedTree(member.ControlledBrain, member, visited))
                        if (pet.Brain is IControlledBrain brain && state.Pets.TryAdd(brain, brain.AggressionState))
                        {
                            brain.Disengage();
                            brain.SetAggressionState(eAggressionState.Passive);
                            brain.FollowOwner();
                        }
                }
                return true;
            }
        }

        public static bool TryFiringPoint(IPathfindingMgr nav, Zone zone, Vector3 origin, Vector3 target,
            int range, out Vector3 point)
        {
            point = origin;
            if (nav?.IsAvailable != true || zone == null || range < 1000) return false;
            float distance = Vector3.Distance(origin, target);
            float advance = Math.Max(0, distance - (range - 100));
            // No solo expedition into a pack: at most a short, proven approach from the party.
            if (advance > 450) return false;
            Vector3 desired = advance == 0 ? origin : Vector3.Lerp(origin, target, advance / distance);
            if (!AutonomousNavigationSurface.TryFloor(nav, zone, desired, out point)) return false;
            return Vector3.Distance(origin, point) <= 450 && Vector3.Distance(point, target) <= range - 50 &&
                nav.HasLineOfSight(zone, point, target, nav.DefaultFilters) &&
                AutonomousZoneItinerary.HasCompleteCorridor(nav, zone, origin, point) &&
                AutonomousZoneItinerary.HasCompleteCorridor(nav, zone, point, origin);
        }

        public static bool Hold(GameBot bot)
        {
            if (bot == null || !Holding.TryGetValue(bot, out State state)) return false;
            lock (state)
            {
                if (!state.Active) return false;
                if (bot.Group == null || !state.Target.IsAlive || state.Target.ObjectState != GameObject.eObjectState.Active ||
                    state.Members.Any(member => !member.IsAlive || member.Group != bot.Group ||
                        member.CurrentRegion != state.Shooter.CurrentRegion || member.IsOnStableMasterRoute) ||
                    state.Target.CurrentRegion != bot.CurrentRegion || GameLoop.GameLoopTime >= state.Until)
                {
                    Finish(state, false);
                    return false;
                }
                if (state.Members.Any(member => member.IsWithinRadius(state.Target, ContactRadius)))
                {
                    Finish(state, true);
                    return false;
                }
                if (bot != state.Shooter) return true;
                Vector3 destination = state.Shot ? state.Anchor : state.FiringPoint;
                if (Vector3.DistanceSquared(new(bot.X, bot.Y, bot.Z), destination) > 45 * 45)
                {
                    if (!bot.IsCasting && GameLoop.GameLoopTime >= state.NextMove)
                    {
                        state.NextMove = GameLoop.GameLoopTime + 1500;
                        bot.PathTo(destination, bot.MaxSpeed);
                    }
                    return true;
                }
                if (bot.IsCasting || bot.castingComponent?.HasPendingSkillRequests == true) return true;
                bot.StopMovingOnPath();
                bot.StopMoving();
                if (state.Shot || bot.IsCrowdControlled) return true;
                int range = state.Spell?.Range ?? bot.attackComponent.AttackRange;
                if (!bot.IsWithinRadius(state.Target, range - 50) ||
                    !PathfindingProvider.Instance.HasLineOfSight(bot.CurrentZone, new(bot.X, bot.Y, bot.Z),
                        new(state.Target.X, state.Target.Y, state.Target.Z), PathfindingProvider.Instance.DefaultFilters)) return true;
                bot.TargetObject = state.Target;
                if (state.Spell != null)
                {
                    if (bot.Mana >= bot.PowerCost(state.Spell) && bot.GetSkillDisabledDuration(state.Spell) == 0)
                        bot.CastSpell(state.Spell, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
                }
                else if (!bot.IsAttacking && bot.Endurance >= bot.rangeAttackComponent.ShotEnduranceCost)
                    bot.StartAttack(state.Target);
                return true;
            }
        }

        public static void OnAttack(GameLiving actor, AttackData attack)
        {
            if (actor?.Group == null || !States.TryGetValue(actor.Group, out State state)) return;
            lock (state)
            {
                if (!state.Active || state.Shooter != actor || attack?.Attacker != actor ||
                    attack.Target != state.Target || !attack.CausesCombat) return;
                state.Shot = true;
                state.Shooter.StopAttack();
            }
        }

        public static void OnThreat(GameLiving member, GameLiving attacker)
        {
            if (member?.Group == null || !States.TryGetValue(member.Group, out State state)) return;
            lock (state)
                if (state.Active && (attacker != state.Target || member.IsWithinRadius(attacker, ContactRadius)))
                    Finish(state, attacker == state.Target);
        }

        public static void Cancel(Group group)
        {
            if (group == null || !States.TryGetValue(group, out State state)) return;
            lock (state) if (state.Active) Finish(state, false);
        }

        private static void Finish(State state, bool engage)
        {
            state.Active = false;
            state.RetryAfter = GameLoop.GameLoopTime + 10_000;
            state.Shooter.StopAttack();
            state.Shooter.SwitchWeapon(state.PreviousWeapon);
            foreach (var pet in state.Pets)
                if (pet.Key.AggressionState == eAggressionState.Passive) pet.Key.SetAggressionState(pet.Value);
            state.Pets.Clear();
            foreach (GameBot member in state.Members)
            {
                Holding.Remove(member);
                if (member.Brain is BotBrain brain)
                {
                    if (engage && member.IsAlive && member.Group == state.Shooter.Group &&
                        member.CurrentRegion == state.Target.CurrentRegion)
                    {
                        brain.AddToAggroList(state.Target, Math.Max(25, state.Target.EffectiveLevel * 10));
                        brain.CommitDungeonPull(state.Target);
                        brain.FSM.SetCurrentState(eFSMStateType.AGGRO);
                    }
                    else brain.RemoveFromAggroList(state.Target);
                }
            }
        }
    }
}
