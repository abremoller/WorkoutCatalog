using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WorkoutCatalog.Services;

public class FFmpegProbeService
{
    private readonly string _ffprobePath;

    public FFmpegProbeService(string ffmpegPath)
    {
        _ffprobePath = Path.Combine(
            Path.GetDirectoryName(ffmpegPath) ?? string.Empty, "ffprobe.exe");
    }

    public async Task<double?> GetDurationAsync(string filePath)
    {
        if (!File.Exists(filePath))
            return null;

        try
        {
            var output = await RunProbeAsync(filePath);
            if (output == null) return null;

            var probeData = JsonSerializer.Deserialize<ProbeOutput>(output);
            if (probeData?.Format?.Duration != null &&
                double.TryParse(probeData.Format.Duration, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var duration))
            {
                return duration;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<ProbeResult?> ProbeFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
            return null;

        try
        {
            var output = await RunProbeAsync(filePath);
            if (output == null) return null;

            var probeData = JsonSerializer.Deserialize<ProbeOutput>(output);
            if (probeData == null) return null;

            var result = new ProbeResult();

            foreach (var stream in probeData.Streams)
            {
                if (stream.CodecType == "video")
                    result.HasVideo = true;
                else if (stream.CodecType == "audio")
                    result.HasAudio = true;
            }

            if (probeData.Format?.Duration != null &&
                double.TryParse(probeData.Format.Duration, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var duration))
            {
                result.DurationSeconds = duration;
            }

            return result;
        }
        catch
        {
            return null;
        }
    }

    private async Task<string?> RunProbeAsync(string filePath)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = _ffprobePath,
                Arguments = $"-v quiet -print_format json -show_format -show_streams \"{filePath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        var output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();

        return process.ExitCode == 0 ? output : null;
    }

    // Internal DTOs for FFprobe JSON output
    private class ProbeOutput
    {
        [JsonPropertyName("streams")]
        public List<ProbeStream> Streams { get; set; } = [];

        [JsonPropertyName("format")]
        public ProbeFormat? Format { get; set; }
    }

    private class ProbeStream
    {
        [JsonPropertyName("codec_type")]
        public string CodecType { get; set; } = string.Empty;
    }

    private class ProbeFormat
    {
        [JsonPropertyName("duration")]
        public string? Duration { get; set; }
    }
}

public class ProbeResult
{
    public bool HasVideo { get; set; }
    public bool HasAudio { get; set; }
    public double? DurationSeconds { get; set; }
}
