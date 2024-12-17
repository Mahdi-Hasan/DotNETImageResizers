using System.Collections.Concurrent;
using ImageMagick;
using SixLabors.ImageSharp.Formats.Jpeg;
using System.Diagnostics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Bmp;
using SkiaSharp;

namespace DotNETImageResizer;
public class ImageCompressor
{
    public readonly int Quality = 30;
    public async Task<ConcurrentBag<CompressionResult>> RunBenchmarksAsync(string inputFolderPath, string outputFilesPath, int expectedSize)
    {
        // Get all supported image files
        var supportedExtensions = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".webp" };
        var imageFiles = Directory.GetFiles(inputFolderPath)
            .Where(file => supportedExtensions.Contains(Path.GetExtension(file).ToLower()))
            .ToList();
        var results = new ConcurrentBag<CompressionResult>();

        await Parallel.ForEachAsync(imageFiles, async (inputFilePath, cancellationToken) =>
        {
            string inputExtension = Path.GetExtension(inputFilePath).ToLower();
            string outputFormat = inputExtension.TrimStart('.');

            // Add results using thread-safe collection
            var imageSharpResult = await CompressWithImageSharp(inputFilePath, expectedSize, outputFormat, outputFilesPath);
            results.Add(imageSharpResult);

            var magickNetResult = await CompressWithMagickNet(inputFilePath, expectedSize, outputFormat, outputFilesPath);
            results.Add(magickNetResult);

            var skiaSharpResult = await CompressWithSkiaSharp(inputFilePath, expectedSize, outputFormat, outputFilesPath);
            results.Add(skiaSharpResult);
        });

        return results;
    }

    private async Task<CompressionResult> CompressWithImageSharp(string inputPath, int targetSize, string format, string outputFilesPath)
    {
        var stopwatch = Stopwatch.StartNew();
        var initialMemory = GC.GetTotalMemory(true);
        var inputFileInfo = new FileInfo(inputPath);

        using var image = await Image.LoadAsync(inputPath);

        var outputPath = $"{outputFilesPath}{Path.GetFileNameWithoutExtension(inputPath)}_ImageSharp.{format}";
        IImageEncoder encoder = format.ToLower() switch
        {
            "jpg" or "jpeg" => new JpegEncoder { Quality = Quality },
            "png" => new PngEncoder { CompressionLevel = PngCompressionLevel.Level5 },
            "bmp" => new BmpEncoder(),
            "webp" => new WebpEncoder { Quality = Quality },
            _ => throw new ArgumentException($"Unsupported format Image Sharp: {format}")
        };

        await image.SaveAsync(outputPath, encoder);
        stopwatch.Stop();

        var outputFileInfo = new FileInfo(outputPath);

        return new CompressionResult
        {
            FileName = Path.GetFileName(inputPath),
            LibraryName = "ImageSharp",
            TargetSize = targetSize,
            Format = format,
            CompressionTimeMs = stopwatch.ElapsedMilliseconds,
            MemoryUsedBytes = GC.GetTotalMemory(true) - initialMemory,
            InputFileSizeBytes = inputFileInfo.Length,
            OutputFileSizeBytes = outputFileInfo.Length
        };
    }

    private async Task<CompressionResult> CompressWithMagickNet(string inputPath, int targetSize, string format, string outputFilesPath)
    {
        var stopwatch = Stopwatch.StartNew();
        var initialMemory = GC.GetTotalMemory(true);
        var inputFileInfo = new FileInfo(inputPath);

        using var image = new MagickImage(inputPath);

        var outputPath = $"{outputFilesPath}{Path.GetFileNameWithoutExtension(inputPath)}_Magicknet.{format}";
        image.Quality = (uint)Quality;

        // More precise color depth handling
        image.Depth = Math.Min(image.Depth, 8);

        // Optimize compression based on file type
        var extension = Path.GetExtension(inputPath).ToLower();
        image.Format = format switch
        {
            "jpeg" or "jpg" => MagickFormat.Jpeg,
            "png" => MagickFormat.Png,
            "webp" => MagickFormat.WebP,
            "bmp" => MagickFormat.Bmp,
            _ => throw new NotSupportedException($"Unsupported image format Magic: {extension}")
        };
        image.Strip();
        await image.WriteAsync(outputPath);
        stopwatch.Stop();
        var outputFileInfo = new FileInfo(outputPath);

        return new CompressionResult
        {
            FileName = Path.GetFileName(inputPath),
            LibraryName = "MagickNet",
            TargetSize = targetSize,
            Format = format,
            CompressionTimeMs = stopwatch.ElapsedMilliseconds,
            MemoryUsedBytes = GC.GetTotalMemory(true) - initialMemory,
            InputFileSizeBytes = inputFileInfo.Length,
            OutputFileSizeBytes = outputFileInfo.Length
        };
    }

    private async Task<CompressionResult> CompressWithSkiaSharp(string inputPath, int targetSize, string format, string outputFilesPath)
    {
        var stopwatch = Stopwatch.StartNew();
        var initialMemory = GC.GetTotalMemory(true);
        var inputFileInfo = new FileInfo(inputPath);
        SKEncodedImageFormat encodedFormat = format.ToLower() switch
        {
            "jpg" or "jpeg" or "bmp" => SKEncodedImageFormat.Jpeg,
            "png" => SKEncodedImageFormat.Png,
            "webp" => SKEncodedImageFormat.Webp,
            _ => throw new ArgumentException($"Unsupported output file format Skia encode: {format}")
        };
        var outputPath = Path.Combine(outputFilesPath, $"{Path.GetFileNameWithoutExtension(inputPath)}_skia.{format}");
        // Read the original image
        byte[] imageData;
        using (var originalBitmap = SKBitmap.Decode(inputPath))
        {
            using (var encodedImage = SKImage.FromBitmap(originalBitmap))
            {
                using (var encodedData = encodedImage.Encode(encodedFormat, Quality))
                {
                    imageData = encodedData.ToArray();
                }
            }
        }

        // Use FileStream with explicit file sharing
        await using (var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await fileStream.WriteAsync(imageData, 0, imageData.Length);
        }
        stopwatch.Stop();
        var outputFileInfo = new FileInfo(outputPath);

        return new CompressionResult
        {
            FileName = Path.GetFileName(inputPath),
            LibraryName = "SkiaSharp",
            TargetSize = targetSize,
            Format = format,
            CompressionTimeMs = stopwatch.ElapsedMilliseconds,
            MemoryUsedBytes = GC.GetTotalMemory(true) - initialMemory,
            InputFileSizeBytes = inputFileInfo.Length,
            OutputFileSizeBytes = outputFileInfo.Length
        };
    }
}
