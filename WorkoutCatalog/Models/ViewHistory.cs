namespace WorkoutCatalog.Models;

public class ViewHistory
{
    public int Id { get; set; }
    public int ExerciseId { get; set; }
    public DateTime ViewedAt { get; set; }
    public string VideoType { get; set; } = string.Empty;

    public Exercise Exercise { get; set; } = null!;
}
