namespace OfflineDaoc.Launcher;

/// <summary>
/// Isolated client display profile for this distribution. Seeds borderless
/// fullscreen only for a brand-new profile; never overwrites a saved
/// Windowed / fullscreen choice or resolution.
/// </summary>
public static class ClientDisplayPreferences
{
    public const string DefaultProfileName = "OfflineDAoCGitHub03";

    public static void EnsureIsolatedLaunchProfile(string clientDirectory)
    {
        EnsureIsolatedLaunchProfile(
            clientDirectory,
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Electronic Arts",
                "Dark Age of Camelot"));
    }

    public static void EnsureIsolatedLaunchProfile(string clientDirectory, string profilesRoot)
    {
        string profileDirectory = Path.Combine(profilesRoot, ResolveProfileName(clientDirectory));
        Directory.CreateDirectory(profileDirectory);
        EnsureDefaultBorderlessIfUnset(Path.Combine(profileDirectory, "user.dat"));
    }

    public static string ResolveProfileName(string clientDirectory)
    {
        string pathsFile = Path.Combine(clientDirectory, "paths.dat");
        if (!File.Exists(pathsFile))
            return DefaultProfileName;

        string? value = File.ReadLines(pathsFile)
            .FirstOrDefault(line => line.TrimStart().StartsWith("settings=", StringComparison.OrdinalIgnoreCase));
        if (value == null)
            return DefaultProfileName;

        string candidate = value[(value.IndexOf('=') + 1)..].Trim();
        if (candidate.Length > 0 && candidate.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '-'))
            return candidate;
        return DefaultProfileName;
    }

    public static void EnsureDefaultBorderlessIfUnset(string userDatPath)
    {
        var lines = File.Exists(userDatPath) ? File.ReadAllLines(userDatPath).ToList() : new List<string>();
        if (lines.Exists(line => line.Trim().Equals("[main]", StringComparison.OrdinalIgnoreCase)))
            return;

        if (lines.Count > 0 && lines[^1].Length != 0)
            lines.Add(string.Empty);
        lines.Add("[main]");
        lines.Add("fullscreen_windowed=1");

        string? directory = Path.GetDirectoryName(userDatPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
        File.WriteAllLines(userDatPath, lines);
    }
}
