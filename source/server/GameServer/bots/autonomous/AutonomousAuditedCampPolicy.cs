using System;

namespace DOL.GS
{
    /// <summary>Measured historical-anchor/live-level mismatches, not new spawns.</summary>
    public static class AutonomousAuditedCampPolicy
    {
        // The Salisbury spirit spawn is surrounded within a few hundred units
        // by aggressive undead druids and filidh above the spirit's level.
        // Keep the real target available to formed parties, but do not send a
        // solo leveler into the pack for a nominally green spirit goal.
        public static bool CanAssignToParty(string campId, int partySize) =>
            !string.Equals(campId, "capnbry:1:1:3:spirit", StringComparison.OrdinalIgnoreCase) ||
            partySize >= 4;

        public static bool UsesLiveAnchor(ushort region, string name) =>
            (region == 200 && name?.ToLowerInvariant() is
                "orchard nipper" or "lugradan whelp" or "luricaduane" or "hill toad" or "feccan") ||
            (region == 51 && name?.Equals("large dragonfly", StringComparison.OrdinalIgnoreCase) == true) ||
            (region == 151 && name?.Equals("boobrie hatchling", StringComparison.OrdinalIgnoreCase) == true) ||
            (region == 100 && name?.ToLowerInvariant() is "huldu outcast" or "green serpent");

        /// <summary>
        /// These outdoor spawns contain cliffs, raised props, or isolated mesh
        /// islands close enough to an otherwise valid camp anchor to pass the
        /// ordinary radius lookup. Prove a real, reversible melee approach
        /// before an autonomous bot commits to one of those individual mobs.
        /// </summary>
        public static bool RequiresVerifiedTargetRoute(ushort region, string name) =>
            UsesLiveAnchor(region, name) && name?.ToLowerInvariant() is
                "large dragonfly" or "boobrie hatchling" or "feccan" or "huldu outcast" or "green serpent";
    }
}
