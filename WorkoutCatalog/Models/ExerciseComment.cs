namespace WorkoutCatalog.Models;

public class ExerciseComment
{
    public int Id { get; set; }
    public int ExerciseId { get; set; }
    public string Text { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public Exercise Exercise { get; set; } = null!;
}
