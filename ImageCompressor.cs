using ImageMagick;
using SixLabors.ImageSharp.Formats.Jpeg;
using System.Diagnostics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Bmp;
using SkiaSharp;
using System.Text;

namespace DotNETImageResizer;
public class ImageCompressor
{
    public readonly int Quality = 75;
    public async Task<CompressionResult[]> RunBenchmarksAsync(string inputFolderPath, string outputFilesPath, int expectedSize)
    {
        // Get all supported image files
        var supportedExtensions = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".webp" };
        var imageFiles = Directory.GetFiles(inputFolderPath)
            .Where(file => supportedExtensions.Contains(Path.GetExtension(file).ToLower()))
            .ToList();
        var totalResults = 3 * imageFiles.Count + 1;
        CompressionResult[] results = new CompressionResult[totalResults];

        await Parallel.ForEachAsync(imageFiles, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, async (inputFilePath, cancellationToken) =>
        {
            try
            {
                string outputFormat = Path.GetExtension(inputFilePath).ToLower().TrimStart('.');

                var compressionTasks = new[]
                {
                    CompressWithImageSharp(inputFilePath, expectedSize, outputFormat, outputFilesPath),
                    CompressWithMagickNet(inputFilePath, expectedSize, outputFormat, outputFilesPath),
                    CompressWithSkiaSharp(inputFilePath, expectedSize, outputFormat, outputFilesPath)
                };

                results = await Task.WhenAll(compressionTasks);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing {inputFilePath}: {ex.Message}");
            }
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
            _ => throw new ArgumentException($"Unsupported format: {format}")
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
        image.Format = extension switch
        {
            ".jpeg" or ".jpg" => MagickFormat.Jpeg,
            ".png" => MagickFormat.Png,
            ".webp" => MagickFormat.WebP,
            ".bmp" => MagickFormat.Bmp,
            _ => throw new NotSupportedException($"Unsupported image format: {extension}")
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

        await using var originalStream = File.OpenRead(inputPath);
        using var originalBitmap = SKBitmap.Decode(originalStream);
        SKEncodedImageFormat encodedFormat = format.ToLower() switch
        {
            ".jpg" or ".jpeg" => SKEncodedImageFormat.Jpeg,
            ".png" => SKEncodedImageFormat.Png,
            ".webp" => SKEncodedImageFormat.Webp,
            ".bmp" => SKEncodedImageFormat.Bmp,
            _ => throw new ArgumentException($"Unsupported output file format: {format}")
        };

        var outputPath = Path.Combine(outputFilesPath, $"{Path.GetFileNameWithoutExtension(inputPath)}_skia{format}");

        SKData encodedData;
        using (var encodedImage = SKImage.FromBitmap(originalBitmap))
        {
            encodedData = encodedFormat switch
            {
                SKEncodedImageFormat.Jpeg => encodedImage.Encode(encodedFormat, Quality),
                SKEncodedImageFormat.Png => encodedImage.Encode(encodedFormat, Quality),
                SKEncodedImageFormat.Webp => encodedImage.Encode(encodedFormat, Quality),
                SKEncodedImageFormat.Bmp => encodedImage.Encode(encodedFormat, 100),
                _ => throw new ArgumentException($"Unsupported output file format: {format}")
            };
        }

        await File.WriteAllBytesAsync(outputPath, encodedData.ToArray());

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
