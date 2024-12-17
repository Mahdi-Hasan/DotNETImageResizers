namespace DotNETImageResizer;

public class ReportGenerator
{
    public void GenerateReport(List<CompressionResult> results, string outputPath)
    {
        // Generate TSV file
        var lines = new List<string>
        {
            // Header
            "FileName\tLibrary\tFormat\tCompressionTime(ms)\tMemoryUsed(KB)\tInputFileSize(KB)\tOutputFileSize(KB)"
        };

        // Add data rows with sizes in KB
        lines.AddRange(results.Select(r =>
            $"{r.FileName}\t{r.LibraryName}\t{r.Format}\t{r.CompressionTimeMs}\t{r.MemoryUsedBytes / 1024.0:F2}\t{r.InputFileSizeBytes / 1024.0:F2}\t{r.OutputFileSizeBytes / 1024.0:F2}"
        ));


        // Write to file
        var fileName = $"compression_report_{DateTime.Now:dd-MMM_HH-mm-G}.tsv";
        File.WriteAllLines(Path.Combine(outputPath, fileName), lines);
        Console.WriteLine($"Report generated: {outputPath}");
    }
}