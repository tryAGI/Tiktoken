using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Tiktoken.Encodings;

/// <summary>
/// 
/// </summary>
public class Tokenizer
{
    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("truncation")]
    public object? Truncation { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("padding")]
    public object? Padding { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("added_tokens")]
    public IReadOnlyList<AddedToken> AddedTokens { get; } = [];

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("normalizer")]
    public object? Normalizer { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("pre_tokenizer")]
    public PreTokenizer? PreTokenizer { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("post_processor")]
    public PostProcessor? PostProcessor { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("decoder")]
    public Decoder? Decoder { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("model")]
    public Model? Model { get; set; }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public static Encoding GetEncodingFromFile(string path)
    {
        var json = File.ReadAllText(path);
        
        return GetEncodingFromJson(json);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="json"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    public static Encoding GetEncodingFromJson(string json, string name = "tokenizer")
    {
        json = json ?? throw new ArgumentNullException(nameof(json));
        
        var tokenizer =
            JsonSerializer.Deserialize(json, SourceGenerationContext.Default.Tokenizer) ??
            throw new InvalidOperationException("Json deserialization failed.");
        
        return ToEncoding(tokenizer, name);
    }

#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
    /// <summary>
    /// 
    /// </summary>
    /// <param name="path"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async Task <Encoding> GetEncodingFromFileAsync(string path, CancellationToken cancellationToken = default)
    {
        var json = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
        
        return GetEncodingFromJson(json);
    }
#endif
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="tokenizer"></param>
    /// <param name="encodingName"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    public static Encoding ToEncoding(Tokenizer tokenizer, string encodingName)
    {
        tokenizer = tokenizer ?? throw new ArgumentNullException(nameof(tokenizer));
        if (tokenizer.Model is null)
        {
            throw new InvalidOperationException("Model is not defined.");
        }

        return new Encoding(
            name: encodingName,
            // patterns: [
            //     @"[^\r\n\p{L}\p{N}]?[\p{Lu}\p{Lt}\p{Lm}\p{Lo}\p{M}]*[\p{Ll}\p{Lm}\p{Lo}\p{M}]+(?i:'s|'t|'re|'ve|'m|'ll|'d)?",
            //     @"[^\r\n\p{L}\p{N}]?[\p{Lu}\p{Lt}\p{Lm}\p{Lo}\p{M}]+[\p{Ll}\p{Lm}\p{Lo}\p{M}]*(?i:'s|'t|'re|'ve|'m|'ll|'d)?",
            //     @"\p{N}{1,3}",
            //     @" ?[^\s\p{L}\p{N}]+[\r\n/]*",
            //     @"\s*[\r\n]+",
            //     @"\s+(?!\S)",
            //     @"\s+",
            // ],
            patterns: tokenizer.Model.Vocab.Select(pair => Regex.Escape(pair.Key)).ToArray(),
            mergeableRanks: tokenizer.Model.Vocab
                .ToDictionary(
                    x => System.Text.Encoding.UTF8.GetBytes(x.Key),
                    x => x.Value),
            specialTokens: new Dictionary<string, int> //tokenizer.AddedTokens.ToDictionary(token => token.Content, token => token.Id))
            {
                ["<|endoftext|>"] = 50256,
            }) 
        {
            CompiledRegex = false,
        };
    }
}