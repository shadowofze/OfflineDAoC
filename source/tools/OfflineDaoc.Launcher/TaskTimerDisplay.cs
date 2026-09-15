namespace OfflineDaoc.Launcher;

// Display-only clock. Never queries SQLite, contacts the server or expires a task.
internal static class TaskTimerDisplay
{
    public static long? Remaining(string deadlineUtc, DateTime utcNow) =>
        DateTime.TryParse(deadlineUtc, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime deadline)
            ? Math.Max(0, (long)Math.Ceiling((deadline.ToUniversalTime() - utcNow.ToUniversalTime()).TotalMilliseconds)) : null;

    public static string Format(long milliseconds)
    {
        long seconds = (long)Math.Ceiling(Math.Max(0, milliseconds) / 1000d);
        return $"{seconds / 60:00}:{seconds % 60:00}";
    }
}
