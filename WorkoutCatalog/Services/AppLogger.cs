namespace WorkoutCatalog.Services;

public static class AppLogger
{
    private static readonly object Sync = new();

    private static readonly string PrimaryLogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "WorkoutCatalog",
        "logs",
        "app.log");

    private static readonly string FallbackLogPath = Path.Combine(
        Path.GetTempPath(),
        "WorkoutCatalog",
        "app.log");

    public static string LogPath => PrimaryLogPath;

    public static void LogInfo(string message)
    {
        WriteEntry("INFO", message, null);
    }

    public static void LogError(string context, Exception ex)
    {
        WriteEntry("ERROR", context, ex);
    }

    private static void WriteEntry(string level, string message, Exception? ex)
    {
        var body = ex == null ? message : $"{message}{Environment.NewLine}{ex}";
        var entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {level} {body}{Environment.NewLine}{Environment.NewLine}";

        if (TryAppend(PrimaryLogPath, entry))
            return;

        _ = TryAppend(FallbackLogPath, entry);
    }

    private static bool TryAppend(string path, string entry)
    {
        try
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            lock (Sync)
            {
                File.AppendAllText(path, entry);
            }

            return true;
        }
        catch
        {
            // Never throw from logger.
            return false;
        }
    }
}
