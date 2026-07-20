namespace VideoAudioMerger.Models;

public class MediaFileInfo
{
    public string FilePath { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public DateTime CreatedDate { get; set; }
    public bool HasVideo { get; set; }
    public bool HasAudio { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public double? DurationSeconds { get; set; }
    
    public bool IsVideoFile => HasVideo && !HasAudio;
    public bool IsAudioFile => HasAudio && !HasVideo;
    public bool IsFullyMerged => HasVideo && HasAudio;
}
