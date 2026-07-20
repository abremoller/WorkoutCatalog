namespace WorkoutCatalog.Models;

public class FolderScanResult
{
    public string FolderName { get; set; } = string.Empty;
    public string FolderPath { get; set; } = string.Empty;
    public string? DescriptionText { get; set; }
    public string? DescriptionFileName { get; set; }
    public string? ImageFileName { get; set; }
    public string? ThumbnailFileName { get; set; }
    public string? PreviewVideoFileName { get; set; }
    public string? FullVideoFileName { get; set; }
    public double? PreviewDurationSeconds { get; set; }
    public double? FullDurationSeconds { get; set; }
    public int Mp4Count { get; set; }
    public bool NeedsMerging { get; set; }
    public string? Warning { get; set; }
}
