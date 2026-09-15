using System;
using System.Linq;
using System.Runtime.CompilerServices;
using DOL.AI.Brain;

namespace DOL.GS
{
    /// <summary>Deployment decisions only. Native summons, turret brains and spell costs remain authoritative.</summary>
    public static class BotAnimistPolicy
    {
        private sealed class State
        {
            public long NextRelocation;
            public long NextField;
            public GameLiving PendingTarget;
            public bool PendingRestore;
        }
        private static readonly ConditionalWeakTable<GameBot, State> States = new();
        private static readonly string[] TravelWords = { "travel", "walking", "crossing", "meeting", "returning", "riding" };
        public static bool AppliesTo(GameLiving owner) => owner is GameBot bot &&
            bot.CharacterClass?.ID == (int)eCharacterClass.Animist;

        public static bool ValidEncounter(GameBot bot, GameLiving target) =>
            target != null && target != bot && target.IsAlive && target.ObjectState == GameObject.eObjectState.Active &&
            target.CurrentRegion == bot.CurrentRegion &&
            (target is not GameNPC npc || AutonomousSummonActivity.AutonomousOwner(npc) != bot) &&
            !(target is GameSummonedPet pet && pet.Owner == bot) &&
            GameServer.ServerRules.IsAllowedToAttack(bot, target, true);

        public static int UsefulRadius(int effectiveRange) => Math.Clamp(effectiveRange > 0 ? effectiveRange : 1500, 750, 2500);

