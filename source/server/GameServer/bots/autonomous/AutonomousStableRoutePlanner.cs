using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using DOL.Database;
using DOL.GS.Movement;

namespace DOL.GS;

public static class AutonomousStableRouteLifecycle
{
    public static bool ShouldComplete(
        bool departurePending,
        bool movingOnPath,
        bool hasCurrentPathPoint,
        bool reachedFinalWaypoint) =>
        !departurePending && !movingOnPath && !hasCurrentPathPoint && reachedFinalWaypoint;
}

/// <summary>
/// Scores the complete trip through the live stable network. A route is always
/// actual waypoint travel: this class only chooses which ticket to board first.
/// After each ride the bot replans, allowing any number of useful connections.
/// </summary>
public static class AutonomousStableRoutePlanner
{
    public const short StableSpeed = 1500;
    private const int MaximumPathPoints = 5000;
    private const int MaximumBoardingDistance = 500;
    private static readonly TimeSpan NetworkCacheLifetime = TimeSpan.FromMinutes(5);
    private static readonly object NetworkCacheLock = new();
    private static readonly Dictionary<(ushort RegionId, eRealm Realm), CandidateCache> NetworkCache = new();

    public readonly record struct LegMetric(
        int Index,
        double OriginX,
        double OriginY,
        double EndX,
        double EndY,
        double RideSeconds,
        long Price);

    public readonly record struct RouteDecision(
        int FirstLegIndex,
        double EstimatedSeconds,
        double DirectWalkSeconds,
        int HopCount,
        long PlannedPrice);

    public sealed record Choice(
        GameStableMaster Master,
        DbItemTemplate Ticket,
        PathPoint Route,
        string DestinationName,
        double RideSeconds,
        double EstimatedSeconds,
        double DirectWalkSeconds,
        int PlannedHops,
        long PlannedPrice,
        Vector3 BoardingPoint,
        Vector3 InteractionPoint);

    private sealed record Candidate(
        GameStableMaster Master,
        DbItemTemplate Ticket,
        PathPoint Route,
        PathPoint End,
        double RideSeconds,
        Vector3 BoardingPoint,
        Vector3 InteractionPoint);

    private sealed record CandidateCache(DateTime ExpiresUtc, Candidate[] Candidates);

    /// <summary>
    /// Dijkstra over route endpoints. Walking connects the bot, every ticket
    /// origin, every ticket endpoint, and the goal. Positive edge costs prevent
    /// cycling, while the first selected leg is returned for live execution.
    /// </summary>
    public static RouteDecision? ChooseFirstLeg(
        double startX,
        double startY,
        double goalX,
        double goalY,
        double walkSpeed,
        long availableMoney,
        IReadOnlyList<LegMetric> legs,
        IReadOnlySet<int> excludedBoardingLegs = null)
    {
        walkSpeed = Math.Max(1d, walkSpeed);
        double direct = Distance(startX, startY, goalX, goalY) / walkSpeed;
        if (legs == null || legs.Count == 0)
            return null;

        int count = legs.Count;
        double[] best = Enumerable.Repeat(double.PositiveInfinity, count).ToArray();
        int[] first = Enumerable.Repeat(-1, count).ToArray();
        int[] hops = new int[count];
        long[] spent = new long[count];
        bool[] visited = new bool[count];

        for (int index = 0; index < count; index++)
        {
            LegMetric leg = legs[index];
            if (excludedBoardingLegs?.Contains(index) == true)
                continue;
            if (leg.Price < 0 || leg.Price > availableMoney || leg.RideSeconds <= 0)
                continue;
            best[index] = Distance(startX, startY, leg.OriginX, leg.OriginY) / walkSpeed + leg.RideSeconds;
            first[index] = index;
            hops[index] = 1;
            spent[index] = leg.Price;
        }

        double bestTrip = direct;
        int bestFirst = -1;
        int bestHops = 0;
        long bestPrice = 0;

        for (int iteration = 0; iteration < count; iteration++)
        {
            int currentIndex = -1;
            double currentCost = double.PositiveInfinity;
            for (int index = 0; index < count; index++)
            {
                if (!visited[index] && best[index] < currentCost)
                {
                    currentIndex = index;
                    currentCost = best[index];
                }
            }

            if (currentIndex < 0)
                break;
            visited[currentIndex] = true;
            LegMetric current = legs[currentIndex];

            double completed = currentCost + Distance(current.EndX, current.EndY, goalX, goalY) / walkSpeed;
            if (completed + 0.25 < bestTrip)
            {
                bestTrip = completed;
                bestFirst = first[currentIndex];
                bestHops = hops[currentIndex];
                bestPrice = spent[currentIndex];
            }

            for (int nextIndex = 0; nextIndex < count; nextIndex++)
            {
                if (visited[nextIndex])
                    continue;
                LegMetric next = legs[nextIndex];
                if (next.Price < 0 || spent[currentIndex] > availableMoney - next.Price || next.RideSeconds <= 0)
                    continue;

                double candidate = currentCost +
                                   Distance(current.EndX, current.EndY, next.OriginX, next.OriginY) / walkSpeed +
                                   next.RideSeconds;
                if (candidate + 0.001 >= best[nextIndex])
                    continue;

                best[nextIndex] = candidate;
                first[nextIndex] = first[currentIndex];
                hops[nextIndex] = hops[currentIndex] + 1;
                spent[nextIndex] = spent[currentIndex] + next.Price;
            }
        }

        return bestFirst < 0
            ? null
            : new RouteDecision(bestFirst, bestTrip, direct, bestHops, bestPrice);
    }

