using System;
using System.Collections.Generic;
using System.Numerics;

namespace DOL.GS
{
    /// <summary>At most eight entries, updated by the existing group pulse; no timers or I/O.</summary>
    public sealed class AutonomousRendezvousAttendance
    {
        public const long TimeoutMilliseconds = 15 * 60_000L;
        public const int ArrivalRadius = 38;
        public const int OutdoorArrivalRadius = 80;
        public const int EndpointTolerance = 8;
        public static int ApproachRadius(bool tight) => Radius(tight) / 2;
        public static int Radius(bool tight) => tight ? ArrivalRadius : OutdoorArrivalRadius;
        public static bool IsAtSlot(Vector3 position, Vector3 slot, bool tight = true)
        {
            int radius = Radius(tight);
            return Vector3.DistanceSquared(position, slot) <= radius * radius;
        }
        private readonly Dictionary<long, long> _due = new();
        private readonly HashSet<long> _arrived = new();
        private readonly Dictionary<long, DateTime> _dueUtc = new();
        public long Revision { get; private set; }

        public void Add(long memberId, long now, DateTime? utcNow = null)
        {
            if (_due.TryAdd(memberId, now + TimeoutMilliseconds))
            {
                _dueUtc[memberId] = (utcNow ?? DateTime.UtcNow).AddMilliseconds(TimeoutMilliseconds);
                Revision++;
            }
        }

        // Stable UTC value for the launcher's local countdown. No per-second writes.
        public DateTime? DeadlineUtc(long memberId) => !_arrived.Contains(memberId) &&
            _dueUtc.TryGetValue(memberId, out DateTime due) ? due : null;

        public bool Observe(long memberId, long now, bool atRendezvous)
        {
            Add(memberId, now);
            if (atRendezvous && _arrived.Add(memberId))
                Revision++;
            else if (!atRendezvous && _arrived.Remove(memberId))
                Revision++;
            return !_arrived.Contains(memberId) && now >= _due[memberId];
        }

        public long WaitedMilliseconds(long memberId, long now) =>
            _due.TryGetValue(memberId, out long due) ? now - (due - TimeoutMilliseconds) : 0;

        /// <summary>
        /// Starts one new, shared attendance window after the coordinator has
        /// changed the rendezvous or rebuilt formation slots. Keeping the old
        /// expired deadlines would immediately expel members that were already
        /// present but were assigned a different slot by that rebuild.
        /// </summary>
        public void Rebase(IEnumerable<long> memberIds, long now, DateTime? utcNow = null)
        {
            _due.Clear();
            _arrived.Clear();
            _dueUtc.Clear();
            Revision++;
            foreach (long memberId in memberIds)
                Add(memberId, now, utcNow);
        }

        public void Reset()
        {
            _due.Clear();
            _arrived.Clear();
            _dueUtc.Clear();
            Revision++;
        }
    }
}