        public static bool IsTraveling(GameBot bot)
        {
            if (bot.IsMoving || bot.IsOnHorse || bot.IsOnStableMasterRoute || bot.IsReturningAfterRelease ||
                bot.IsMovingOnPath || bot.CurrentPathPoint != null ||
                AutonomousBotGroupCoordinator.IsInitialMeetup(bot) || AutonomousBotGroupCoordinator.IsRecovering(bot)) return true;
            if (bot.PlayerGroupLeader is GamePlayer leader &&
                (leader.IsMoving || leader.CurrentRegion != bot.CurrentRegion || !bot.IsWithinRadius(leader, 600))) return true;
            if (bot.IsPlayerLedGroup && bot.Brain?.FSM?.GetCurrentState()?.StateType == eFSMStateType.FOLLOW) return true;
            // A paused travel controller can be stationary while obtaining its
            // next route. Merely selecting a hostile destination is not arrival.
            string activity = bot.PersistentRecord?.Activity ?? string.Empty;
            foreach (string word in TravelWords)
                if (activity.Contains(word, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        public static bool FieldReady(long now, long nextField, int mana, int maxMana, int nearby) =>
            maxMana > 0 && (long)mana * 100 >= (long)maxMana * 35 && now >= nextField && nearby < 3;

        public static void RestoreEncounterTarget(GameBot bot)
        {
            if (bot == null || !States.TryGetValue(bot, out State state) || !state.PendingRestore ||
                bot.IsCasting || bot.castingComponent?.HasPendingSkillRequests == true) return;
            GameLiving target = state.PendingTarget;
            state.PendingTarget = null;
            state.PendingRestore = false;
            if (ReferenceEquals(bot.TargetObject, bot))
                bot.TargetObject = ValidEncounter(bot, target) ? target : null;
        }

        public static bool Maintain(GameBot bot, GameLiving encounterTarget, ref long nextDeployable,
            out string activity, Func<Spell, bool> allowed = null)
        {
            activity = string.Empty;
            RestoreEncounterTarget(bot);
            if (!bot.IsAlive || bot.IsCasting || bot.castingComponent?.HasPendingSkillRequests == true ||
                bot.IsCrowdControlled || bot.IsRecoveryResting) return false;
            bool encounter = ValidEncounter(bot, encounterTarget);
            if (IsTraveling(bot)) return false;
            long now = GameLoop.GameLoopTime;
            State state = States.GetOrCreateValue(bot);
            var main = bot.ControlledBrain?.Body as TurretPet;
            if (encounter && main?.IsAlive == true && main.ObjectState == GameObject.eObjectState.Active &&
                bot.ControlledBrain is TurretBrain { IsMainPet: true } && now >= state.NextRelocation)
            {
                int radius = UsefulRadius(main.TurretSpell?.CalculateEffectiveRange(main) ?? 0);
                if (main.CurrentRegion != bot.CurrentRegion || !main.IsWithinRadius(encounterTarget, radius))
                {
                    state.NextRelocation = now + 3000;
                    bot.CommandNpcRelease();
                    return false; // The next normal maintenance pass may summon a replacement.
                }
            }

            if (bot.ControlledBrain?.Body is GameNPC old &&
                (!old.IsAlive || old.ObjectState != GameObject.eObjectState.Active))
                bot.CommandNpcRelease();

            if (now < nextDeployable) return false;
            if (bot.ControlledBrain != null &&
                (!encounter || !FieldReady(now, state.NextField, bot.Mana, bot.MaxMana, 0))) return false;
            // Enumerate learned spells only when a summon can actually be attempted.
            var spells = AutonomousPetSupport.KnownSpells(bot)
                .Where(entry => (allowed == null || allowed(entry.Spell)) && AutonomousPetSupport.CanCast(bot, entry.Spell));
            if (bot.ControlledBrain == null)
            {
                var summon = AutonomousPetSupport.ChooseWeightedByRank(
                    spells.Where(entry => entry.Spell.SpellType == eSpellType.SummonAnimistPet), bot.IsEndgameCompanion);
                if (summon.Spell != null && Cast(bot, encounter ? encounterTarget : null, summon.Spell, summon.Line))
                {
                    nextDeployable = now + Math.Max(1500, summon.Spell.CastTime + 500);
                    state.NextRelocation = now + 3000;
                    activity = $"Deploying main turret: {summon.Spell.Name}";
                    return true;
                }
            }

            if (!encounter) return false;
            // Count only this Animist's active field turrets near this encounter.
            // Native handlers independently retain their global/area caps.
            if (!FieldReady(now, state.NextField, bot.Mana, bot.MaxMana, 0)) return false;
            int nearby = encounterTarget.GetNPCsInRadius(1000).Count(npc => npc is TurretFnfPet turret &&
                turret.IsAlive && turret.ObjectState == GameObject.eObjectState.Active && turret.Owner == bot);
            if (!FieldReady(now, state.NextField, bot.Mana, bot.MaxMana, nearby) ||
                !AutonomousPetSupport.CanDeployFieldTurret(bot, encounterTarget)) return false;
            var field = AutonomousPetSupport.ChooseWeightedByRank(spells.Where(entry =>
                AutonomousPetSupport.IsAnimistFieldTurret(entry.Spell.SpellType) &&
                bot.IsWithinRadius(encounterTarget, entry.Spell.CalculateEffectiveRange(bot))), bot.IsEndgameCompanion);
            if (field.Spell == null || !Cast(bot, encounterTarget, field.Spell, field.Line)) return false;
            state.NextField = now + 6500;
            nextDeployable = now + Math.Max(750, field.Spell.CastTime + 250);
            activity = $"Planting field turret: {field.Spell.Name}";
            return true;
        }

        internal static void FinishSummonTarget(GameBot bot, GameLiving target, GameObject previous, bool accepted)
        {
            if (accepted && (bot.IsCasting || bot.castingComponent?.HasPendingSkillRequests == true))
            {
                State state = States.GetOrCreateValue(bot);
                state.PendingTarget = target;
                state.PendingRestore = true;
            }
            else if (ReferenceEquals(bot.TargetObject, bot))
                bot.TargetObject = ValidEncounter(bot, target) ? target :
                    previous is GameLiving living ? (ValidEncounter(bot, living) ? living : null) : previous;
        }

        private static bool Cast(GameBot bot, GameLiving target, Spell spell, SpellLine line)
        {
            AutonomousPetSupport.PrepareAnimistGroundTarget(bot, target, spell);
            GameObject previous = bot.TargetObject;
            bot.TargetObject = bot;
            bool accepted = bot.CastSpell(spell, line);
            FinishSummonTarget(bot, target, previous, accepted);
            return accepted;
        }
    }
}
