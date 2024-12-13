namespace DotNETImageResizer;
public class CompressionResult
{
    public string FileName { get; set; }
    public string LibraryName { get; set; }
    public int TargetSize { get; set; }
    public string Format { get; set; }
    public long CompressionTimeMs { get; set; }
    public long MemoryUsedBytes { get; set; }
    public long InputFileSizeBytes { get; set; }
    public long OutputFileSizeBytes { get; set; }
}