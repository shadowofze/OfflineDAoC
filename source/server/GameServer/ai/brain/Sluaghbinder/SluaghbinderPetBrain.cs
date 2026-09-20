using DOL.GS;

namespace DOL.AI.Brain
{
    /// <summary>
    /// Controlled brain used only by player-owned Sluaghbinder pets in the
    /// isolated new-class build.
    /// </summary>
    public sealed class SluaghbinderPetBrain : ControlledMobBrain
    {
        public SluaghbinderPetBrain(GameLiving owner) : base(owner) { }

        private bool IsZombiePriest =>
            Body?.NPCTemplate?.Name?.Equals("zombie priest", System.StringComparison.OrdinalIgnoreCase) == true;

        /// <summary>
        /// The priest remains a normal controlled melee/spell pet, but checks
        /// its defensive spell list before committing to another attack tick.
        /// A valid heal or upkeep cast therefore interrupts melee naturally;
        /// when nobody needs help, the base pet brain attacks as usual.
        /// </summary>
        public override void AttackMostWanted()
        {
            if (IsZombiePriest && Body.IsAlive && CheckSpells(eCheckSpellType.Defensive))
            {
                Body.StopAttack();
                return;
            }

            base.AttackMostWanted();
        }
    }
}
