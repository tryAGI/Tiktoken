using System.Text.Json.Serialization;

namespace Tiktoken.Encodings;

/// <summary>
/// 
/// </summary>
public class Model
{
    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("dropout")]
    public object? Dropout { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("unk_token")]
    public object? UnkToken { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("continuing_subword_prefix")]
    public string ContinuingSubwordPrefix { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("end_of_word_suffix")]
    public string EndOfWordSuffix { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("fuse_unk")]
    public bool? FuseUnk { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("vocab")]
    public IReadOnlyDictionary<string, int> Vocab { get; set; } = new Dictionary<string, int>();

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("merges")]
    public IReadOnlyList<string> Merges { get; } = [];
}