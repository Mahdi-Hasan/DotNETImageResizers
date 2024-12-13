using ImageMagick;
using SixLabors.ImageSharp.Formats.Jpeg;
using SkiaSharp;
using System.Diagnostics;

namespace DotNETImageResizer;

public class ImageCompressor
{
    private const int ITERATIONS = 5;
    private const int[] IMAGE_SIZES = { 100, 500, 1000, 2000 };
    private static readonly string[] IMAGE_FORMATS = { "jpg", "png", "webp" };

    public async Task<List<CompressionResult>> RunBenchmarksAsync(string inputImagePath)
    {
        var results = new List<CompressionResult>();

        foreach (var size in IMAGE_SIZES)
        {
            foreach (var format in IMAGE_FORMATS)
            {
                results.Add(await CompressWithImageSharp(inputImagePath, size, format));
                results.Add(await CompressWithMagickNet(inputImagePath, size, format));
                results.Add(await CompressWithSkiaSharp(inputImagePath, size, format));
            }
        }

        return results;
    }

    private async Task<CompressionResult> CompressWithImageSharp(string inputPath, int targetSize, string format)
    {
        return await Task.Run(() =>
        {
            var stopwatch = Stopwatch.StartNew();
            var initialMemory = GC.GetTotalMemory(true);

            using var image = Image.Load(inputPath);

            // Resize
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(targetSize),
                Mode = ResizeMode.Max
            }));

            // Compress with quality control
            var outputPath = $"output_imagesharp_{targetSize}.{format}";

            using var outputStream = new MemoryStream();
            image.Save(outputStream, format == "jpg" ? new JpegEncoder { Quality = 75 } : null);

            stopwatch.Stop();

            return new CompressionResult
            {
                LibraryName = "ImageSharp",
                CompressionTimeMs = stopwatch.ElapsedMilliseconds,
                MemoryUsedBytes = GC.GetTotalMemory(true) - initialMemory,
                OutputFileSizeBytes = outputStream.Length
            };
        });
    }

    private async Task<CompressionResult> CompressWithMagickNet(string inputPath, int targetSize, string format)
    {
        return await Task.Run(() =>
        {
            var stopwatch = Stopwatch.StartNew();
            var initialMemory = GC.GetTotalMemory(true);

            using var image = new MagickImage(inputPath);

            // Resize
            image.Resize(targetSize, targetSize);

            // Compress
            var outputPath = $"output_magicknet_{targetSize}.{format}";
            image.Quality = 75;
            image.Write(outputPath);

            stopwatch.Stop();

            return new CompressionResult
            {
                LibraryName = "MagickNet",
                CompressionTimeMs = stopwatch.ElapsedMilliseconds,
                MemoryUsedBytes = GC.GetTotalMemory(true) - initialMemory,
                OutputFileSizeBytes = new FileInfo(outputPath).Length
            };
        });
    }

    private async Task<CompressionResult> CompressWithSkiaSharp(string inputPath, int targetSize, string format)
    {
        return await Task.Run(() =>
        {
            var stopwatch = Stopwatch.StartNew();
            var initialMemory = GC.GetTotalMemory(true);

            using var originalBitmap = SKBitmap.Decode(inputPath);
            var resizedBitmap = originalBitmap.Resize(
                new SKImageInfo(targetSize, targetSize),
                SKFilterQuality.Medium
            );

            var outputPath = $"output_skia_{targetSize}.{format}";

            using var image = SKImage.FromBitmap(resizedBitmap);
            using var data = image.Encode(SKEncodedImageFormat.Jpeg, 75);
            File.WriteAllBytes(outputPath, data.ToArray());

            stopwatch.Stop();

            return new CompressionResult
            {
                LibraryName = "SkiaSharp",
                CompressionTimeMs = stopwatch.ElapsedMilliseconds,
                MemoryUsedBytes = GC.GetTotalMemory(true) - initialMemory,
                OutputFileSizeBytes = new FileInfo(outputPath).Length
            };
        });
    }

    public void GenerateReport(List<CompressionResult> results)
    {
        Console.WriteLine("Image Compression Benchmark Results:");
        foreach (var result in results)
        {
            Console.WriteLine($"Library: {result.LibraryName}");
            Console.WriteLine($"Compression Time: {result.CompressionTimeMs}ms");
            Console.WriteLine($"Memory Used: {result.MemoryUsedBytes / 1024}KB");
            Console.WriteLine($"Output File Size: {result.OutputFileSizeBytes / 1024}KB");
            Console.WriteLine("---");
        }
    }
}