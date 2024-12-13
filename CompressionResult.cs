namespace DotNETImageResizer;
public class CompressionResult
{
    public string LibraryName { get; set; }
    public long CompressionTimeMs { get; set; }
    public long MemoryUsedBytes { get; set; }
    public long OutputFileSizeBytes { get; set; }
}