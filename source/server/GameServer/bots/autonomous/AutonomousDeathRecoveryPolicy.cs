namespace DOL.GS
{
    /// <summary>The selected fallback and its live pull filter must agree.</summary>
    public static class AutonomousDeathRecoveryPolicy
    {
        public static ConColor EncounterMaximum(ConColor requested, ConColor natural,
            ConColor? selectedFallback, bool dynamicGroup)
        {
            if (dynamicGroup || !selectedFallback.HasValue || selectedFallback.Value <= ConColor.GREY)
                return requested;
            return (ConColor)System.Math.Clamp((int)selectedFallback.Value, (int)ConColor.GREEN, (int)natural);
        }

        public static bool IsEligible(ConColor target, ConColor maximum) =>
            target > ConColor.GREY && target <= maximum;
    }
}
