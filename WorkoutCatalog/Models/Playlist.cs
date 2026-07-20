namespace WorkoutCatalog.Models;

public class Playlist
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastModified { get; set; }

    public ICollection<PlaylistExercise> PlaylistExercises { get; set; } = new List<PlaylistExercise>();
}
