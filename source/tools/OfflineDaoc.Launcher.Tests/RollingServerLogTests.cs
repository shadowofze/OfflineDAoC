using System.Reflection;
using NUnit.Framework;

namespace OfflineDaoc.Launcher.Tests;

[TestFixture]
public sealed class RollingServerLogTests
{
    [Test]
    public void RotationKeepsAggregateBelowConfiguredLimit()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"offline-daoc-log-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            Type type = Assembly.Load("OfflineDAoC").GetType("OfflineDaoc.Launcher.RollingServerLog", true)!;
            object log = Activator.CreateInstance(type, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null, [Path.Combine(directory, "server-console.log"), 1024L, 3072L], null)!;
            MethodInfo writeLine = type.GetMethod("WriteLine")!;
            for (int i = 0; i < 200; i++)
                writeLine.Invoke(log, [new string('x', 80)]);
            ((IDisposable)log).Dispose();

            long total = Directory.GetFiles(directory, "server-console.log*").Sum(file => new FileInfo(file).Length);
            Assert.That(total, Is.LessThanOrEqualTo(3072L));
            Assert.That(Directory.GetFiles(directory, "server-console.log*").Length, Is.GreaterThan(1));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }
}
