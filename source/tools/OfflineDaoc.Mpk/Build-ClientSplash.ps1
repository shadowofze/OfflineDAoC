param(
    [Parameter(Mandatory = $true)]
    [string] $SourceImage,

    [Parameter(Mandatory = $true)]
    [string] $OutputMpk
)

$ErrorActionPreference = 'Stop'

$sourcePath = [System.IO.Path]::GetFullPath($SourceImage)
$outputPath = [System.IO.Path]::GetFullPath($OutputMpk)
$toolPath = Join-Path $PSScriptRoot 'bin\Release\net10.0\OfflineDaoc.Mpk.exe'
if (-not (Test-Path -LiteralPath $sourcePath -PathType Leaf)) {
    throw "Splash artwork does not exist: $sourcePath"
}
if (-not (Test-Path -LiteralPath $toolPath -PathType Leaf)) {
    throw "Build OfflineDaoc.Mpk in Release first: $toolPath"
}

# DAoC's pregame renderer expects the legacy, uncompressed true-color TGA
# variant (image type 2). RLE-compressed TGAs are accepted by modern image
# tools but render as black in this client.
Add-Type -AssemblyName System.Drawing.Common
$drawingAssembly = [System.Drawing.Bitmap].Assembly.Location
$drawingPrimitivesAssembly = [System.Drawing.RectangleF].Assembly.Location
$windowsDrawingAssemblies = Get-ChildItem -LiteralPath (Split-Path -Parent $drawingAssembly) `
    -Filter 'System.Private.Windows*.dll' | Select-Object -ExpandProperty FullName
$drawingReferences = @($drawingAssembly, $drawingPrimitivesAssembly) + @($windowsDrawingAssemblies)
Add-Type -ReferencedAssemblies $drawingReferences -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

public static class DaocSplashTgaWriter
{
    public static void Write(string sourcePath, string destinationPath)
    {
        const int width = 1024;
        const int height = 768;
        using var source = new Bitmap(sourcePath);
        using var target = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        using (Graphics graphics = Graphics.FromImage(target))
        {
            graphics.CompositingMode = CompositingMode.SourceCopy;
            graphics.CompositingQuality = CompositingQuality.HighQuality;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.DrawImage(source, 0, 0, width, height);
        }

        using var stream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);
        using var writer = new BinaryWriter(stream);
        writer.Write((byte)0); // ID length
        writer.Write((byte)0); // no color map
        writer.Write((byte)2); // uncompressed true-color image
        writer.Write(new byte[5]);
        writer.Write((ushort)0); // x origin
        writer.Write((ushort)0); // y origin
        writer.Write((ushort)width);
        writer.Write((ushort)height);
        writer.Write((byte)32);
        writer.Write((byte)8); // 8 alpha bits, bottom-left origin (matches stock DAoC)

        // TGA stores BGRA pixels. The stock splash files use a bottom-left
        // origin, so emit the image from its bottom scanline upward.
        for (int y = height - 1; y >= 0; y--)
        {
            for (int x = 0; x < width; x++)
            {
                Color pixel = target.GetPixel(x, y);
                writer.Write(pixel.B);
                writer.Write(pixel.G);
                writer.Write(pixel.R);
                writer.Write(pixel.A);
            }
        }

        writer.Write(0u); // extension-area offset
        writer.Write(0u); // developer-directory offset
        writer.Write(System.Text.Encoding.ASCII.GetBytes("TRUEVISION-XFILE."));
        writer.Write((byte)0);
    }
}
'@

$tempBase = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath())
$tempRoot = Join-Path $tempBase ('offline-daoc-splash-build-' + [guid]::NewGuid().ToString('N'))
$resolvedTemp = [System.IO.Path]::GetFullPath($tempRoot)
if (-not $resolvedTemp.StartsWith($tempBase, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Refusing unsafe temporary path: $resolvedTemp"
}

try {
    New-Item -ItemType Directory -Path $resolvedTemp | Out-Null
    $firstSplash = Join-Path $resolvedTemp 'splash1.tga'
    [DaocSplashTgaWriter]::Write($sourcePath, $firstSplash)
    foreach ($index in 2..8) {
        Copy-Item -LiteralPath $firstSplash -Destination (Join-Path $resolvedTemp "splash$index.tga")
    }

    $outputDirectory = Split-Path -Parent $outputPath
    if ($outputDirectory) {
        New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
    }
    # The physical asset keeps its descriptive build filename, but DAoC binds
    # this archive by its legacy internal name.
    & $toolPath pack $resolvedTemp $outputPath 'splash.mpk'
    if ($LASTEXITCODE -ne 0) {
        throw "OfflineDaoc.Mpk failed with exit code $LASTEXITCODE"
    }
}
finally {
    if (Test-Path -LiteralPath $resolvedTemp) {
        Remove-Item -LiteralPath $resolvedTemp -Recurse -Force
    }
}
