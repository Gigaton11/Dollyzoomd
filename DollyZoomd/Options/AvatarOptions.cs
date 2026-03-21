namespace DollyZoomd.Options;

public class AvatarOptions
{
    public const string SectionName = "Avatar";

    public string StoragePath { get; set; } = "uploads/avatars";
    public long MaxAvatarSizeBytes { get; set; } = 8 * 1024 * 1024;
    public int ResizeMaxSidePixels { get; set; } = 512;
    public int JpegQuality { get; set; } = 85;
    public int MaxInputDimensionPixels { get; set; } = 4096;
    public int MinInputDimensionPixels { get; set; } = 32;
}
