using System.Text.Json.Serialization;

namespace Tiktoken.Encodings;

/// <summary>
/// Represents a HuggingFace tokenizer.json file.
/// </summary>
public class TokenizerJson
{
    /// <summary>
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// </summary>
    [JsonPropertyName("added_tokens")]
    public IReadOnlyList<TokenizerAddedToken> AddedTokens { get; set; } = [];

    /// <summary>
    /// </summary>
    [JsonPropertyName("pre_tokenizer")]
    public TokenizerPreTokenizer? PreTokenizer { get; set; }

    /// <summary>
    /// </summary>
    [JsonPropertyName("model")]
    public TokenizerModel? Model { get; set; }
}

/// <summary>
/// A token added to the tokenizer vocabulary.
/// </summary>
public class TokenizerAddedToken
{
    /// <summary>
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// </summary>
    [JsonPropertyName("special")]
    public bool Special { get; set; }

    /// <summary>
    /// </summary>
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// The pre-tokenizer configuration. Supports ByteLevel, Split, Sequence, and other types.
/// </summary>
public class TokenizerPreTokenizer
{
    /// <summary>
    /// The pre-tokenizer type (ByteLevel, Split, Sequence, Metaspace, etc.).
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// For Split type: the pattern to split on.
    /// </summary>
    [JsonPropertyName("pattern")]
    public TokenizerSplitPattern? Pattern { get; set; }

    /// <summary>
    /// For Sequence type: the list of nested pre-tokenizers.
    /// </summary>
    [JsonPropertyName("pretokenizers")]
    public IReadOnlyList<TokenizerPreTokenizer>? PreTokenizers { get; set; }
}

/// <summary>
/// A split pattern — either a Regex or a String.
/// </summary>
public class TokenizerSplitPattern
{
    /// <summary>
    /// Regex pattern variant.
    /// </summary>
    [JsonPropertyName("Regex")]
    public string? Regex { get; set; }

    /// <summary>
    /// String pattern variant.
    /// </summary>
    [JsonPropertyName("String")]
    public string? String { get; set; }
}

/// <summary>
/// The BPE model definition.
/// </summary>
public class TokenizerModel
{
    /// <summary>
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// </summary>
    [JsonPropertyName("vocab")]
#pragma warning disable CA2227
    public Dictionary<string, int>? Vocab { get; set; }
#pragma warning restore CA2227

    /// <summary>
    /// </summary>
    [JsonPropertyName("merges")]
    public IReadOnlyList<string>? Merges { get; set; }
}
