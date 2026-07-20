using System.IO;

namespace VideoAudioMerger.Services;

public class NameTrackingService
{
    private readonly string _trackingFilePath;
    private readonly HashSet<string> _processedNames;
    private readonly object _lock = new();

    public NameTrackingService(string trackingFilePath)
    {
        _trackingFilePath = trackingFilePath;
        _processedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        LoadFromFile();
    }

    private void LoadFromFile()
    {
        lock (_lock)
        {
            if (File.Exists(_trackingFilePath))
            {
                var lines = File.ReadAllLines(_trackingFilePath);
                foreach (var line in lines)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        _processedNames.Add(line.Trim());
                    }
                }
            }
        }
    }

    public bool IsNameProcessed(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        lock (_lock)
        {
            return _processedNames.Contains(name.Trim());
        }
    }

    public void AddName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return;

        lock (_lock)
        {
            var trimmedName = name.Trim();
            if (_processedNames.Add(trimmedName))
            {
                // Ensure directory exists
                var directory = Path.GetDirectoryName(_trackingFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Append to file
                File.AppendAllLines(_trackingFilePath, new[] { trimmedName });
            }
        }
    }

    public IReadOnlyCollection<string> GetAllProcessedNames()
    {
        lock (_lock)
        {
            return _processedNames.ToList().AsReadOnly();
        }
    }
}
