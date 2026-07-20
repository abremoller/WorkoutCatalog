namespace VideoAudioMerger.Models;

public class ProcessingResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ErrorDetails { get; set; }
    public string? OutputPath { get; set; }
}
