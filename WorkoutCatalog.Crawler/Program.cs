using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic.FileIO;
using WorkoutCatalog.Data;
using WorkoutCatalog.Models;
using WorkoutCatalog.Services;

namespace WorkoutCatalog.Crawler;

class Program
{
    static async Task<int> Main(string[] args)
    {
        if (args.Length == 0)
        {
            PrintUsage();
            return 1;
        }

        var command = args[0].ToLower();
        var options = ParseOptions(args[1..]);

        return command switch
        {
            "scan" => await RunScan(options),
            "merge" => await RunMerge(options),
            "cleanup" => await RunCleanup(options),
            _ => PrintUsage()
        };
    }

    static int PrintUsage()
    {
        Console.WriteLine("WorkoutCatalog.Crawler - Batch video processing tool");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  crawler scan    --root <path> --ffmpeg <path>   Scan folders and populate database");
        Console.WriteLine("  crawler merge   --root <path> --ffmpeg <path>   Merge 4-MP4 folders into Full.mp4 + Preview.mp4");
        Console.WriteLine("  crawler cleanup --root <path>                   Move source files to Recycle Bin after merge");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --root <path>     Root folder containing exercise subfolders");
        Console.WriteLine("  --ffmpeg <path>   Path to ffmpeg.exe");
        Console.WriteLine("  --dry-run         Show what would be done without making changes");
        return 1;
    }

