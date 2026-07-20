using System.Diagnostics;
using System.IO;
using System.Text.Json;
using VideoAudioMerger.Models;

namespace VideoAudioMerger.Services;

public class FFmpegService
{
    private readonly string _ffmpegPath;
    private readonly string _ffprobePath;

    public FFmpegService(string ffmpegPath)
    {
        _ffmpegPath = ffmpegPath;
        _ffprobePath = Path.Combine(Path.GetDirectoryName(ffmpegPath) ?? string.Empty, "ffprobe.exe");
    }

    public async Task<bool> ValidateFFmpegPath()
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _ffmpegPath,
                    Arguments = "-version",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            
            process.Start();
            await process.WaitForExitAsync();
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    public async Task<MediaFileInfo?> ProbeFile(string filePath)
    {
        if (!File.Exists(filePath))
            return null;

        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _ffprobePath,
                    Arguments = $"-v quiet -print_format json -show_format -show_streams \"{filePath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
                return null;

            var probeData = JsonSerializer.Deserialize<FFprobeOutput>(output);
            if (probeData == null)
                return null;

            var fileInfo = new FileInfo(filePath);
            var mediaInfo = new MediaFileInfo
            {
                FilePath = filePath,
                FileSizeBytes = fileInfo.Length,
                CreatedDate = fileInfo.CreationTime
            };

            foreach (var stream in probeData.Streams)
            {
                if (stream.CodecType == "video")
                {
                    mediaInfo.HasVideo = true;
                    mediaInfo.Width = stream.Width;
                    mediaInfo.Height = stream.Height;
                }
                else if (stream.CodecType == "audio")
                {
                    mediaInfo.HasAudio = true;
                }
            }

            if (probeData.Format?.Duration != null && double.TryParse(probeData.Format.Duration, out var duration))
            {
                mediaInfo.DurationSeconds = duration;
            }

            return mediaInfo;
        }
        catch
        {
            return null;
        }
    }

    public async Task<ProcessingResult> MergeVideoAudio(string videoPath, string audioPath, string outputPath, IProgress<string>? progress = null)
    {
        try
        {
            // Ensure output directory exists
            var outputDir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            progress?.Report($"Starting merge of {Path.GetFileName(outputPath)}...");

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _ffmpegPath,
                    Arguments = $"-y -i \"{videoPath}\" -i \"{audioPath}\" -shortest -c:v copy -c:a copy \"{outputPath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            var errorOutput = new System.Text.StringBuilder();

            process.Start();
            
            // Read stderr line by line to show progress (FFmpeg outputs to stderr)
            var errorTask = Task.Run(async () =>
            {
                while (!process.StandardError.EndOfStream)
                {
                    var line = await process.StandardError.ReadLineAsync();
                    if (!string.IsNullOrEmpty(line))
                    {
                        errorOutput.AppendLine(line);
                        // Report key progress lines
                        if (line.Contains("time=") || line.Contains("size=") || line.Contains("bitrate="))
                        {
                            progress?.Report(line.Trim());
                        }
                    }
                }
            });
            
            // Also read stdout to prevent deadlock
            var outputTask = process.StandardOutput.ReadToEndAsync();
            
            await process.WaitForExitAsync();
            await errorTask;
            var error = errorOutput.ToString();

            if (process.ExitCode != 0)
            {
                return new ProcessingResult
                {
                    Success = false,
                    Message = "FFmpeg processing failed",
                    ErrorDetails = error
                };
            }

            progress?.Report($"✓ Completed {Path.GetFileName(outputPath)}");

            return new ProcessingResult
            {
                Success = true,
                Message = "Merge completed successfully",
                OutputPath = outputPath
            };
        }
        catch (Exception ex)
        {
            return new ProcessingResult
            {
                Success = false,
                Message = "Error during merge",
                ErrorDetails = ex.Message
            };
        }
    }
}
