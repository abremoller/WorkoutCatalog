using Microsoft.EntityFrameworkCore;
using WorkoutCatalog.Data;
using WorkoutCatalog.Models;

namespace WorkoutCatalog.Services;

public class PlaylistService
{
    private readonly CatalogDbContext _db;

    public PlaylistService(CatalogDbContext db)
    {
        _db = db;
    }

    public async Task<List<Playlist>> GetAllAsync()
    {
        return await _db.Playlists
            .Include(p => p.PlaylistExercises)
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<Playlist?> GetByIdAsync(int id)
    {
        return await _db.Playlists
            .Include(p => p.PlaylistExercises)
                .ThenInclude(pe => pe.Exercise)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Playlist> CreateAsync(string name, string description = "")
    {
        var playlist = new Playlist
        {
            Name = name,
            Description = description,
            CreatedAt = DateTime.Now
        };
        _db.Playlists.Add(playlist);
        await _db.SaveChangesAsync();
        return playlist;
    }

    public async Task UpdateAsync(int playlistId, string name, string description)
    {
        var playlist = await _db.Playlists.FindAsync(playlistId);
        if (playlist == null) return;

        playlist.Name = name;
        playlist.Description = description;
        playlist.LastModified = DateTime.Now;
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int playlistId)
    {
        var playlist = await _db.Playlists.FindAsync(playlistId);
        if (playlist == null) return;

        _db.Playlists.Remove(playlist);
        await _db.SaveChangesAsync();
    }

    public async Task AddExerciseAsync(int playlistId, int exerciseId)
    {
        var exists = await _db.PlaylistExercises
            .AnyAsync(pe => pe.PlaylistId == playlistId && pe.ExerciseId == exerciseId);
        if (exists) return;

        var maxOrder = await _db.PlaylistExercises
            .Where(pe => pe.PlaylistId == playlistId)
            .MaxAsync(pe => (int?)pe.SortOrder) ?? 0;

        _db.PlaylistExercises.Add(new PlaylistExercise
        {
            PlaylistId = playlistId,
            ExerciseId = exerciseId,
            SortOrder = maxOrder + 1
        });
        await _db.SaveChangesAsync();
    }

    public async Task RemoveExerciseAsync(int playlistId, int exerciseId)
    {
        var entry = await _db.PlaylistExercises
            .FirstOrDefaultAsync(pe => pe.PlaylistId == playlistId && pe.ExerciseId == exerciseId);
        if (entry == null) return;

        _db.PlaylistExercises.Remove(entry);
        await _db.SaveChangesAsync();
    }

    public async Task ReorderExerciseAsync(int playlistId, int exerciseId, int newSortOrder)
    {
        var entry = await _db.PlaylistExercises
            .FirstOrDefaultAsync(pe => pe.PlaylistId == playlistId && pe.ExerciseId == exerciseId);
        if (entry == null) return;

        entry.SortOrder = newSortOrder;
        await _db.SaveChangesAsync();
    }

    public async Task<List<Playlist>> GetPlaylistsForExerciseAsync(int exerciseId)
    {
        return await _db.PlaylistExercises
            .Where(pe => pe.ExerciseId == exerciseId)
            .Select(pe => pe.Playlist)
            .OrderBy(p => p.Name)
            .ToListAsync();
    }
}