    static Dictionary<string, string> ParseOptions(string[] args)
    {
        var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].StartsWith("--"))
            {
                var key = args[i][2..].ToLower();
                if (key == "dry-run")
                {
                    options[key] = "true";
                }
                else if (i + 1 < args.Length && !args[i + 1].StartsWith("--"))
                {
                    options[key] = args[++i];
                }
            }
        }
        return options;
    }

    static async Task<int> RunScan(Dictionary<string, string> options)
    {
        if (!options.TryGetValue("root", out var root) || !options.TryGetValue("ffmpeg", out var ffmpeg))
        {
            Console.Error.WriteLine("Error: --root and --ffmpeg are required for scan.");
            return 1;
        }

        if (!Directory.Exists(root))
        {
            Console.Error.WriteLine($"Error: Root folder does not exist: {root}");
            return 1;
        }

        Console.WriteLine($"Scanning: {root}");

        using var db = new CatalogDbContext();
        await db.Database.EnsureCreatedAsync();

        var probeService = new FFmpegProbeService(ffmpeg);
        var thumbnailService = new ThumbnailService(ffmpeg);
        var scanService = new FolderScanService(db, probeService, thumbnailService);

        var progress = new Progress<string>(msg => Console.WriteLine($"  {msg}"));
        var count = await scanService.ScanFoldersAsync(root, progress);

        Console.WriteLine($"Done. {count} exercises found.");
        return 0;
    }

    static async Task<int> RunMerge(Dictionary<string, string> options)
    {
        if (!options.TryGetValue("root", out var root) || !options.TryGetValue("ffmpeg", out var ffmpeg))
        {
            Console.Error.WriteLine("Error: --root and --ffmpeg are required for merge.");
            return 1;
        }

        if (!Directory.Exists(root))
        {
            Console.Error.WriteLine($"Error: Root folder does not exist: {root}");
            return 1;
        }

        var dryRun = options.ContainsKey("dry-run");
        var ffprobePath = Path.Combine(Path.GetDirectoryName(ffmpeg) ?? "", "ffprobe.exe");

        Console.WriteLine($"Scanning for 4-MP4 folders in: {root}");
        if (dryRun) Console.WriteLine("(DRY RUN - no files will be created)");

        var subfolders = Directory.GetDirectories(root);
        var mergedCount = 0;
        var skippedCount = 0;
        var errorCount = 0;

        foreach (var folder in subfolders)
        {
            var folderName = Path.GetFileName(folder);
            var mp4Files = Directory.GetFiles(folder, "*.mp4");

            if (mp4Files.Length != 4)
                continue;

            // Check if already merged (Full.mp4 and Preview.mp4 exist)
            var fullPath = Path.Combine(folder, "Full.mp4");
            var previewPath = Path.Combine(folder, "Preview.mp4");
            if (File.Exists(fullPath) && File.Exists(previewPath))
            {
                Console.WriteLine($"  SKIP (already merged): {folderName}");
                skippedCount++;
                continue;
            }

            Console.WriteLine($"  Probing: {folderName}");

            // Probe all 4 files
            var probeResults = new List<(string path, bool hasVideo, bool hasAudio, long size)>();
            foreach (var mp4 in mp4Files)
            {
                var probe = await ProbeFileAsync(ffprobePath, mp4);
                if (probe != null)
                {
                    probeResults.Add((mp4, probe.Value.hasVideo, probe.Value.hasAudio, new FileInfo(mp4).Length));
                }
            }

            // Separate video-only and audio-only
            var videoFiles = probeResults.Where(f => f.hasVideo && !f.hasAudio).OrderByDescending(f => f.size).ToList();
            var audioFiles = probeResults.Where(f => f.hasAudio && !f.hasVideo).OrderByDescending(f => f.size).ToList();

            if (videoFiles.Count < 2 || audioFiles.Count < 2)
            {
                Console.WriteLine($"  ERROR: Could not classify files (Videos: {videoFiles.Count}, Audio: {audioFiles.Count}): {folderName}");
                errorCount++;
                continue;
            }

            // Largest = Full, second = Preview
            var fullVideo = videoFiles[0].path;
            var fullAudio = audioFiles[0].path;
            var previewVideo = videoFiles[1].path;
            var previewAudio = audioFiles[1].path;

            Console.WriteLine($"    Full:    {Path.GetFileName(fullVideo)} + {Path.GetFileName(fullAudio)}");
            Console.WriteLine($"    Preview: {Path.GetFileName(previewVideo)} + {Path.GetFileName(previewAudio)}");

            if (dryRun)
            {
                Console.WriteLine($"    Would create: Full.mp4, Preview.mp4");
                mergedCount++;
                continue;
            }

            // Merge Full
            var fullResult = await MergeAsync(ffmpeg, fullVideo, fullAudio, fullPath);
            if (!fullResult)
            {
                Console.WriteLine($"    FAILED: Full merge failed for {folderName}");
                errorCount++;
                continue;
            }
            Console.WriteLine($"    Created: Full.mp4");

            // Merge Preview
            var previewResult = await MergeAsync(ffmpeg, previewVideo, previewAudio, previewPath);
            if (!previewResult)
            {
                Console.WriteLine($"    FAILED: Preview merge failed for {folderName}");
                errorCount++;
                continue;
            }
            Console.WriteLine($"    Created: Preview.mp4");

            mergedCount++;
        }

        Console.WriteLine();
        Console.WriteLine($"Done. Merged: {mergedCount}, Skipped: {skippedCount}, Errors: {errorCount}");
        return errorCount > 0 ? 2 : 0;
    }

    static Task<int> RunCleanup(Dictionary<string, string> options)
    {
        if (!options.TryGetValue("root", out var root))
        {
            Console.Error.WriteLine("Error: --root is required for cleanup.");
            return Task.FromResult(1);
        }

        if (!Directory.Exists(root))
        {
            Console.Error.WriteLine($"Error: Root folder does not exist: {root}");
            return Task.FromResult(1);
        }

        var dryRun = options.ContainsKey("dry-run");

        Console.WriteLine($"Cleaning up merged folders in: {root}");
        if (dryRun) Console.WriteLine("(DRY RUN - no files will be deleted)");

        var subfolders = Directory.GetDirectories(root);
        var cleanedCount = 0;

        foreach (var folder in subfolders)
        {
            var folderName = Path.GetFileName(folder);
            var fullPath = Path.Combine(folder, "Full.mp4");
            var previewPath = Path.Combine(folder, "Preview.mp4");

            // Only cleanup if merged outputs exist
            if (!File.Exists(fullPath) || !File.Exists(previewPath))
                continue;

            // Find source files (all MP4s that aren't Full.mp4 or Preview.mp4)
            var sourceFiles = Directory.GetFiles(folder, "*.mp4")
                .Where(f =>
                {
                    var name = Path.GetFileName(f);
                    return !name.Equals("Full.mp4", StringComparison.OrdinalIgnoreCase) &&
                           !name.Equals("Preview.mp4", StringComparison.OrdinalIgnoreCase);
                })
                .ToList();

            if (sourceFiles.Count == 0)
                continue;

            Console.WriteLine($"  {folderName}: {sourceFiles.Count} source files to recycle");

            if (dryRun)
            {
                foreach (var f in sourceFiles)
                    Console.WriteLine($"    Would recycle: {Path.GetFileName(f)}");
                cleanedCount++;
                continue;
            }

            foreach (var sourceFile in sourceFiles)
            {
                try
                {
                    FileSystem.DeleteFile(sourceFile, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                    Console.WriteLine($"    Recycled: {Path.GetFileName(sourceFile)}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"    ERROR recycling {Path.GetFileName(sourceFile)}: {ex.Message}");
                }
            }

            cleanedCount++;
        }

        Console.WriteLine($"Done. Cleaned {cleanedCount} folders.");
        return Task.FromResult(0);
    }

    // --- FFmpeg/FFprobe helpers ---

    static async Task<(bool hasVideo, bool hasAudio)?> ProbeFileAsync(string ffprobePath, string filePath)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ffprobePath,
                    Arguments = $"-v quiet -print_format json -show_streams \"{filePath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0) return null;

            var data = JsonSerializer.Deserialize<ProbeOutput>(output);
            if (data == null) return null;

            var hasVideo = data.Streams.Any(s => s.CodecType == "video");
            var hasAudio = data.Streams.Any(s => s.CodecType == "audio");
            return (hasVideo, hasAudio);
        }
        catch
        {
            return null;
        }
    }

    static async Task<bool> MergeAsync(string ffmpegPath, string videoPath, string audioPath, string outputPath)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = $"-y -i \"{videoPath}\" -i \"{audioPath}\" -shortest -c:v copy -c:a copy \"{outputPath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            return process.ExitCode == 0 && File.Exists(outputPath);
        }
        catch
        {
            return false;
        }
    }

    // Internal DTO for probe JSON
    private class ProbeOutput
    {
        [JsonPropertyName("streams")]
        public List<ProbeStream> Streams { get; set; } = [];
    }

    private class ProbeStream
    {
        [JsonPropertyName("codec_type")]
        public string CodecType { get; set; } = string.Empty;
    }
}
