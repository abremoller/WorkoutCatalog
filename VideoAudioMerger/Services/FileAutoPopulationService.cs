using System.IO;
using VideoAudioMerger.Models;

namespace VideoAudioMerger.Services;

public class FileAutoPopulationService
{
    private readonly FFmpegService _ffmpegService;

    public FileAutoPopulationService(FFmpegService ffmpegService)
    {
        _ffmpegService = ffmpegService;
    }

    public async Task<AutoPopulatedFiles?> AutoPopulateFromFolder(string folderPath, IProgress<string>? progress = null)
    {
        if (!Directory.Exists(folderPath))
            return null;

        // Check if this is a complete project folder (contains both info.txt and info.png)
        var infoTxtPath = Directory.GetFiles(folderPath, "info.txt", SearchOption.TopDirectoryOnly).FirstOrDefault();
        var infoPngPath = Directory.GetFiles(folderPath, "info.png", SearchOption.TopDirectoryOnly).FirstOrDefault();

        if (!string.IsNullOrEmpty(infoTxtPath) && !string.IsNullOrEmpty(infoPngPath))
        {
            progress?.Report("Detected complete project folder with info files!");
            return await LoadCompleteProjectFolder(folderPath, infoTxtPath, infoPngPath, progress);
        }

        progress?.Report("Scanning folder for MP4 files...");

        // Get top 10 MP4 files sorted by creation date descending
        var mp4Files = Directory.GetFiles(folderPath, "*.mp4", SearchOption.TopDirectoryOnly)
            .Select(f => new FileInfo(f))
            .OrderByDescending(f => f.CreationTime)
            .Take(10)
            .Select(f => f.FullName)
            .ToList();

        if (mp4Files.Count < 2)
        {
            progress?.Report("Not enough MP4 files found in folder");
            return null;
        }

        progress?.Report($"Found {mp4Files.Count} MP4 files, probing...");

        // Probe all files
        var mediaFiles = new List<MediaFileInfo>();
        foreach (var file in mp4Files)
        {
            var mediaInfo = await _ffmpegService.ProbeFile(file);
            if (mediaInfo != null)
            {
                mediaFiles.Add(mediaInfo);
            }
        }

        // Separate video-only and audio-only files
        var videoFiles = mediaFiles.Where(f => f.IsVideoFile).OrderByDescending(f => f.FileSizeBytes).ToList();
        var audioFiles = mediaFiles.Where(f => f.IsAudioFile).OrderByDescending(f => f.FileSizeBytes).ToList();

        // Check if we have enough files
        if (videoFiles.Count < 2 || audioFiles.Count < 2)
        {
            progress?.Report($"Not enough separated files found (Videos: {videoFiles.Count}, Audio: {audioFiles.Count})");
            return null;
        }

        progress?.Report("Auto-population complete!");

        // Try to detect project name from files
        var projectName = DetectProjectName(mp4Files);

        return new AutoPopulatedFiles
        {
            FullVideoPath = videoFiles[0].FilePath,
            FullAudioPath = audioFiles[0].FilePath,
            PreviewVideoPath = videoFiles[1].FilePath,
            PreviewAudioPath = audioFiles[1].FilePath,
            ProjectName = projectName
        };
    }

    private async Task<AutoPopulatedFiles?> LoadCompleteProjectFolder(string folderPath, string infoTxtPath, string infoPngPath, IProgress<string>? progress)
    {
        progress?.Report("Loading all 4 files from project folder...");

        // Get all MP4 files in the folder
        var mp4Files = Directory.GetFiles(folderPath, "*.mp4", SearchOption.TopDirectoryOnly).ToList();

        if (mp4Files.Count < 4)
        {
            progress?.Report($"Expected 4 MP4 files but found only {mp4Files.Count}");
            return null;
        }

        // Probe all files to determine which is which
        var mediaFiles = new List<MediaFileInfo>();
        foreach (var file in mp4Files)
        {
            var mediaInfo = await _ffmpegService.ProbeFile(file);
            if (mediaInfo != null)
            {
                mediaFiles.Add(mediaInfo);
            }
        }

        // Separate video-only and audio-only files
        var videoFiles = mediaFiles.Where(f => f.IsVideoFile).OrderByDescending(f => f.FileSizeBytes).ToList();
        var audioFiles = mediaFiles.Where(f => f.IsAudioFile).OrderByDescending(f => f.FileSizeBytes).ToList();

        if (videoFiles.Count < 2 || audioFiles.Count < 2)
        {
            progress?.Report($"Expected 2 video and 2 audio files, found: Videos={videoFiles.Count}, Audio={audioFiles.Count}");
            return null;
        }

        // Load info.txt content
        string infoText = string.Empty;
        try
        {
            infoText = await File.ReadAllTextAsync(infoTxtPath);
        }
        catch (Exception ex)
        {
            progress?.Report($"Warning: Could not read info.txt - {ex.Message}");
        }

        // Detect project name from folder or files
        var folderName = Path.GetFileName(folderPath);
        var projectName = !string.IsNullOrWhiteSpace(folderName) ? folderName : DetectProjectName(mp4Files);

        progress?.Report("Complete project loaded successfully!");

        return new AutoPopulatedFiles
        {
            FullVideoPath = videoFiles[0].FilePath,
            FullAudioPath = audioFiles[0].FilePath,
            PreviewVideoPath = videoFiles[1].FilePath,
            PreviewAudioPath = audioFiles[1].FilePath,
            ProjectName = projectName,
            InfoText = infoText,
            InfoImagePath = infoPngPath
        };
    }

    private string DetectProjectName(List<string> filePaths)
    {
        // Look for files with numbers in brackets that don't contain "video" or "audio"
        foreach (var filePath in filePaths)
        {
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            var lowerFileName = fileName.ToLower();
            
            // Skip files with "video" or "audio" in the name
            if (lowerFileName.Contains("video") || lowerFileName.Contains("audio"))
                continue;
            
            // Check if it has a number in brackets
            var bracketMatch = System.Text.RegularExpressions.Regex.Match(fileName, @"\s*\(\d+\)");
            if (bracketMatch.Success)
            {
                // Remove the bracketed number and return the clean name
                return fileName.Substring(0, bracketMatch.Index).Trim();
            }
        }
        
        // Fallback: look for any file without "video" or "audio"
        foreach (var filePath in filePaths)
        {
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            var lowerFileName = fileName.ToLower();
            
            if (!lowerFileName.Contains("video") && !lowerFileName.Contains("audio"))
            {
                // Remove any bracketed numbers
                var bracketMatch = System.Text.RegularExpressions.Regex.Match(fileName, @"\s*\(\d+\)");
                if (bracketMatch.Success)
                {
                    return fileName.Substring(0, bracketMatch.Index).Trim();
                }
                return fileName.Trim();
            }
        }
        
        return string.Empty;
    }
}

public class AutoPopulatedFiles
{
    public string FullVideoPath { get; set; } = string.Empty;
    public string FullAudioPath { get; set; } = string.Empty;
    public string PreviewVideoPath { get; set; } = string.Empty;
    public string PreviewAudioPath { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public string InfoText { get; set; } = string.Empty;
    public string InfoImagePath { get; set; } = string.Empty;
}
