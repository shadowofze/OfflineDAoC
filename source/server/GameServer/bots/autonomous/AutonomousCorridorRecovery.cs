using System;
using System.Numerics;

namespace DOL.GS
{
    /// <summary>Cold failure recovery only; never a per-tick route search or teleport.</summary>
    public static class AutonomousCorridorRecovery
    {
        public static bool TryNextCorner(IPathfindingMgr nav, Zone zone, Vector3 current,
            Vector3 destination, out Vector3 corner)
        {
            corner = default;
            if (nav == null || zone == null || !nav.IsAvailable || !nav.HasNavmesh(zone) ||
                !AutonomousNavigationSurface.TryFloor(nav, zone, current, out Vector3 floor)) return false;
            Span<WrappedPathfindingNode> nodes = stackalloc WrappedPathfindingNode[256];
            PathfindingResult path = nav.GetPathStraight(zone, floor, destination, nav.DefaultFilters, nodes);
            if (path.Status != PathfindingStatus.PathFound || path.NodeCount < 1 || path.NodeCount > nodes.Length)
                return false;
            // This is a PathTo destination, NOT a direct WalkTo chord. Keep
            // the first real path bend even when it is close: straight sideways
            // probes reject exactly the narrow corners needing recovery.
            for (int i = 0; i < path.NodeCount; i++)
            {
                Vector3 point = nodes[i].Position;
                float distance = Vector3.Distance(floor, point);
                if (distance < 16) continue;
                if (distance > 1500 || !AutonomousZoneItinerary.HasCompleteCorridor(nav, zone, floor, point)) return false;
                corner = point;
                return true;
            }
            return false;
        }
    }
}
