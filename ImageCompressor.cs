using ImageMagick;
using SixLabors.ImageSharp.Formats.Jpeg;
using SkiaSharp;
using System.Diagnostics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Formats;

namespace DotNETImageResizer;


public class ImageCompressor
{
    private static readonly int[] IMAGE_SIZES = new[] { 100, 500, 1000, 2000 };
    private static readonly string[] IMAGE_FORMATS = new[] { "jpg", "png", "webp" };

    public async Task<List<CompressionResult>> RunBenchmarksAsync(string inputFolderPath)
    {
        var results = new List<CompressionResult>();

        // Get all supported image files
        var supportedExtensions = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".webp" };
        var imageFiles = Directory.GetFiles(inputFolderPath)
            .Where(file => supportedExtensions.Contains(Path.GetExtension(file).ToLower()))
            .ToList();

        foreach (var inputPath in imageFiles)
        {
            foreach (var size in IMAGE_SIZES)
            {
                foreach (var format in IMAGE_FORMATS)
                {
                    results.Add(await CompressWithImageSharp(inputPath, size, format));
                    results.Add(await CompressWithMagickNet(inputPath, size, format));
                    results.Add(await CompressWithSkiaSharp(inputPath, size, format));
                }
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
            var inputFileInfo = new FileInfo(inputPath);

            using var image = SixLabors.ImageSharp.Image.Load(inputPath);

            // Resize
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new SixLabors.ImageSharp.Size(targetSize, targetSize),
                Mode = ResizeMode.Max
            }));

            // Compress with quality control
            var outputPath = $"output_imagesharp_{Path.GetFileNameWithoutExtension(inputPath)}_{targetSize}.{format}";

            // Determine encoder based on format
            IImageEncoder encoder = format switch
            {
                "jpg" => new JpegEncoder { Quality = 75 },
                "png" => new PngEncoder { CompressionLevel = PngCompressionLevel.Level5 },
                "webp" => new WebpEncoder { Quality = 75 },
                _ => throw new ArgumentException($"Unsupported format: {format}")
            };

            // Save with appropriate encoder
            image.Save(outputPath, encoder);

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
        });
    }

    private async Task<CompressionResult> CompressWithMagickNet(string inputPath, int targetSize, string format)
    {
        return await Task.Run(() =>
        {
            var stopwatch = Stopwatch.StartNew();
            var initialMemory = GC.GetTotalMemory(true);
            var inputFileInfo = new FileInfo(inputPath);

            using var image = new MagickImage(inputPath);

            // Resize
            image.Resize(new Percentage(targetSize));

            // Compress
            var outputPath = $"output_magicknet_{Path.GetFileNameWithoutExtension(inputPath)}_{targetSize}.{format}";
            image.Quality = 75;
            image.Write(outputPath);

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
        });
    }

    private async Task<CompressionResult> CompressWithSkiaSharp(string inputPath, int targetSize, string format)
    {
        return await Task.Run(() =>
        {
            var stopwatch = Stopwatch.StartNew();
            var initialMemory = GC.GetTotalMemory(true);
            var inputFileInfo = new FileInfo(inputPath);

            using var originalBitmap = SKBitmap.Decode(inputPath);
            var resizedBitmap = originalBitmap.Resize(
                new SKImageInfo(targetSize, targetSize),
                SKFilterQuality.Medium
            );

            var outputPath = $"output_skia_{Path.GetFileNameWithoutExtension(inputPath)}_{targetSize}.{format}";

            using var image = SKImage.FromBitmap(resizedBitmap);
            using var data = image.Encode(SKEncodedImageFormat.Jpeg, 75);
            File.WriteAllBytes(outputPath, data.ToArray());

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
        });
    }

    public void GenerateReport(List<CompressionResult> results, string outputPath)
    {
        // Generate TSV file
        var lines = new List<string>
            {
                // Header
                "FileName\tLibrary\tTargetSize\tFormat\tCompressionTime(ms)\tMemoryUsed(bytes)\tInputFileSize(bytes)\tOutputFileSize(bytes)"
            };

        // Add data rows
        lines.AddRange(results.Select(r =>
            $"{r.FileName}\t{r.LibraryName}\t{r.TargetSize}\t{r.Format}\t{r.CompressionTimeMs}\t{r.MemoryUsedBytes}\t{r.InputFileSizeBytes}\t{r.OutputFileSizeBytes}"
        ));

        // Write to file
        File.WriteAllLines(outputPath, lines);
        Console.WriteLine($"Report generated: {outputPath}");
    }
}