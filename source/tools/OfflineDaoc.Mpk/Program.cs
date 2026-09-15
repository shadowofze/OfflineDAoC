using DOL.MPK;

if (args.Length == 2)
{
    var archivePath = Path.GetFullPath(args[0]);
    var outputDirectory = Path.GetFullPath(args[1]);
    if (!File.Exists(archivePath))
    {
        Console.Error.WriteLine($"Archive does not exist: {archivePath}");
        return 3;
    }

    Directory.CreateDirectory(outputDirectory);
    var archive = new MpkHandler(archivePath, create: false);
    archive.Extract(outputDirectory);
    Console.WriteLine($"Extracted {archive.Count} files from {archivePath} to {outputDirectory} (internal name: {archive.Name})");
    return 0;
}

if ((args.Length == 3 || args.Length == 4) && args[0].Equals("pack", StringComparison.OrdinalIgnoreCase))
{
    var inputDirectory = Path.GetFullPath(args[1]);
    var archivePath = Path.GetFullPath(args[2]);
    var internalArchiveName = args.Length == 4 ? args[3] : Path.GetFileName(archivePath);
    if (!Directory.Exists(inputDirectory))
    {
        Console.Error.WriteLine($"Input directory does not exist: {inputDirectory}");
        return 4;
    }

    if (string.IsNullOrWhiteSpace(internalArchiveName) ||
        !string.Equals(Path.GetFileName(internalArchiveName), internalArchiveName, StringComparison.Ordinal))
    {
        Console.Error.WriteLine($"Internal archive name must be a filename, not a path: {internalArchiveName}");
        return 5;
    }

    var archive = new MpkHandler(internalArchiveName, create: true);
    foreach (var path in Directory.EnumerateFiles(inputDirectory, "*", SearchOption.TopDirectoryOnly).Order())
    {
        var file = new MpkFile(path);
        file.Header.Name = Path.GetFileName(path).ToLowerInvariant();
        if (!archive.AddFile(file)) throw new InvalidOperationException($"Duplicate MPK entry: {file.Header.Name}");
    }
    archive.Write(archivePath);
    Console.WriteLine($"Packed {archive.Count} files from {inputDirectory} into {archivePath}");
    return 0;
}

Console.Error.WriteLine("Usage: OfflineDaoc.Mpk <archive.mpk> <output-directory>");
Console.Error.WriteLine("   or: OfflineDaoc.Mpk pack <input-directory> <archive.mpk> [internal-archive-name]");
return 2;
