using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace OfflineDaoc.Launcher;

/// <summary>
/// Client-side diagnostics for the legacy connect.exe/game.dll process. This
/// never changes packets, input, movement, server state, or gameplay.
/// </summary>
public static class ClientSessionDiagnostics
{
    private const long MaximumLogBytes = 4 * 1024 * 1024;
    private static readonly Lock LogLock = new();

    [StructLayout(LayoutKind.Sequential)]
    private struct LastInputInfo
    {
        public uint Size;
        public uint TickCount;
    }

    [DllImport("user32.dll")]
    private static extern bool GetLastInputInfo(ref LastInputInfo info);

    public static void Prepare(string logsDirectory)
    {
        try
        {
            string dumpDirectory = Path.Combine(logsDirectory, "client-dumps");
            Directory.CreateDirectory(dumpDirectory);
            ConfigureLocalDumps("connect.exe", dumpDirectory);
            ConfigureLocalDumps("game.dll", dumpDirectory);
            ConfigureLocalDumps("game.exe", dumpDirectory);
        }
        catch (Exception exception)
        {
            Write(logsDirectory, $"DIAGNOSTICS_SETUP_FAILED type={exception.GetType().Name} message={Quote(exception.Message)}");
        }
    }

    public static void Start(Process client, string clientDirectory, string logsDirectory)
    {
        _ = MonitorAsync(client, clientDirectory, logsDirectory);
    }

    private static async Task MonitorAsync(Process bootstrapClient, string clientDirectory, string logsDirectory)
    {
        DateTime startedUtc = DateTime.UtcNow;
        Process trackedClient = bootstrapClient;
        int bootstrapProcessId = SafeProcessId(bootstrapClient);
        int processId = bootstrapProcessId;
        bool trackingGameProcess = false;
        bool bootstrapExitLogged = false;
        DateTime successorDeadlineUtc = startedUtc.AddMinutes(3);
        DateTime nextSampleUtc = startedUtc;
        string previousConnectionState = string.Empty;
        try
        {
            string gameDll = Path.Combine(clientDirectory, "game.dll");
            FileInfo game = new(gameDll);
            FileVersionInfo version = FileVersionInfo.GetVersionInfo(gameDll);
            Write(logsDirectory,
                $"CLIENT_SESSION_START bootstrapPid={bootstrapProcessId} utc={startedUtc:O} os={Quote(Environment.OSVersion.VersionString)} " +
                $"gameVersion={Quote(version.FileVersion ?? string.Empty)} gameBytes={game.Length} gameUtc={game.LastWriteTimeUtc:O}");

            while (true)
            {
                await Task.Delay(TimeSpan.FromSeconds(2)).ConfigureAwait(false);

                if (!trackingGameProcess)
                {
                    Process? gameProcess = FindGameProcess(clientDirectory, startedUtc, bootstrapProcessId);
                    if (gameProcess != null)
                    {
                        trackedClient = gameProcess;
                        processId = SafeProcessId(gameProcess);
                        trackingGameProcess = true;
                        Write(logsDirectory,
                            $"CLIENT_PROCESS_HANDOFF bootstrapPid={bootstrapProcessId} gamePid={processId} utc={DateTime.UtcNow:O} " +
                            $"image={Quote(SafeProcessPath(gameProcess))}");
                    }
                    else if (SafeHasExited(bootstrapClient))
                    {
                        if (!bootstrapExitLogged)
                        {
                            bootstrapExitLogged = true;
                            Write(logsDirectory,
                                $"CLIENT_BOOTSTRAP_EXIT pid={bootstrapProcessId} utc={DateTime.UtcNow:O} " +
                                $"exitCode={SafeExitCode(bootstrapClient)} awaitingGameProcess=true");
                        }
                        if (DateTime.UtcNow >= successorDeadlineUtc)
                            break;
                        continue;
                    }
                }

                if (SafeHasExited(trackedClient))
                    break;

                string connectionState = LoopbackServerConnectionState();
                if (!string.Equals(previousConnectionState, connectionState, StringComparison.Ordinal))
                {
                    Write(logsDirectory,
                        $"CLIENT_TCP_CHANGE pid={processId} utc={DateTime.UtcNow:O} loopback10300={connectionState}");
                    previousConnectionState = connectionState;
                }

                if (DateTime.UtcNow < nextSampleUtc)
                    continue;
                nextSampleUtc = DateTime.UtcNow.AddSeconds(30);

                Write(logsDirectory,
                    $"CLIENT_SAMPLE pid={processId} utc={DateTime.UtcNow:O} runtimeSeconds={(long)(DateTime.UtcNow - startedUtc).TotalSeconds} " +
                    $"idleSeconds={IdleSeconds()} responding={SafeResponding(trackedClient)} metrics={Quote(SafeProcessMetrics(trackedClient))} " +
                    $"loopback10300={connectionState}");
            }

            int exitCode = SafeExitCode(trackedClient);
            string dump = NewestDump(logsDirectory, startedUtc);
            Write(logsDirectory,
                $"CLIENT_SESSION_END pid={processId} utc={DateTime.UtcNow:O} runtimeSeconds={(long)(DateTime.UtcNow - startedUtc).TotalSeconds} " +
                $"idleSeconds={IdleSeconds()} exitCode={exitCode} exitHex=0x{unchecked((uint)exitCode):X8} " +
                $"classification={ClassifyExitCode(exitCode)} trackedGame={trackingGameProcess} " +
                $"loopback10300={LoopbackServerConnectionState()} dump={Quote(dump)}");
        }
        catch (Exception exception)
        {
            Write(logsDirectory,
                $"CLIENT_MONITOR_FAILED utc={DateTime.UtcNow:O} type={exception.GetType().Name} message={Quote(exception.Message)}");
        }
        finally
        {
            if (!ReferenceEquals(trackedClient, bootstrapClient))
                trackedClient.Dispose();
            bootstrapClient.Dispose();
        }
    }

