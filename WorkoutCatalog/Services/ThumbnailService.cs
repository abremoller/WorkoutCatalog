using System.Diagnostics;

namespace WorkoutCatalog.Services;

public class ThumbnailService
{
    private readonly string _ffmpegPath;
    private const string ThumbnailFileName = "thumb.jpg";
    private const int ThumbnailTimeSeconds = 30;

    public ThumbnailService(string ffmpegPath)
    {
        _ffmpegPath = ffmpegPath;
    }

    /// <summary>
    /// Captures a frame at the specified time and saves it as the thumbnail. Always overwrites.
    /// Returns the thumbnail file name if successful.
    /// </summary>
    public async Task<string?> CaptureFrameAtAsync(string videoFilePath, string outputFolder, int timeSeconds)
    {
        if (!File.Exists(videoFilePath) || string.IsNullOrEmpty(_ffmpegPath) || !File.Exists(_ffmpegPath))
            return null;

        var thumbnailPath = Path.Combine(outputFolder, ThumbnailFileName);

        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _ffmpegPath,
                    Arguments = $"-y -ss {timeSeconds} -i \"{videoFilePath}\" -vframes 1 -q:v 2 -vf scale=640:-1 \"{thumbnailPath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            return process.ExitCode == 0 && File.Exists(thumbnailPath) ? ThumbnailFileName : null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Generates a thumbnail from a video file. Returns the thumbnail file name if successful.
    /// </summary>
    public async Task<string?> GenerateThumbnailAsync(string videoFilePath, string outputFolder)
    {
        if (!File.Exists(videoFilePath) || string.IsNullOrEmpty(_ffmpegPath) || !File.Exists(_ffmpegPath))
            return null;

        var thumbnailPath = Path.Combine(outputFolder, ThumbnailFileName);

        // Skip if thumbnail already exists
        if (File.Exists(thumbnailPath))
            return ThumbnailFileName;

        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _ffmpegPath,
                    Arguments = $"-y -ss {ThumbnailTimeSeconds} -i \"{videoFilePath}\" -vframes 1 -q:v 2 -vf scale=640:-1 \"{thumbnailPath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode == 0 && File.Exists(thumbnailPath))
                return ThumbnailFileName;

            // If seeking to 30s failed (e.g., video shorter), try at 0s
            process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _ffmpegPath,
                    Arguments = $"-y -ss 0 -i \"{videoFilePath}\" -vframes 1 -q:v 2 -vf scale=640:-1 \"{thumbnailPath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            return process.ExitCode == 0 && File.Exists(thumbnailPath) ? ThumbnailFileName : null;
        }
        catch
        {
            return null;
        }
    }
}
