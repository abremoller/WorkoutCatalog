using Microsoft.EntityFrameworkCore;
using WorkoutCatalog.Data;
using WorkoutCatalog.Models;

namespace WorkoutCatalog.Services;

public class FolderScanService
{
    private readonly CatalogDbContext _db;
    private readonly FFmpegProbeService _probeService;
    private readonly ThumbnailService? _thumbnailService;

    private static readonly string[] ImageExtensions = [".png", ".jpg", ".jpeg", ".gif", ".bmp"];

    public FolderScanService(CatalogDbContext db, FFmpegProbeService probeService, ThumbnailService? thumbnailService = null)
    {
        _db = db;
        _probeService = probeService;
        _thumbnailService = thumbnailService;
    }

    public async Task<int> ScanFoldersAsync(string rootPath, IProgress<string>? progress = null, CancellationToken ct = default)
    {
        if (!Directory.Exists(rootPath))
            return 0;

        var subfolders = Directory.GetDirectories(rootPath);
        var count = 0;

        for (int i = 0; i < subfolders.Length; i++)
        {
            ct.ThrowIfCancellationRequested();

            var folder = subfolders[i];
            var folderName = Path.GetFileName(folder);
            progress?.Report($"Scanning ({i + 1}/{subfolders.Length}): {folderName}");

            var scanResult = await ScanSingleFolderAsync(folder, rootPath);
            if (scanResult == null) continue;

            await UpsertExerciseAsync(scanResult, rootPath);
            count++;
        }

        progress?.Report($"Scan complete. {count} exercises found.");
        return count;
    }

    public async Task<bool> RescanSingleFolderAsync(string rootPath, string folderPath)
    {
        if (!Directory.Exists(rootPath) || !Directory.Exists(folderPath))
            return false;

        var scanResult = await ScanSingleFolderAsync(folderPath, rootPath);
        if (scanResult == null)
            return false;

        await UpsertExerciseAsync(scanResult, rootPath);
        return true;
    }

    public async Task<FolderScanResult?> ScanSingleFolderAsync(string folderPath, string? rootPath = null)
    {
        if (!Directory.Exists(folderPath))
            return null;

        var result = new FolderScanResult
        {
            FolderName = Path.GetFileName(folderPath),
            FolderPath = folderPath
        };

        // Find description file (.txt)
        var txtFiles = Directory.GetFiles(folderPath, "*.txt");
        if (txtFiles.Length > 0)
        {
            result.DescriptionFileName = Path.GetFileName(txtFiles[0]);
            result.DescriptionText = await File.ReadAllTextAsync(txtFiles[0]);
        }

        // Find image file
        foreach (var ext in ImageExtensions)
        {
            var images = Directory.GetFiles(folderPath, $"*{ext}");
            if (images.Length > 0)
            {
                result.ImageFileName = Path.GetFileName(images[0]);
                break;
            }
        }

        // Find and classify MP4 files
        var mp4Files = Directory.GetFiles(folderPath, "*.mp4");
        result.Mp4Count = mp4Files.Length;

        if (mp4Files.Length == 0)
        {
            result.Warning = "No MP4 files found";
            return result;
        }

        if (mp4Files.Length == 4)
        {
            result.NeedsMerging = true;
            result.Warning = "Folder has 4 MP4 files - may need merging";
        }

        if (mp4Files.Length == 1)
        {
            var duration = await _probeService.GetDurationAsync(mp4Files[0]);
            result.FullVideoFileName = Path.GetFileName(mp4Files[0]);
            result.FullDurationSeconds = duration;
            result.PreviewVideoFileName = result.FullVideoFileName;
            result.PreviewDurationSeconds = duration;

            await GenerateThumbnailAsync(mp4Files[0], folderPath, result);
            return result;
        }

        if (mp4Files.Length == 2)
        {
            var durations = new (string path, double? duration)[2];
            durations[0] = (mp4Files[0], await _probeService.GetDurationAsync(mp4Files[0]));
            durations[1] = (mp4Files[1], await _probeService.GetDurationAsync(mp4Files[1]));

            var sorted = durations.OrderByDescending(d => d.duration ?? 0).ToArray();

            result.FullVideoFileName = Path.GetFileName(sorted[0].path);
            result.FullDurationSeconds = sorted[0].duration;
            result.PreviewVideoFileName = Path.GetFileName(sorted[1].path);
            result.PreviewDurationSeconds = sorted[1].duration;

            // Generate thumbnail from the full video (so a 30s capture is available)
            await GenerateThumbnailAsync(sorted[0].path, folderPath, result);
            return result;
        }

        // 3 or 5+ MP4s - unusual
        result.Warning = $"Unexpected number of MP4 files: {mp4Files.Length}";

        var allDurations = new List<(string path, double duration)>();
        foreach (var mp4 in mp4Files)
        {
            var dur = await _probeService.GetDurationAsync(mp4);
            if (dur.HasValue)
                allDurations.Add((mp4, dur.Value));
        }

        if (allDurations.Count >= 2)
        {
            var topTwo = allDurations.OrderByDescending(d => d.duration).Take(2).ToArray();
            result.FullVideoFileName = Path.GetFileName(topTwo[0].path);
            result.FullDurationSeconds = topTwo[0].duration;
            result.PreviewVideoFileName = Path.GetFileName(topTwo[1].path);
            result.PreviewDurationSeconds = topTwo[1].duration;

            // Generate thumbnail from the full video (so a 30s capture is available)
            await GenerateThumbnailAsync(topTwo[0].path, folderPath, result);
        }
        else if (allDurations.Count == 1)
        {
            result.FullVideoFileName = Path.GetFileName(allDurations[0].path);
            result.FullDurationSeconds = allDurations[0].duration;

            await GenerateThumbnailAsync(allDurations[0].path, folderPath, result);
        }

        return result;
    }

