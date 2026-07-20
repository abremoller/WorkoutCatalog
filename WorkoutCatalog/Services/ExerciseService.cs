using Microsoft.EntityFrameworkCore;
using WorkoutCatalog.Data;
using WorkoutCatalog.Models;

namespace WorkoutCatalog.Services;

public class ExerciseService
{
    private readonly CatalogDbContext _db;

    public ExerciseService(CatalogDbContext db)
    {
        _db = db;
    }

    public async Task<List<Exercise>> GetAllAsync()
    {
        return await _db.Exercises
            .OrderBy(e => e.Name)
            .ToListAsync();
    }

    public async Task<List<Exercise>> SearchAsync(
        string? keyword = null,
        ExerciseType? type = null,
        ExerciseLevel? level = null,
        double? minDurationSeconds = null,
        double? maxDurationSeconds = null,
        int? minRating = null)
    {
        var query = _db.Exercises.AsQueryable();

        if (type.HasValue && type.Value != ExerciseType.Unknown)
            query = query.Where(e => e.Type == type.Value);

        if (level.HasValue && level.Value != ExerciseLevel.Unknown)
            query = query.Where(e => e.Level == level.Value);

        if (minDurationSeconds.HasValue)
            query = query.Where(e => e.FullDurationSeconds >= minDurationSeconds.Value);

        if (maxDurationSeconds.HasValue)
            query = query.Where(e => e.FullDurationSeconds <= maxDurationSeconds.Value);

        if (minRating.HasValue)
            query = query.Where(e => e.Rating >= minRating.Value);

        // Always include comments to enable keyword search across all text fields
        var all = await query.Include(e => e.Comments).OrderBy(e => e.Name).ToListAsync();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var lower = keyword.ToLower();
            // Search name, description, and all comment text; order by relevance
            return all.Where(e =>
                e.Name.ToLower().Contains(lower) ||
                (e.Description?.ToLower().Contains(lower) ?? false) ||
                e.Comments.Any(c => c.Text.ToLower().Contains(lower))
            )
            .OrderByDescending(e => e.Name.ToLower().Contains(lower))  // Name match first
            .ThenByDescending(e => e.Comments.Any(c => c.Text.ToLower().Contains(lower)))  // Comments second
            .ThenByDescending(e => (e.Description?.ToLower().Contains(lower) ?? false))  // Description third
            .ThenBy(e => e.Name)  // Then alphabetical
            .ToList();
        }

        return all;
    }

    public async Task<Exercise?> GetByIdAsync(int id)
    {
        return await _db.Exercises
            .Include(e => e.Comments.OrderByDescending(c => c.CreatedAt))
            .Include(e => e.ViewHistory.OrderByDescending(h => h.ViewedAt))
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<List<Exercise>> GetTopRatedAsync(int count = 10)
    {
        return await _db.Exercises
            .Where(e => e.Rating.HasValue)
            .OrderByDescending(e => e.Rating)
            .ThenBy(e => e.Name)
            .Take(count)
            .ToListAsync();
    }

    public async Task<List<ViewHistory>> GetRecentHistoryAsync(int count = 50)
    {
        return await _db.ViewHistory
            .Include(h => h.Exercise)
            .OrderByDescending(h => h.ViewedAt)
            .Take(count)
            .ToListAsync();
    }

    public async Task UpdateRatingAsync(int exerciseId, int? rating)
    {
        var exercise = await _db.Exercises.FindAsync(exerciseId);
        if (exercise == null) return;

        exercise.Rating = rating;
        exercise.LastModified = DateTime.Now;
        await _db.SaveChangesAsync();
    }

    public async Task UpdateMetadataAsync(int exerciseId, ExerciseType type, ExerciseLevel level)
    {
        var exercise = await _db.Exercises.FindAsync(exerciseId);
        if (exercise == null) return;

        exercise.Type = type;
        exercise.Level = level;
        exercise.LastModified = DateTime.Now;
        await _db.SaveChangesAsync();
    }

    public async Task AddCommentAsync(int exerciseId, string text)
    {
        _db.Comments.Add(new ExerciseComment
        {
            ExerciseId = exerciseId,
            Text = text,
            CreatedAt = DateTime.Now
        });
        await _db.SaveChangesAsync();
    }

    public async Task DeleteCommentAsync(int commentId)
    {
        var comment = await _db.Comments.FindAsync(commentId);
        if (comment == null) return;

        _db.Comments.Remove(comment);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateThumbnailAsync(int exerciseId, string? thumbnailFileName)
    {
        var exercise = await _db.Exercises.FindAsync(exerciseId);
        if (exercise == null) return;

        exercise.ThumbnailFileName = thumbnailFileName;
        exercise.LastModified = DateTime.Now;
        await _db.SaveChangesAsync();
    }

    public async Task RecordViewAsync(int exerciseId, VideoKind videoType)
    {
        _db.ViewHistory.Add(new ViewHistory
        {
            ExerciseId = exerciseId,
            ViewedAt = DateTime.Now,
            VideoType = videoType.ToString()
        });
        await _db.SaveChangesAsync();
    }
}