    public static Choice FindBest(GameBot bot, Vector3 goal, IReadOnlySet<GameStableMaster> excludedBoardingMasters = null,
        bool boundedMeetupApproach = false)
    {
        if (bot?.CurrentRegion == null)
            return null;

        List<Candidate> candidates = GetCandidates(bot);
        if (candidates.Count == 0)
            return null;

        LegMetric[] metrics = candidates.Select((candidate, index) => new LegMetric(
            index,
            candidate.BoardingPoint.X,
            candidate.BoardingPoint.Y,
            candidate.End.X,
            candidate.End.Y,
            candidate.RideSeconds,
            Math.Max(0L, (long)candidate.Ticket.Price))).ToArray();

        long money = bot.DatabaseID > 0 ? AutonomousBotEconomy.GetMoney(bot.DatabaseID) : 0;
        // Exclude only boarding here. A later horse can still reach that master
        // without repeating this bot's failed walk from its current location.
        HashSet<int> excluded = excludedBoardingMasters == null ? null : candidates
            .Select((candidate, index) => (candidate, index))
            .Where(entry => excludedBoardingMasters.Contains(entry.candidate.Master))
            .Select(entry => entry.index).ToHashSet();

        // A timed meetup must not spend most of its fifteen-minute attendance
        // window walking away to a distant first horse (the Vuloch failures).
        // Later network legs remain legal; outside assembly behavior is unchanged.
        if (boundedMeetupApproach)
            for (int i = 0; i < candidates.Count; i++)
                if (!CanApproachStableDuringMeetup(Distance(bot.X, bot.Y,
                        candidates[i].BoardingPoint.X, candidates[i].BoardingPoint.Y), bot.MaxSpeed))
                    (excluded ??= []).Add(i);

        // A stable on another disconnected surface can be closer in straight
        // line than the reachable stable inside a fortress.  Exclude it only as
        // this journey's first boarding leg after proving that both points are
        // in the same zone but have no Detour corridor.  It remains available
        // as a later horse-network destination.
        Zone currentZone = bot.CurrentZone;
        IPathfindingMgr nav = PathfindingProvider.Instance;
        if (currentZone != null && nav.IsAvailable && nav.HasNavmesh(currentZone) &&
            AutonomousNavigationSurface.TryFloor(nav, currentZone,
                new(bot.X, bot.Y, bot.Z), out Vector3 currentFloor))
        {
            foreach ((Candidate candidate, int index) in candidates.Select((candidate, index) => (candidate, index)))
            {
                Zone boardingZone = bot.CurrentRegion.GetZone(
                    (int)candidate.BoardingPoint.X, (int)candidate.BoardingPoint.Y);
                bool sameZone = boardingZone == currentZone;
                bool completeCorridor = !sameZone || AutonomousZoneItinerary.HasCompleteCorridor(
                    nav, currentZone, currentFloor, candidate.BoardingPoint);
                if (CanUseAsFirstBoardingLeg(sameZone, completeCorridor))
                    continue;
                (excluded ??= []).Add(index);
            }
        }
        RouteDecision? decision = ChooseFirstLeg(
            bot.X, bot.Y, goal.X, goal.Y, Math.Max(1, (int)bot.MaxSpeed), money, metrics, excluded);
        if (!decision.HasValue)
            return null;

        Candidate selected = candidates[decision.Value.FirstLegIndex];
        return new Choice(
            selected.Master,
            selected.Ticket,
            CloneRoute(selected.Route),
            TicketDestination(selected.Ticket),
            selected.RideSeconds,
            decision.Value.EstimatedSeconds,
            decision.Value.DirectWalkSeconds,
            decision.Value.HopCount,
            decision.Value.PlannedPrice,
            selected.BoardingPoint,
            selected.InteractionPoint);
    }

