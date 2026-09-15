using System.Text;

namespace OfflineDaoc.Launcher;

/// <summary>
/// Keeps the launcher's redirected server console bounded without touching the
/// server's own log4net files.  Segments are rotated locally and the oldest
/// complete segment is removed once the aggregate reaches the configured cap.
/// </summary>
internal sealed class RollingServerLog : IDisposable
{
    internal const long DefaultSegmentBytes = 512L * 1024 * 1024;
    internal const long DefaultTotalBytes = 2L * 1024 * 1024 * 1024;
    private const int MaximumArchiveFiles = 8;

    private readonly object _sync = new();
    private readonly string _path;
    private readonly long _segmentBytes;
    private readonly long _totalBytes;
    private StreamWriter? _writer;
    private long _currentBytes;
    private long _unflushedBytes;
    private DateTime _lastFlushUtc;
    private const long FlushByteThreshold = 64 * 1024;
    private static readonly TimeSpan FlushInterval = TimeSpan.FromSeconds(1);

    public RollingServerLog(string path, long segmentBytes = DefaultSegmentBytes,
        long totalBytes = DefaultTotalBytes)
    {
        _path = path;
        _segmentBytes = Math.Max(1024, segmentBytes);
        _totalBytes = Math.Max(_segmentBytes, totalBytes);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        if (File.Exists(_path) && new FileInfo(_path).Length >= _segmentBytes)
            Rotate();
        OpenWriter();
        PruneOldestArchives();
    }

    public void WriteLine(string value)
    {
        lock (_sync)
        {
            long lineBytes = Encoding.UTF8.GetByteCount(value) + Environment.NewLine.Length;
            if (_currentBytes > 0 && _currentBytes + lineBytes > _segmentBytes)
                Rotate();
            _writer!.WriteLine(value);
            _currentBytes += lineBytes;
            _unflushedBytes += lineBytes;
            DateTime now = DateTime.UtcNow;
            if (_unflushedBytes >= FlushByteThreshold || now - _lastFlushUtc >= FlushInterval)
            {
                _writer.Flush();
                _unflushedBytes = 0;
                _lastFlushUtc = now;
            }
            PruneOldestArchives();
        }
    }

    private void OpenWriter()
    {
        var stream = new FileStream(_path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
        _currentBytes = stream.Length;
        _unflushedBytes = 0;
        _lastFlushUtc = DateTime.UtcNow;
        _writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = false };
    }

    private void Rotate()
    {
        _writer?.Dispose();
        _writer = null;

        string oldest = ArchivePath(MaximumArchiveFiles);
        if (File.Exists(oldest))
            File.Delete(oldest);
        for (int index = MaximumArchiveFiles - 1; index >= 1; index--)
        {
            string source = ArchivePath(index);
            if (File.Exists(source))
                File.Move(source, ArchivePath(index + 1), true);
        }
        if (File.Exists(_path))
            File.Move(_path, ArchivePath(1), true);

        OpenWriter();
        PruneOldestArchives();
    }

    private void PruneOldestArchives()
    {
        long total = (_writer != null ? _currentBytes : (File.Exists(_path) ? new FileInfo(_path).Length : 0)) +
            Enumerable.Range(1, MaximumArchiveFiles)
                .Select(ArchivePath)
                .Where(File.Exists)
                .Sum(file => new FileInfo(file).Length);
        for (int index = MaximumArchiveFiles; index >= 1 && total > _totalBytes; index--)
        {
            string archive = ArchivePath(index);
            if (!File.Exists(archive))
                continue;
            long length = new FileInfo(archive).Length;
            File.Delete(archive);
            total -= length;
        }
    }

    private string ArchivePath(int index) => $"{_path}.{index}";

    public void Dispose()
    {
        lock (_sync)
        {
            _writer?.Dispose();
            _writer = null;
        }
    }
}
