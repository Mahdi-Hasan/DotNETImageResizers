namespace DotNETImageResizer;

public class ReportGenerator
{
    public void GenerateReport(CompressionResult[] results, string outputPath)
    {
        // Generate TSV file
        var lines = new List<string>
        {
            // Header
            "FileName\tLibrary\tFormat\tCompressionTime(ms)\tMemoryUsed(KB)\tInputFileSize(KB)\tOutputFileSize(KB)"
        };

        // Add data rows with sizes in KB
        foreach (var result in results)
        {
            lines.Add($"{result.FileName}\t{result.LibraryName}\t{result.Format}\t{result.CompressionTimeMs}\t{result.MemoryUsedBytes / 1024.0:F2}\t{result.InputFileSizeBytes / 1024.0:F2}\t{result.OutputFileSizeBytes / 1024.0:F2}");
        }
        // Write to file
        var fileName = $"compression_report_{DateTime.Now:dd-MMM_HH-mm}.tsv";
        File.WriteAllLines(Path.Combine(outputPath, fileName), lines);
        Console.WriteLine($"Report generated: {outputPath}");
    }
}