    public static bool CanUseAsFirstBoardingLeg(bool sameZone, bool completeCorridor) =>
        !sameZone || completeCorridor;

    public static bool CanApproachStableDuringMeetup(double distance, double speed) =>
        double.IsFinite(distance) && distance >= 0 && speed > 0 && distance <= speed * 120;

    public static bool FinishMeetupOnFoot(bool meetup, float distanceSquared) =>
        meetup && float.IsFinite(distanceSquared) && distanceSquared >= 0 && distanceSquared <= 2_000 * 2_000;

    public static bool MeetupBoardingExpired(bool meetup, long started, long now, long lastProgress = 0) =>
        meetup && now >= started && now - started >= 120_000 &&
        (now - started >= 300_000 || lastProgress <= started || now - lastProgress >= 90_000);

    // NpcMovementComponent toggles FiredFlag while traversing even a Once
    // route. Cached topology must therefore be cloned per rider or one horse
    // can make another skip/reverse a point and appear riderless.
    internal static PathPoint CloneRoute(PathPoint route)
    {
        PathPoint first = null;
        PathPoint previous = null;
        for (PathPoint current = route; current != null; current = current.Next)
        {
            var copy = new PathPoint(current.X, current.Y, current.Z, current.MaxSpeed, current.Type)
            {
                WaitTime = current.WaitTime,
            };
            first ??= copy;
            copy.Prev = previous;
            if (previous != null)
                previous.Next = copy;
            previous = copy;
        }
        return first;
    }

    private static List<Candidate> GetCandidates(GameBot bot)
    {
        var key = (bot.CurrentRegionID, bot.Realm);
        DateTime now = DateTime.UtcNow;
        CandidateCache cache;
        lock (NetworkCacheLock)
        {
            if (!NetworkCache.TryGetValue(key, out cache) || cache.ExpiresUtc <= now)
            {
                cache = new CandidateCache(now + NetworkCacheLifetime,
                    BuildCandidates(bot.CurrentRegion, bot.Realm).ToArray());
                NetworkCache[key] = cache;
            }
        }

        // Masters can be removed between cache rebuilds; live state validation
        // is cheap and prevents choosing a stale boarding point.
        return cache.Candidates
            .Where(candidate => candidate.Master?.ObjectState is GameObject.eObjectState.Active &&
                                candidate.Master.CurrentRegion == bot.CurrentRegion)
            .ToList();
    }

