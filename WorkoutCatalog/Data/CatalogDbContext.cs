using Microsoft.EntityFrameworkCore;
using WorkoutCatalog.Models;

namespace WorkoutCatalog.Data;

public class CatalogDbContext : DbContext
{
    public DbSet<Exercise> Exercises => Set<Exercise>();
    public DbSet<ExerciseComment> Comments => Set<ExerciseComment>();
    public DbSet<ViewHistory> ViewHistory => Set<ViewHistory>();
    public DbSet<Playlist> Playlists => Set<Playlist>();
    public DbSet<PlaylistExercise> PlaylistExercises => Set<PlaylistExercise>();

    private readonly string _dbPath;

    public CatalogDbContext(string? dbPath = null)
    {
        _dbPath = dbPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "WorkoutCatalog",
            "catalog.db");
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        var dir = Path.GetDirectoryName(_dbPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        options.UseSqlite($"Data Source={_dbPath}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Exercise>(e =>
        {
            e.HasIndex(x => x.Name);
            e.HasIndex(x => x.Type);
            e.HasIndex(x => x.Level);
            e.HasIndex(x => x.Rating);
            e.HasIndex(x => x.FolderRelativePath).IsUnique();
        });

        modelBuilder.Entity<ExerciseComment>(e =>
        {
            e.HasOne(c => c.Exercise)
             .WithMany(ex => ex.Comments)
             .HasForeignKey(c => c.ExerciseId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ViewHistory>(e =>
        {
            e.HasOne(h => h.Exercise)
             .WithMany(ex => ex.ViewHistory)
             .HasForeignKey(h => h.ExerciseId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(h => h.ViewedAt);
        });

        modelBuilder.Entity<Playlist>(e =>
        {
            e.HasIndex(x => x.Name);
        });

        modelBuilder.Entity<PlaylistExercise>(e =>
        {
            e.HasOne(pe => pe.Playlist)
             .WithMany(p => p.PlaylistExercises)
             .HasForeignKey(pe => pe.PlaylistId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(pe => pe.Exercise)
             .WithMany(ex => ex.PlaylistExercises)
             .HasForeignKey(pe => pe.ExerciseId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(pe => new { pe.PlaylistId, pe.ExerciseId }).IsUnique();
        });
    }

    public async Task EnsurePlaylistSchemaAsync()
    {
        await Database.ExecuteSqlRawAsync(@"
CREATE TABLE IF NOT EXISTS Playlists (
    Id INTEGER NOT NULL CONSTRAINT PK_Playlists PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    Description TEXT NOT NULL,
    CreatedAt TEXT NOT NULL,
    LastModified TEXT NULL
);");

        await Database.ExecuteSqlRawAsync(@"
CREATE TABLE IF NOT EXISTS PlaylistExercises (
    Id INTEGER NOT NULL CONSTRAINT PK_PlaylistExercises PRIMARY KEY AUTOINCREMENT,
    PlaylistId INTEGER NOT NULL,
    ExerciseId INTEGER NOT NULL,
    SortOrder INTEGER NOT NULL,
    CONSTRAINT FK_PlaylistExercises_Playlists_PlaylistId FOREIGN KEY (PlaylistId) REFERENCES Playlists (Id) ON DELETE CASCADE,
    CONSTRAINT FK_PlaylistExercises_Exercises_ExerciseId FOREIGN KEY (ExerciseId) REFERENCES Exercises (Id) ON DELETE CASCADE
);");

        await Database.ExecuteSqlRawAsync("CREATE INDEX IF NOT EXISTS IX_Playlists_Name ON Playlists (Name);");
        await Database.ExecuteSqlRawAsync("CREATE UNIQUE INDEX IF NOT EXISTS IX_PlaylistExercises_PlaylistId_ExerciseId ON PlaylistExercises (PlaylistId, ExerciseId);");
    }
}
