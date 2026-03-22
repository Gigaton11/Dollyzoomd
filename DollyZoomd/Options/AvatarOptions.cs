namespace DollyZoomd.Options;

public class AvatarOptions
{
    public const string SectionName = "Avatar";

    // Local filesystem storage (deprecated for production, kept for Development)
    public string StoragePath { get; set; } = "uploads/avatars";
    
    // Cloud Storage configuration
    public bool UseCloudStorage { get; set; } = false;
    public string? CloudStorageBucket { get; set; }
    public string? CloudStorageProjectId { get; set; }
    
    // Image processing settings (applies to both local and cloud storage)
    public long MaxAvatarSizeBytes { get; set; } = 8 * 1024 * 1024;
    public int ResizeMaxSidePixels { get; set; } = 512;
    public int JpegQuality { get; set; } = 85;
    public int MaxInputDimensionPixels { get; set; } = 4096;
    public int MinInputDimensionPixels { get; set; } = 32;
}