    private static List<Candidate> BuildCandidates(Region region, eRealm realm)
    {
        List<Candidate> candidates = new();
        foreach (GameStableMaster master in region.Objects.OfType<GameStableMaster>()
                     .Where(master => master.ObjectState is GameObject.eObjectState.Active &&
                                      (master.Realm == realm || master.Realm == eRealm.None)))
        {
            if (master.TradeItems == null)
                continue;

            foreach (DictionaryEntry entry in master.TradeItems.GetAllItems())
            {
                if (entry.Value is not DbItemTemplate ticket || ticket.Item_Type != 40)
                    continue;
                PathPoint route = MovementMgr.LoadPath(ticket.Id_nb);
                if (!TryMeasureRoute(master, route, out PathPoint endpoint, out double rideSeconds))
                    continue;
                if (region.GetZone(endpoint.X, endpoint.Y) == null)
                    continue;
                // Some old ticket origins are below the installed terrain
                // (Mularn/Fort Atla was ~220 units low). Normalize only the
                // walking approach, never the authoritative horse waypoints.
                Zone boardingZone = region.GetZone(route.X, route.Y);
                IPathfindingMgr nav = PathfindingProvider.Instance;
                if (boardingZone == null || !TryResolveBoardingPoint(new(route.X, route.Y, route.Z),
                        p => nav.GetClosestPoint(boardingZone, p, 48, 48, 256, nav.DefaultFilters), out Vector3 boarding))
                    continue;
                // Match proximity on the same walkable surface when this old
                // NPC also has stale terrain Z. No entity or ticket is moved.
                if (!TryResolveBoardingPoint(new(master.X, master.Y, master.Z),
                        p => nav.GetClosestPoint(boardingZone, p, 48, 48, 256, nav.DefaultFilters), out Vector3 interaction) ||
                    region.GetZone((int)boarding.X, (int)boarding.Y) != boardingZone ||
                    region.GetZone((int)interaction.X, (int)interaction.Y) != boardingZone ||
                    Vector3.Distance(boarding, interaction) > master.InteractDistance ||
                    !AutonomousZoneItinerary.HasCompleteCorridor(nav, boardingZone, boarding, interaction))
                    continue;
                candidates.Add(new Candidate(master, ticket, route, endpoint, rideSeconds, boarding, interaction));
            }
        }
        return candidates;
    }

    public static bool TryResolveBoardingPoint(Vector3 point, Func<Vector3, Vector3?> snap, out Vector3 result)
    {
        result = default;
        Vector3? snapped = snap(point);
        if (!snapped.HasValue || !float.IsFinite(snapped.Value.X) || !float.IsFinite(snapped.Value.Y) ||
            !float.IsFinite(snapped.Value.Z) || Math.Abs(snapped.Value.Z - point.Z) > 256 ||
            Vector2.Distance(new(point.X, point.Y), new(snapped.Value.X, snapped.Value.Y)) > 48)
            return false;
        result = snapped.Value;
        return true;
    }

    private static bool TryMeasureRoute(
        GameStableMaster master,
        PathPoint route,
        out PathPoint endpoint,
        out double rideSeconds)
    {
        endpoint = route;
        rideSeconds = 0;
        if (route == null || route.Type is not EPathType.Once ||
            Distance(master.X, master.Y, route.X, route.Y) > MaximumBoardingDistance)
            return false;

        return TryMeasureRide(route, out endpoint, out rideSeconds);
    }

    /// <summary>
    /// Measures a validated stable path without choosing, buying, or charging
    /// for a ticket.  Player-led temporary companions use this only to mirror
    /// the real player's already-approved native ride.
    /// </summary>
    public static bool TryMeasureRide(PathPoint route, out PathPoint endpoint, out double rideSeconds)
    {
        endpoint = route;
        rideSeconds = 0;
        if (route == null || route.Type is not EPathType.Once)
            return false;

        int points = 0;
        while (endpoint.Next != null && points++ < MaximumPathPoints)
        {
            PathPoint next = endpoint.Next;
            short segmentSpeed = (short)Math.Clamp((int)next.MaxSpeed, 1, (int)StableSpeed);
            rideSeconds += Distance(endpoint.X, endpoint.Y, next.X, next.Y) / segmentSpeed;
            rideSeconds += Math.Max(0, endpoint.WaitTime) * 0.1;
            endpoint = next;
        }

        return endpoint.Next == null && points > 0 && rideSeconds > 0;
    }

    private static string TicketDestination(DbItemTemplate ticket)
    {
        string name = ticket?.Name?.Trim() ?? string.Empty;
        int marker = name.IndexOf("ticket to", StringComparison.OrdinalIgnoreCase);
        if (marker >= 0)
            name = name[(marker + "ticket to".Length)..].Trim();
        return string.IsNullOrWhiteSpace(name) ? "the next stable" : name;
    }

    private static double Distance(double x1, double y1, double x2, double y2)
    {
        double dx = x2 - x1;
        double dy = y2 - y1;
        return Math.Sqrt(dx * dx + dy * dy);
    }
}