    private async Task GenerateThumbnailAsync(string videoPath, string folderPath, FolderScanResult result)
    {
        if (_thumbnailService == null) return;

        var thumbFile = await _thumbnailService.GenerateThumbnailAsync(videoPath, folderPath);
        if (thumbFile != null)
            result.ThumbnailFileName = thumbFile;
    }

    private async Task UpsertExerciseAsync(FolderScanResult scan, string rootPath)
    {
        var relativePath = Path.GetRelativePath(rootPath, scan.FolderPath);

        var existing = await _db.Exercises
            .FirstOrDefaultAsync(e => e.FolderRelativePath == relativePath);

        if (existing != null)
        {
            ApplyScan(existing, scan);
            existing.LastModified = DateTime.Now;
        }
        else
        {
            var exercise = new Exercise
            {
                FolderRelativePath = relativePath,
                DateAdded = DateTime.Now
            };
            ApplyScan(exercise, scan);
            _db.Exercises.Add(exercise);
        }

        await _db.SaveChangesAsync();
    }

    // Copies the scanned metadata onto an entity, used for both insert and update
    // so the field mapping lives in exactly one place.
    private static void ApplyScan(Exercise exercise, FolderScanResult scan)
    {
        exercise.Name = scan.FolderName;
        exercise.Description = scan.DescriptionText ?? string.Empty;
        exercise.HasDescription = scan.DescriptionText != null;
        exercise.HasImage = scan.ImageFileName != null;
        exercise.HasPreviewVideo = scan.PreviewVideoFileName != null;
        exercise.HasFullVideo = scan.FullVideoFileName != null;
        exercise.DescriptionFileName = scan.DescriptionFileName;
        exercise.ImageFileName = scan.ImageFileName;
        exercise.ThumbnailFileName = scan.ThumbnailFileName;
        exercise.PreviewVideoFileName = scan.PreviewVideoFileName;
        exercise.FullVideoFileName = scan.FullVideoFileName;
        exercise.PreviewDurationSeconds = scan.PreviewDurationSeconds;
        exercise.FullDurationSeconds = scan.FullDurationSeconds;
    }
}