    private static void ConfigureLocalDumps(string executableName, string dumpDirectory)
    {
        using RegistryKey? key = Registry.CurrentUser.CreateSubKey(
            $@"Software\Microsoft\Windows\Windows Error Reporting\LocalDumps\{executableName}");
        key?.SetValue("DumpFolder", dumpDirectory, RegistryValueKind.ExpandString);
        key?.SetValue("DumpCount", 3, RegistryValueKind.DWord);
        key?.SetValue("DumpType", 1, RegistryValueKind.DWord); // Small minidump, not a full-memory dump.
    }

    private static Process? FindGameProcess(string clientDirectory, DateTime startedUtc, int bootstrapProcessId)
    {
        foreach (Process candidate in Process.GetProcesses())
        {
            try
            {
                if (candidate.Id == bootstrapProcessId || candidate.HasExited ||
                    candidate.StartTime.ToUniversalTime() < startedUtc.AddSeconds(-10) ||
                    !IsGameProcessPath(candidate.MainModule?.FileName, clientDirectory))
                {
                    candidate.Dispose();
                    continue;
                }
                return candidate;
            }
            catch
            {
                candidate.Dispose();
            }
        }
        return null;
    }

    public static bool IsGameProcessPath(string? candidatePath, string clientDirectory)
    {
        if (string.IsNullOrWhiteSpace(candidatePath) || string.IsNullOrWhiteSpace(clientDirectory))
            return false;
        try
        {
            string fileName = Path.GetFileName(candidatePath);
            if (!fileName.Equals("game.dll", StringComparison.OrdinalIgnoreCase) &&
                !fileName.Equals("game.exe", StringComparison.OrdinalIgnoreCase))
                return false;
            return string.Equals(Path.GetFullPath(Path.GetDirectoryName(candidatePath) ?? string.Empty)
                    .TrimEnd(Path.DirectorySeparatorChar),
                Path.GetFullPath(clientDirectory).TrimEnd(Path.DirectorySeparatorChar),
                StringComparison.OrdinalIgnoreCase);
        }
        catch { return false; }
    }

