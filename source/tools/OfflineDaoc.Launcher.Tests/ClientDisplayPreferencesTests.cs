using NUnit.Framework;

namespace OfflineDaoc.Launcher;

[TestFixture]
public sealed class ClientDisplayPreferencesTests
{
    [Test]
    public void MissingUserDatSeedsBorderlessFullscreen()
    {
        using var workspace = new TempWorkspace();
        string userDat = Path.Combine(workspace.Root, "user.dat");

        ClientDisplayPreferences.EnsureDefaultBorderlessIfUnset(userDat);

        Assert.That(File.ReadAllLines(userDat), Is.EqualTo(new[] { "[main]", "fullscreen_windowed=1" }));
    }

    [Test]
    public void MissingMainSectionSeedsBorderlessWithoutDroppingOtherSections()
    {
        using var workspace = new TempWorkspace();
        string userDat = Path.Combine(workspace.Root, "user.dat");
        File.WriteAllLines(userDat, ["[keyboard]", "forward=w"]);

        ClientDisplayPreferences.EnsureDefaultBorderlessIfUnset(userDat);

        Assert.That(File.ReadAllLines(userDat), Is.EqualTo(new[]
        {
            "[keyboard]",
            "forward=w",
            "",
            "[main]",
            "fullscreen_windowed=1",
        }));
    }

    [Test]
    public void ExistingWindowedChoiceIsLeftAlone()
    {
        AssertUnchanged("""
            [main]
            fullscreen_windowed=0
            screen_width=1280
            screen_height=720
            """);
    }

    [Test]
    public void ExistingBorderlessChoiceIsLeftAlone()
    {
        AssertUnchanged("""
            [main]
            fullscreen_windowed=1
            """);
    }

    [Test]
    public void ExistingMainWithoutDisplayModeIsNotReinjected()
    {
        AssertUnchanged("""
            [main]
            screen_width=1600
            screen_height=900
            charname_0=Test
            """);
    }

    [Test]
    public void PathsDatSettingsSelectsTheIsolatedProfileName()
    {
        using var workspace = new TempWorkspace();
        File.WriteAllLines(Path.Combine(workspace.ClientDirectory, "paths.dat"), ["settings=CustomProfile_1"]);

        Assert.That(ClientDisplayPreferences.ResolveProfileName(workspace.ClientDirectory), Is.EqualTo("CustomProfile_1"));
    }

    [TestCase("")]
    [TestCase("settings=")]
    [TestCase("settings=../escape")]
    [TestCase("settings=has space")]
    [TestCase("settings=bad/name")]
    public void InvalidOrMissingPathsDatFallsBackToDefaultProfile(string pathsLine)
    {
        using var workspace = new TempWorkspace();
        if (pathsLine.Length > 0)
            File.WriteAllText(Path.Combine(workspace.ClientDirectory, "paths.dat"), pathsLine + Environment.NewLine);

        Assert.That(
            ClientDisplayPreferences.ResolveProfileName(workspace.ClientDirectory),
            Is.EqualTo(ClientDisplayPreferences.DefaultProfileName));
    }

    [Test]
    public void IsolatedLaunchProfileSeedsOnlyAMissingUserDatUnderTheResolvedName()
    {
        using var workspace = new TempWorkspace();
        File.WriteAllLines(Path.Combine(workspace.ClientDirectory, "paths.dat"), ["settings=ForkProfile"]);
        string existing = Path.Combine(workspace.ProfilesRoot, "ForkProfile", "user.dat");
        Directory.CreateDirectory(Path.GetDirectoryName(existing)!);
        const string saved = "[main]\r\nfullscreen_windowed=0\r\nscreen_width=1024\r\n";
        File.WriteAllText(existing, saved);

        ClientDisplayPreferences.EnsureIsolatedLaunchProfile(workspace.ClientDirectory, workspace.ProfilesRoot);

        Assert.That(File.ReadAllText(existing), Is.EqualTo(saved));
        Assert.That(Directory.Exists(Path.Combine(workspace.ProfilesRoot, ClientDisplayPreferences.DefaultProfileName)), Is.False);
    }

    [Test]
    public void IsolatedLaunchProfileCreatesDefaultUserDatWhenTheProfileIsNew()
    {
        using var workspace = new TempWorkspace();

        ClientDisplayPreferences.EnsureIsolatedLaunchProfile(workspace.ClientDirectory, workspace.ProfilesRoot);

        string userDat = Path.Combine(workspace.ProfilesRoot, ClientDisplayPreferences.DefaultProfileName, "user.dat");
        Assert.That(File.ReadAllLines(userDat), Is.EqualTo(new[] { "[main]", "fullscreen_windowed=1" }));
    }

    private static void AssertUnchanged(string contents)
    {
        using var workspace = new TempWorkspace();
        string userDat = Path.Combine(workspace.Root, "user.dat");
        File.WriteAllText(userDat, contents);
        DateTime written = File.GetLastWriteTimeUtc(userDat);

        ClientDisplayPreferences.EnsureDefaultBorderlessIfUnset(userDat);

        Assert.That(File.ReadAllText(userDat), Is.EqualTo(contents));
        Assert.That(File.GetLastWriteTimeUtc(userDat), Is.EqualTo(written));
    }

    private sealed class TempWorkspace : IDisposable
    {
        public TempWorkspace()
        {
            Root = Path.Combine(Path.GetTempPath(), $"offline-daoc-display-{Guid.NewGuid():N}");
            ClientDirectory = Path.Combine(Root, "client");
            ProfilesRoot = Path.Combine(Root, "profiles");
            Directory.CreateDirectory(ClientDirectory);
            Directory.CreateDirectory(ProfilesRoot);
        }

        public string Root { get; }
        public string ClientDirectory { get; }
        public string ProfilesRoot { get; }

        public void Dispose()
        {
            if (Directory.Exists(Root))
                Directory.Delete(Root, true);
        }
    }
}
