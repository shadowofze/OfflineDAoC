using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;

namespace OfflineDaoc.SluaghbinderPatch;

/// <summary>
/// Creates the client-side part of the optional class patch without changing
/// the source installation. The caller stages the returned files, backs up
/// existing targets, and only then installs them in its separate game copy.
/// No Python runtime or machine-specific client path is needed by players.
/// </summary>
public static class ClientAssetMerge
{
    private static readonly Encoding Latin1 = Encoding.Latin1;
    private static readonly StringComparer FileNameComparer = StringComparer.OrdinalIgnoreCase;

    // Paths are relative to runtime/client-opendaoc/app. The installer may
    // feed these directly to its existing backup/replacement transaction.
    public const string CatalogPath = "gamedata.mpk";
    public const string DefenderSkinPath = "figures/skins/skin099.mpk";
    public const string DullahanSkinPath = "figures/skins/skin106.mpk";
    public const string DefenderNifPath = "figures/Sluaghbinder_ZombieDefender.NIF";
    public const string DullahanNifPath = "figures/Sluaghbinder_Dullahan.NIF";

    private const string DefenderDds = "sluagh_zombie_defender_body.dds";
    private const string DullahanDds = "sluagh_dullahan_body.dds";

    /// <param name="clientAppDirectory">The copied installation's client app directory.</param>
    /// <param name="assetDirectory">Four package files: the two private NIFs and two named DDS atlases.</param>
    /// <returns>Five complete files keyed by paths relative to the client app directory.</returns>
    public static IReadOnlyDictionary<string, byte[]> Build(string clientAppDirectory, string assetDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientAppDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(assetDirectory);

        byte[] defenderNif = ReadRequired(assetDirectory, "Sluaghbinder_ZombieDefender.NIF");
        byte[] dullahanNif = ReadRequired(assetDirectory, "Sluaghbinder_Dullahan.NIF");
        byte[] defenderTexture = ReadRequired(assetDirectory, DefenderDds);
        byte[] dullahanTexture = ReadRequired(assetDirectory, DullahanDds);
        Require(defenderTexture.AsSpan().StartsWith("DDS "u8) && dullahanTexture.AsSpan().StartsWith("DDS "u8),
            "Private pet texture payload must contain two DDS atlases.");
        Require(defenderNif.Length > 1024 && dullahanNif.Length > 1024,
            "Private pet NIF payload is unexpectedly small.");

        Mpak catalog = Mpak.Read(ReadRelative(clientAppDirectory, CatalogPath));
        Mpak bank99 = Mpak.Read(ReadRelative(clientAppDirectory, DefenderSkinPath));
        Mpak bank106 = Mpak.Read(ReadRelative(clientAppDirectory, DullahanSkinPath));

        // Catalog changes are private registrations only. The original Corpse
        // and Headless Corpse rows (and all other client rows) remain byte-for-byte
        // intact. A previously patched private row is replaced, never duplicated.
        catalog.ReplaceEntry("monsters.csv", original => PatchCatalog(original,
        [
            new RowPatch("921", "2494", ["921", "Corpse", "408", "1508"],
                ["2494", "Sluaghbinder Zombie Defender", "987", "7128"]),
            new RowPatch("922", "2495", ["922", "Headless Corpse", "409", "1509"],
                ["2495", "Sluaghbinder Dullahan", "988", "7129"]),
        ]));
        catalog.ReplaceEntry("monnifs.csv", original => PatchCatalog(original,
        [
            new RowPatch("408", "987", ["408", "Corpse", "corpse"],
                ["987", "Sluaghbinder Zombie Defender", "Sluaghbinder_ZombieDefender"]),
            new RowPatch("409", "988", ["409", "Headless Corpse", "corpse_headless"],
                ["988", "Sluaghbinder Dullahan", "Sluaghbinder_Dullahan"]),
        ], fillMonnifHoles: true));
        catalog.ReplaceEntry("skins.csv", original => PatchCatalog(original,
        [
            new RowPatch("1508", "7128", ["1508", "Corpse Body", "corpse_body.tga", "0", "106", "1"],
                ["7128", "Sluaghbinder Zombie Defender Body", DefenderDds, "0", "99", "0"]),
            new RowPatch("1509", "7129", ["1509", "Headless Corpse Body", "corpse_headless_body.tga", "0", "106", "1"],
                ["7129", "Sluaghbinder Dullahan Body", DullahanDds, "0", "106", "1"]),
        ]));

        bank99.UpsertPrivateEntry(DefenderDds, defenderTexture);
        bank106.UpsertPrivateEntry(DullahanDds, dullahanTexture);
        // The old DAoC client resolves these archives by filename order. A
        // technically valid unsorted MPAK can display a magenta pet in-game.
        bank99.SortEntriesByFileName();
        bank106.SortEntriesByFileName();

        byte[] catalogBytes = catalog.WriteAndVerify();
        byte[] bank99Bytes = bank99.WriteAndVerify();
        byte[] bank106Bytes = bank106.WriteAndVerify();
        VerifyMergedCatalog(catalogBytes);
        VerifyMergedSkin(bank99Bytes, DefenderDds, defenderTexture);
        VerifyMergedSkin(bank106Bytes, DullahanDds, dullahanTexture);

        return new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase)
        {
            [CatalogPath] = catalogBytes,
            [DefenderSkinPath] = bank99Bytes,
            [DullahanSkinPath] = bank106Bytes,
            [DefenderNifPath] = defenderNif,
            [DullahanNifPath] = dullahanNif,
        };
    }

    private static byte[] ReadRequired(string directory, string file)
    {
        string path = Path.Combine(directory, file);
        if (!File.Exists(path)) throw new FileNotFoundException($"Sluaghbinder client asset is missing: {file}", path);
        return File.ReadAllBytes(path);
    }

    private static byte[] ReadRelative(string directory, string relative)
    {
        string path = Path.Combine(directory, relative.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(path)) throw new FileNotFoundException($"Required DAoC client archive is missing: {relative}", path);
        return File.ReadAllBytes(path);
    }

    private sealed record RowPatch(string SourceId, string PrivateId, string[] ExpectedSource, string[] PrivatePrefix);
    private sealed record CsvRecord(string Text, string Ending);

    private static byte[] PatchCatalog(byte[] original, IReadOnlyList<RowPatch> patches, bool fillMonnifHoles = false)
    {
        string text = Latin1.GetString(original);
        List<CsvRecord> records = SplitCsvRecords(text);
        string newline = records.FirstOrDefault(r => r.Ending.Length > 0)?.Ending ?? "\r\n";

        foreach (RowPatch patch in patches)
        {
            int sourceIndex = FindUniqueRow(records, patch.SourceId);
            Require(sourceIndex >= 0, $"Client catalog is missing stock row {patch.SourceId}.");
            string sourceText = records[sourceIndex].Text;
            (string[] sourceFields, int prefixEnd) = ReadPrefix(sourceText, patch.ExpectedSource.Length);
            Require(sourceFields.SequenceEqual(patch.ExpectedSource),
                $"Stock client row {patch.SourceId} differs from the supported Classic/Shrouded Isles layout.");
            string privateText = string.Join(',', patch.PrivatePrefix) + ',' + sourceText[prefixEnd..];

            int privateIndex = FindUniqueRow(records, patch.PrivateId);
            if (privateIndex >= 0)
            {
                // A collision with some other client customization must not be
                // silently replaced. The display name makes ownership explicit.
                (string[] existing, _) = ReadPrefix(records[privateIndex].Text, 2);
                Require(existing[1].StartsWith("Sluaghbinder ", StringComparison.OrdinalIgnoreCase),
                    $"Client catalog ID {patch.PrivateId} is already used by a different customization.");
                records[privateIndex] = records[privateIndex] with { Text = privateText };
            }
            else if (fillMonnifHoles)
            {
                // monnifs.csv has fixed blank slots before its MAX MONNIFS row.
                // Reuse a slot in numerical order; appending after the sentinel
                // makes the entry invisible to the legacy loader.
                int beforeSentinel = FindUniqueRow(records, "1000");
                Require(beforeSentinel >= 0, "monnifs.csv is missing its MAX MONNIFS sentinel.");
                int priorId = int.Parse(patch.PrivateId) - 1;
                int prior = FindUniqueRow(records, priorId.ToString());
                Require(prior >= 0 && prior < beforeSentinel, $"monnifs.csv cannot place private row {patch.PrivateId} contiguously.");
                int position = prior + 1;
                Require(position < beforeSentinel && IsBlankCsvRow(records[position].Text),
                    $"monnifs.csv has no blank slot after row {priorId}; refusing to displace another model.");
                records[position] = records[position] with { Text = privateText };
            }
            else
            {
                AppendCsvRow(records, privateText, newline);
            }
        }

        return Latin1.GetBytes(string.Concat(records.Select(r => r.Text + r.Ending)));
    }

    private static bool IsBlankCsvRow(string text) => text.Length > 0 && text.All(c => c is ',' or ' ' or '\t');

    private static void AppendCsvRow(List<CsvRecord> records, string text, string newline)
    {
        if (records.Count > 0 && records[^1].Ending.Length == 0)
            records[^1] = records[^1] with { Ending = newline };
        records.Add(new CsvRecord(text, newline));
    }

    private static int FindUniqueRow(IReadOnlyList<CsvRecord> records, string id)
    {
        int result = -1;
        for (int i = 0; i < records.Count; i++)
        {
            string text = records[i].Text;
            int comma = text.IndexOf(',');
            if (comma != id.Length || !text.AsSpan(0, comma).SequenceEqual(id.AsSpan())) continue;
            Require(result < 0, $"Duplicate client catalog ID {id}.");
            result = i;
        }
        return result;
    }

    // Preserve untouched CSV records as exact Latin-1 text, including header,
    // quoted fields and original line endings. Only the cloned private record
    // is serialized. Most DAoC catalogs have CRLF; this also accepts LF.
    private static List<CsvRecord> SplitCsvRecords(string text)
    {
        var records = new List<CsvRecord>();
        int start = 0;
        bool quoted = false;
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (c == '"')
            {
                if (quoted && i + 1 < text.Length && text[i + 1] == '"') { i++; continue; }
                quoted = !quoted;
            }
            if (quoted || (c != '\r' && c != '\n')) continue;
            string ending = c == '\r' && i + 1 < text.Length && text[i + 1] == '\n' ? "\r\n" : c.ToString();
            records.Add(new CsvRecord(text[start..i], ending));
            if (ending.Length == 2) i++;
            start = i + 1;
        }
        Require(!quoted, "Unterminated quoted field in client catalog.");
        if (start < text.Length) records.Add(new CsvRecord(text[start..], string.Empty));
        return records;
    }

    private static (string[] Fields, int PrefixEnd) ReadPrefix(string row, int count)
    {
        var fields = new List<string>(count);
        var field = new StringBuilder();
        bool quoted = false;
        for (int i = 0; i < row.Length; i++)
        {
            char c = row[i];
            if (c == '"')
            {
                if (quoted && i + 1 < row.Length && row[i + 1] == '"') { field.Append('"'); i++; }
                else quoted = !quoted;
            }
            else if (c == ',' && !quoted)
            {
                fields.Add(field.ToString());
                field.Clear();
                if (fields.Count == count) return (fields.ToArray(), i + 1);
            }
            else field.Append(c);
        }
        throw new InvalidDataException($"Client catalog row has fewer than {count} comma-terminated columns.");
    }

    private static void VerifyMergedCatalog(byte[] content)
    {
        Mpak check = Mpak.Read(content);
        foreach ((string table, string[] ids) in new[]
        {
            ("monsters.csv", new[] { "921", "922", "2494", "2495" }),
            ("monnifs.csv", new[] { "408", "409", "987", "988", "1000" }),
            ("skins.csv", new[] { "1508", "1509", "7128", "7129" }),
        })
        {
            List<CsvRecord> rows = SplitCsvRecords(Latin1.GetString(check.GetEntry(table)));
            foreach (string id in ids) Require(FindUniqueRow(rows, id) >= 0, $"Merged client catalog lost row {table}:{id}.");
        }
    }

    private static void VerifyMergedSkin(byte[] content, string filename, byte[] expected)
    {
        Mpak bank = Mpak.Read(content);
        Require(bank.GetEntry(filename).AsSpan().SequenceEqual(expected), $"Merged skin archive lost {filename}.");
        Require(bank.IsCaseInsensitiveSorted(), "Skin archive directory is not case-insensitively sorted.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidDataException(message);
    }

    /// <summary>Minimal, checked MPAK v2 reader/writer matching the legacy client format.</summary>
    private sealed class Mpak
    {
        private const int MaxExpanded = 128 * 1024 * 1024;
        private sealed record Entry(string Name, byte[] Data, uint Timestamp, uint Flags);
        private readonly byte[] _archiveName;
        private readonly List<Entry> _entries;

        private Mpak(byte[] archiveName, List<Entry> entries)
        {
            _archiveName = archiveName;
            _entries = entries;
        }

        public static Mpak Read(byte[] blob)
        {
            Require(blob.Length >= 21 && blob.AsSpan(0, 5).SequenceEqual("MPAK\x02"u8), "Not a DAoC MPAK v2 archive.");
            Span<byte> header = stackalloc byte[16];
            for (int i = 0; i < 16; i++) header[i] = (byte)(blob[5 + i] ^ i);
            uint directoryCrc = BinaryPrimitives.ReadUInt32LittleEndian(header[..4]);
            uint directorySize = BinaryPrimitives.ReadUInt32LittleEndian(header[4..8]);
            uint nameSize = BinaryPrimitives.ReadUInt32LittleEndian(header[8..12]);
            uint count = BinaryPrimitives.ReadUInt32LittleEndian(header[12..16]);
            Require(count is > 0 and <= 100_000 && directorySize > 0 && nameSize > 0 &&
                    21L + nameSize + directorySize <= blob.Length, "Invalid DAoC MPAK header.");
            int nameStart = 21;
            int directoryStart = checked(nameStart + (int)nameSize);
            int payloadStart = checked(directoryStart + (int)directorySize);
            ReadOnlySpan<byte> compressedDirectory = blob.AsSpan(directoryStart, (int)directorySize);
            Require(Crc32(compressedDirectory) == directoryCrc, "DAoC MPAK directory CRC mismatch.");
            byte[] archiveName = Inflate(blob.AsSpan(nameStart, (int)nameSize), 4096);
            byte[] directory = Inflate(compressedDirectory, checked((int)count * 284));
            Require(directory.Length == checked((int)count * 284), "Invalid DAoC MPAK directory size.");
            var entries = new List<Entry>((int)count);
            var names = new HashSet<string>(FileNameComparer);
            uint expandedOffset = 0;
            uint compressedOffset = 0;
            for (int i = 0; i < (int)count; i++)
            {
                ReadOnlySpan<byte> row = directory.AsSpan(i * 284, 284);
                int nameLength = row[..256].IndexOf((byte)0);
                if (nameLength < 0) nameLength = 256;
                string filename = Latin1.GetString(row[..nameLength]);
                Require(filename.Length > 0 && names.Add(filename), "Duplicate or empty DAoC MPAK filename.");
                uint timestamp = BinaryPrimitives.ReadUInt32LittleEndian(row[256..260]);
                uint flags = BinaryPrimitives.ReadUInt32LittleEndian(row[260..264]);
                uint memory = BinaryPrimitives.ReadUInt32LittleEndian(row[264..268]);
                uint size = BinaryPrimitives.ReadUInt32LittleEndian(row[268..272]);
                uint offset = BinaryPrimitives.ReadUInt32LittleEndian(row[272..276]);
                uint length = BinaryPrimitives.ReadUInt32LittleEndian(row[276..280]);
                uint crc = BinaryPrimitives.ReadUInt32LittleEndian(row[280..284]);
                Require(memory == expandedOffset && offset == compressedOffset &&
                        (long)payloadStart + offset + length <= blob.Length && size <= MaxExpanded,
                    $"Invalid DAoC MPAK offsets for {filename}.");
                ReadOnlySpan<byte> packed = blob.AsSpan(checked(payloadStart + (int)offset), checked((int)length));
                Require(Crc32(packed) == crc, $"DAoC MPAK payload CRC mismatch for {filename}.");
                byte[] data = Inflate(packed, (int)size);
                Require(data.Length == size, $"DAoC MPAK expanded size mismatch for {filename}.");
                entries.Add(new Entry(filename, data, timestamp, flags));
                expandedOffset = checked(expandedOffset + size);
                compressedOffset = checked(compressedOffset + length);
                Require(expandedOffset <= MaxExpanded, "DAoC MPAK expanded image is too large.");
            }
            Require((long)payloadStart + compressedOffset == blob.Length, "Unexpected trailing DAoC MPAK payload.");
            return new Mpak(archiveName, entries);
        }

        public byte[] GetEntry(string filename)
        {
            Entry? entry = _entries.SingleOrDefault(e => FileNameComparer.Equals(e.Name, filename));
            return entry?.Data ?? throw new InvalidDataException($"DAoC MPAK lacks {filename}.");
        }

        public void ReplaceEntry(string filename, Func<byte[], byte[]> transform)
        {
            int index = _entries.FindIndex(e => FileNameComparer.Equals(e.Name, filename));
            Require(index >= 0, $"DAoC client catalog lacks {filename}.");
            _entries[index] = _entries[index] with { Data = transform(_entries[index].Data) };
        }

        public void UpsertPrivateEntry(string filename, byte[] content)
        {
            int index = _entries.FindIndex(e => FileNameComparer.Equals(e.Name, filename));
            if (index >= 0) _entries[index] = _entries[index] with { Name = filename, Data = content };
            else _entries.Add(new Entry(filename, content, 0, 4));
        }

        public void SortEntriesByFileName() => _entries.Sort((a, b) =>
        {
            int folded = StringComparer.OrdinalIgnoreCase.Compare(a.Name, b.Name);
            return folded != 0 ? folded : StringComparer.Ordinal.Compare(a.Name, b.Name);
        });

        public bool IsCaseInsensitiveSorted() =>
            _entries.Zip(_entries.Skip(1)).All(pair =>
                StringComparer.OrdinalIgnoreCase.Compare(pair.First.Name, pair.Second.Name) <= 0);

        public byte[] WriteAndVerify()
        {
            var directory = new MemoryStream();
            var payload = new MemoryStream();
            uint expandedOffset = 0;
            var names = new HashSet<string>(FileNameComparer);
            foreach (Entry entry in _entries)
            {
                byte[] filename = Latin1.GetBytes(entry.Name);
                Require(filename.Length is > 0 and < 256 && !filename.Contains((byte)0) && names.Add(entry.Name),
                    "Invalid or duplicate DAoC MPAK filename.");
                byte[] packed = Deflate(entry.Data);
                byte[] nameField = new byte[256];
                filename.CopyTo(nameField, 0);
                directory.Write(nameField);
                WriteUInt32(directory, entry.Timestamp);
                WriteUInt32(directory, entry.Flags);
                WriteUInt32(directory, expandedOffset);
                WriteUInt32(directory, checked((uint)entry.Data.Length));
                WriteUInt32(directory, checked((uint)payload.Length));
                WriteUInt32(directory, checked((uint)packed.Length));
                WriteUInt32(directory, Crc32(packed));
                payload.Write(packed);
                expandedOffset = checked(expandedOffset + (uint)entry.Data.Length);
                Require(expandedOffset <= MaxExpanded, "DAoC MPAK expanded image is too large.");
            }
            byte[] packedDirectory = Deflate(directory.ToArray());
            byte[] packedName = Deflate(_archiveName);
            var output = new MemoryStream();
            output.Write("MPAK\x02"u8);
            Span<byte> header = stackalloc byte[16];
            BinaryPrimitives.WriteUInt32LittleEndian(header[..4], Crc32(packedDirectory));
            BinaryPrimitives.WriteUInt32LittleEndian(header[4..8], checked((uint)packedDirectory.Length));
            BinaryPrimitives.WriteUInt32LittleEndian(header[8..12], checked((uint)packedName.Length));
            BinaryPrimitives.WriteUInt32LittleEndian(header[12..16], checked((uint)_entries.Count));
            for (int i = 0; i < header.Length; i++) header[i] ^= (byte)i;
            output.Write(header);
            output.Write(packedName);
            output.Write(packedDirectory);
            payload.Position = 0;
            payload.CopyTo(output);
            byte[] written = output.ToArray();
            Mpak roundTrip = Read(written);
            Require(roundTrip._archiveName.AsSpan().SequenceEqual(_archiveName) && roundTrip._entries.Count == _entries.Count,
                "DAoC MPAK round-trip metadata mismatch.");
            for (int i = 0; i < _entries.Count; i++)
                Require(roundTrip._entries[i].Name == _entries[i].Name &&
                        roundTrip._entries[i].Data.AsSpan().SequenceEqual(_entries[i].Data),
                    $"DAoC MPAK round-trip mismatch for {_entries[i].Name}.");
            return written;
        }

        private static byte[] Inflate(ReadOnlySpan<byte> packed, int limit)
        {
            using var input = new MemoryStream(packed.ToArray(), writable: false);
            using var decompressor = new ZLibStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            byte[] buffer = new byte[32 * 1024];
            while (true)
            {
                int read = decompressor.Read(buffer);
                if (read == 0) break;
                Require(output.Length + read <= limit, "DAoC MPAK compressed entry exceeds its size limit.");
                output.Write(buffer, 0, read);
            }
            return output.ToArray();
        }

        private static byte[] Deflate(byte[] data)
        {
            using var output = new MemoryStream();
            using (var compressor = new ZLibStream(output, CompressionLevel.SmallestSize, leaveOpen: true))
                compressor.Write(data);
            return output.ToArray();
        }

        private static void WriteUInt32(Stream stream, uint value)
        {
            Span<byte> bytes = stackalloc byte[4];
            BinaryPrimitives.WriteUInt32LittleEndian(bytes, value);
            stream.Write(bytes);
        }

        private static readonly uint[] CrcTable = BuildCrcTable();

        private static uint Crc32(ReadOnlySpan<byte> data)
        {
            uint crc = 0xffffffff;
            foreach (byte value in data) crc = CrcTable[(int)((crc ^ value) & 0xff)] ^ (crc >> 8);
            return crc ^ 0xffffffff;
        }

        private static uint[] BuildCrcTable()
        {
            var table = new uint[256];
            for (uint i = 0; i < table.Length; i++)
            {
                uint value = i;
                for (int bit = 0; bit < 8; bit++)
                    value = (value & 1) != 0 ? (value >> 1) ^ 0xedb88320 : value >> 1;
                table[i] = value;
            }
            return table;
        }
    }
}
