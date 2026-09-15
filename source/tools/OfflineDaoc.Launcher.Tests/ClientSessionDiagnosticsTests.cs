using NUnit.Framework;

namespace OfflineDaoc.Launcher;

[TestFixture]
public sealed class ClientSessionDiagnosticsTests
{
    [TestCase(0, "clean-exit")]
    [TestCase(unchecked((int)0xC0000005), "access-violation")]
    [TestCase(unchecked((int)0xC00000FD), "stack-overflow")]
    [TestCase(unchecked((int)0xC0000409), "fast-fail-or-stack-buffer-overrun")]
    [TestCase(unchecked((int)0xC0000374), "heap-corruption")]
    [TestCase(123, "abnormal-or-client-defined-exit")]
    public void ExitCodesReceiveStableReadableClassification(int exitCode, string expected) =>
        Assert.That(ClientSessionDiagnostics.ClassifyExitCode(exitCode), Is.EqualTo(expected));

    [Test]
    public void GameProcessDetectionRequiresTheActualConfiguredClientFolder()
    {
        string client = Path.Combine(Path.GetTempPath(), "offline-daoc-client");
        Assert.That(ClientSessionDiagnostics.IsGameProcessPath(Path.Combine(client, "game.dll"), client), Is.True);
        Assert.That(ClientSessionDiagnostics.IsGameProcessPath(Path.Combine(client, "game.exe"), client), Is.True);
        Assert.That(ClientSessionDiagnostics.IsGameProcessPath(Path.Combine(client, "connect.exe"), client), Is.False);
        Assert.That(ClientSessionDiagnostics.IsGameProcessPath(Path.Combine(client, "other", "game.dll"), client), Is.False);
    }
}