    public static string ClassifyExitCode(int exitCode) => unchecked((uint)exitCode) switch
    {
        0 => "clean-exit",
        0xC0000005 => "access-violation",
        0xC00000FD => "stack-overflow",
        0xC0000409 => "fast-fail-or-stack-buffer-overrun",
        0xC0000374 => "heap-corruption",
        0x40000015 => "fatal-application-exit",
        _ => "abnormal-or-client-defined-exit",
    };

    private static long IdleSeconds()
    {
        var info = new LastInputInfo { Size = (uint)Marshal.SizeOf<LastInputInfo>() };
        return GetLastInputInfo(ref info)
            ? unchecked((uint)Environment.TickCount - info.TickCount) / 1000L
            : -1;
    }

    private static bool SafeResponding(Process process)
    {
        try { return process.Responding; }
        catch { return false; }
    }

    private static int SafeProcessId(Process process)
    {
        try { return process.Id; }
        catch { return -1; }
    }

    private static bool SafeHasExited(Process process)
    {
        try
        {
            process.Refresh();
            return process.HasExited;
        }
        catch { return true; }
    }

    private static int SafeExitCode(Process process)
    {
        try { return process.HasExited ? process.ExitCode : int.MinValue; }
        catch { return int.MinValue; }
    }

    private static string SafeProcessPath(Process process)
    {
        try { return process.MainModule?.FileName ?? string.Empty; }
        catch { return string.Empty; }
    }

    private static string SafeProcessMetrics(Process process)
    {
        try
        {
            process.Refresh();
            return $"workingSetMB={process.WorkingSet64 / 1048576d:F1} " +
                $"privateMB={process.PrivateMemorySize64 / 1048576d:F1} " +
                $"peakWorkingSetMB={process.PeakWorkingSet64 / 1048576d:F1} " +
                $"threads={process.Threads.Count} handles={process.HandleCount} " +
                $"cpuSeconds={process.TotalProcessorTime.TotalSeconds:F1}";
        }
        catch (Exception exception)
        {
            return "unavailable-" + exception.GetType().Name;
        }
    }

    private static string LoopbackServerConnectionState()
    {
        try
        {
            TcpConnectionInformation[] matches = IPGlobalProperties.GetIPGlobalProperties()
                .GetActiveTcpConnections()
                .Where(connection => connection.RemoteEndPoint.Port == 10300 &&
                    (connection.RemoteEndPoint.Address.Equals(System.Net.IPAddress.Loopback) ||
                     connection.RemoteEndPoint.Address.Equals(System.Net.IPAddress.IPv6Loopback)))
                .ToArray();
            return matches.Length == 0 ? "absent" : string.Join(',', matches.Select(match => match.State).Distinct());
        }
        catch (Exception exception)
        {
            return "unavailable-" + exception.GetType().Name;
        }
    }

    private static string NewestDump(string logsDirectory, DateTime startedUtc)
    {
        try
        {
            return Directory.EnumerateFiles(Path.Combine(logsDirectory, "client-dumps"), "*.dmp")
                .Select(path => new FileInfo(path))
                .Where(file => file.LastWriteTimeUtc >= startedUtc.AddSeconds(-5))
                .OrderByDescending(file => file.LastWriteTimeUtc)
                .Select(file => file.FullName)
                .FirstOrDefault() ?? string.Empty;
        }
        catch { return string.Empty; }
    }

    private static void Write(string logsDirectory, string message)
    {
        try
        {
            lock (LogLock)
            {
                Directory.CreateDirectory(logsDirectory);
                string path = Path.Combine(logsDirectory, "client-diagnostics.log");
                if (File.Exists(path) && new FileInfo(path).Length >= MaximumLogBytes)
                {
                    string oldest = path + ".3";
                    if (File.Exists(oldest)) File.Delete(oldest);
                    for (int index = 2; index >= 1; index--)
                    {
                        string source = path + "." + index;
                        if (File.Exists(source)) File.Move(source, path + "." + (index + 1));
                    }
                    File.Move(path, path + ".1");
                }
                File.AppendAllText(path, message + Environment.NewLine);
            }
        }
        catch { }
    }

    private static string Quote(string value) => '"' + (value ?? string.Empty).Replace("\r", " ").Replace("\n", " ").Replace("\"", "'") + '"';
}
