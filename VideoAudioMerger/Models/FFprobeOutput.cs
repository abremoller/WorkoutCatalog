using System.Text.Json.Serialization;

namespace VideoAudioMerger.Models;

public class FFprobeOutput
{
    [JsonPropertyName("streams")]
    public List<FFprobeStream> Streams { get; set; } = new();
    
    [JsonPropertyName("format")]
    public FFprobeFormat? Format { get; set; }
}

public class FFprobeStream
{
    [JsonPropertyName("codec_type")]
    public string CodecType { get; set; } = string.Empty;
    
    [JsonPropertyName("width")]
    public int? Width { get; set; }
    
    [JsonPropertyName("height")]
    public int? Height { get; set; }
}

public class FFprobeFormat
{
    [JsonPropertyName("duration")]
    public string? Duration { get; set; }
    
    [JsonPropertyName("size")]
    public string? Size { get; set; }
}
