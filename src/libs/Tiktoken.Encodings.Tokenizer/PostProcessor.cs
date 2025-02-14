using System.Text.Json.Serialization;

namespace Tiktoken.Encodings;

/// <summary>
/// 
/// </summary>
public class PostProcessor
{
    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("add_prefix_space")]
    public bool? AddPrefixSpace { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("trim_offsets")]
    public bool? TrimOffsets { get; set; }
}