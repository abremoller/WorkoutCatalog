namespace WorkoutCatalog.Models;

public class Exercise
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FolderRelativePath { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public ExerciseType Type { get; set; } = ExerciseType.Unknown;
    public ExerciseLevel Level { get; set; } = ExerciseLevel.Unknown;

    public double? PreviewDurationSeconds { get; set; }
    public double? FullDurationSeconds { get; set; }

    public bool HasPreviewVideo { get; set; }
    public bool HasFullVideo { get; set; }
    public bool HasImage { get; set; }
    public bool HasDescription { get; set; }

    public string? PreviewVideoFileName { get; set; }
    public string? FullVideoFileName { get; set; }
    public string? ImageFileName { get; set; }
    public string? ThumbnailFileName { get; set; }
    public string? DescriptionFileName { get; set; }

    public int? Rating { get; set; }

    public DateTime DateAdded { get; set; }
    public DateTime? LastModified { get; set; }

    public ICollection<ExerciseComment> Comments { get; set; } = new List<ExerciseComment>();
    public ICollection<ViewHistory> ViewHistory { get; set; } = new List<ViewHistory>();
    public ICollection<PlaylistExercise> PlaylistExercises { get; set; } = new List<PlaylistExercise>();
}
