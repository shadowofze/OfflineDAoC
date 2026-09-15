using System;

namespace DOL.GS
{
    /// <summary>Measured historical-anchor/live-level mismatches, not new spawns.</summary>
    public static class AutonomousAuditedCampPolicy
    {
        public static bool UsesLiveAnchor(ushort region, string name) => region == 200 &&
            name?.ToLowerInvariant() is "orchard nipper" or "lugradan whelp" or "luricaduane" or "hill toad";
    }
}
