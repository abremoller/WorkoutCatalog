namespace WorkoutCatalog.Models;

public class PlaylistExercise
{
    public int Id { get; set; }
    public int PlaylistId { get; set; }
    public int ExerciseId { get; set; }
    public int SortOrder { get; set; }

    public Playlist Playlist { get; set; } = null!;
    public Exercise Exercise { get; set; } = null!;
}
