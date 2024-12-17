using ImageMagick;
using SixLabors.ImageSharp.Formats.Jpeg;
using SkiaSharp;
using System.Diagnostics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Formats;
using System.Drawing;
using SixLabors.ImageSharp.Formats.Bmp;
using System.Collections.Concurrent;

namespace DotNETImageResizer;

public class ImageCompressor
{
    public async Task<CompressionResult[]> RunBenchmarksAsync(string inputFolderPath, string outputFilesPath)
    {
        // Get all supported image files
        var supportedExtensions = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".webp" };
        var imageFiles = Directory.GetFiles(inputFolderPath)
            .Where(file => supportedExtensions.Contains(Path.GetExtension(file).ToLower()))
            .ToList();
        const int expectedSize = 512;
        CompressionResult[] results;

        await Parallel.ForEachAsync(imageFiles, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, async (inputFilePath, cancellationToken) =>
        {
            try
            {
                string outputFormat = Path.GetExtension(inputFilePath).ToLower().TrimStart('.');

                // Define compression tasks
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
                // Log the error or handle it appropriately
                Console.WriteLine($"Error processing {inputFilePath}: {ex.Message}");
            }
        });

        return results;
    }

    private async Task<CompressionResult> CompressWithImageSharp(string inputPath, int targetSize, string format ,string outputFilesPath)
    {
        var stopwatch = Stopwatch.StartNew();
        var initialMemory = GC.GetTotalMemory(true);
        var inputFileInfo = new FileInfo(inputPath);

        using var image = Image.Load(inputPath);

        // Resize
        image.Mutate(x => x.Resize(new ResizeOptions
        {
            Size = new SixLabors.ImageSharp.Size(targetSize, targetSize),
            Mode = ResizeMode.Max
        }));

        var outputPath = $"{outputFilesPath}_output_imagesharp_{Path.GetFileNameWithoutExtension(inputPath)}.{format}";

        // Define supported encoders based on the format
        IImageEncoder encoder = format.ToLower() switch
        {
            "jpg" or "jpeg" => new JpegEncoder { Quality = 75 }, // JPEG Encoder
            "png" => new PngEncoder { CompressionLevel = PngCompressionLevel.Level5 },
            "bmp" => new BmpEncoder(), // BMP Encoder (default settings)
            "webp" => new WebpEncoder { Quality = 75 },
            _ => throw new ArgumentException($"Unsupported format: {format}")
        };

        // Save with appropriate encoder
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

        // Resize
        image.Resize(new Percentage(targetSize));
        var outputPath = $"{outputFilesPath}_output_magicknet_{Path.GetFileNameWithoutExtension(inputPath)}.{format}";
        image.Quality = 75;
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

        using var originalBitmap = SKBitmap.Decode(inputPath);
        var resizedBitmap = originalBitmap.Resize(
            new SKImageInfo(targetSize, targetSize),
            new SKSamplingOptions(SKFilterMode.Nearest)
        );

        var outputPath = $"{outputFilesPath}_output_skia_{Path.GetFileNameWithoutExtension(inputPath)}.{format}";

        using var image = SKImage.FromBitmap(resizedBitmap);
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, 75);
        await File.WriteAllBytesAsync(outputPath, data.ToArray());

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