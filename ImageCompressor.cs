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

namespace DotNETImageResizer;

public partial class ImageCompressor
{
    public List<CompressionResult> RunBenchmarksAsync(string inputFolderPath)
    {
        var results = new List<CompressionResult>();

        // Get all supported image files
        var supportedExtensions = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".webp" };
        var imageFiles = Directory.GetFiles(inputFolderPath)
            .Where(file => supportedExtensions.Contains(Path.GetExtension(file).ToLower()))
            .ToList();
        const int expectedSize = 512;
        foreach (var inputFilePath in imageFiles)
        {
            string inputExtension = Path.GetExtension(inputFilePath).ToLower();
            string outputFormat = inputExtension.TrimStart('.');
            results.Add(CompressWithImageSharp(inputFilePath, expectedSize, outputFormat));
            results.Add(CompressWithMagickNet(inputFilePath, expectedSize, outputFormat));
            results.Add(CompressWithSkiaSharp(inputFilePath, expectedSize, outputFormat));
        }

        return results;
    }

    private CompressionResult CompressWithImageSharp(string inputPath, int targetSize, string format)
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

        // Compress with quality control
        var outputPath = $"output_imagesharp_{Path.GetFileNameWithoutExtension(inputPath)}_{targetSize}.{format}";

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
    }

    private CompressionResult CompressWithMagickNet(string inputPath, int targetSize, string format)
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
    }

    private CompressionResult CompressWithSkiaSharp(string inputPath, int targetSize, string format)
    {

        var stopwatch = Stopwatch.StartNew();
        var initialMemory = GC.GetTotalMemory(true);
        var inputFileInfo = new FileInfo(inputPath);

        using var originalBitmap = SKBitmap.Decode(inputPath);
        var resizedBitmap = originalBitmap.Resize(
            new SKImageInfo(targetSize, targetSize),
            new SKSamplingOptions(SKFilterMode.Nearest)
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
    }
}