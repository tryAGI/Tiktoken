using System.Text.Json.Serialization;

namespace Tiktoken.Encodings;

/// <summary>
/// 
/// </summary>
public class AddedToken
{
    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("special")]
    public bool Special { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("single_word")]
    public bool SingleWord { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("lstrip")]
    public bool Lstrip { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("rstrip")]
    public bool Rstrip { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("normalized")]
    public bool Normalized { get; set; }
}