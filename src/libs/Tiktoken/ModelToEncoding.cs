using Tiktoken.Encodings;

namespace Tiktoken;

/// <summary>
///
/// </summary>
public static class ModelToEncoding
{
    // Lazy singletons — each encoding is loaded only once, on first access.
    private static readonly Lazy<Encoding> Cl100K = new(static () => new Cl100KBase());
    private static readonly Lazy<Encoding> O200K = new(static () => new O200KBase());

    private static Dictionary<string, Lazy<Encoding>> Dictionary { get; } = new()
    {
        // o-series reasoning models
        { "o3", O200K },
        { "o1", O200K },

        // chat
        { "gpt-4o", O200K },
        { "gpt-4", Cl100K },
        { "gpt-3.5-turbo", Cl100K },
        { "gpt-35-turbo", Cl100K }, // Azure deployment name

        // embeddings
        { "text-embedding-ada-002", Cl100K },
        { "text-embedding-3-small", Cl100K },
        { "text-embedding-3-large", Cl100K },
    };

    /// <summary>
    /// Returns encoding by model name or null.
    /// Uses prefix matching (e.g., "gpt-4o-mini" matches "gpt-4o").
    /// </summary>
    /// <param name="modelName">gpt-4 gpt-3.5-turbo ...</param>
    /// <returns></returns>
    public static Encoding? TryFor(string modelName)
    {
        var lazy = Dictionary
            .FirstOrDefault(a => modelName.StartsWith(a.Key, StringComparison.Ordinal)).Value;

        return lazy?.Value;
    }

    /// <summary>
    /// Returns encoding by model name or throws exception.
    /// Uses prefix matching (e.g., "gpt-4o-mini" matches "gpt-4o").
    /// </summary>
    /// <param name="modelName">gpt-4 gpt-3.5-turbo ...</param>
    /// <exception cref="ArgumentException"></exception>
    /// <returns></returns>
    public static Encoding For(string modelName)
    {
        return TryFor(modelName) ??
               throw new ArgumentException($"Model name {modelName} is not supported.");
    }

    /// <summary>
    /// Returns encoding by encoding name (e.g., "cl100k_base", "o200k_base").
    /// </summary>
    /// <param name="encodingName">cl100k_base, o200k_base, p50k_base, p50k_edit, r50k_base</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static Encoding ForEncoding(string encodingName)
    {
        return TryForEncoding(encodingName) ??
               throw new ArgumentException($"Encoding name {encodingName} is not supported.");
    }

    /// <summary>
    /// Returns encoding by encoding name or null.
    /// </summary>
    /// <param name="encodingName">cl100k_base, o200k_base, p50k_base, p50k_edit, r50k_base</param>
    /// <returns></returns>
    public static Encoding? TryForEncoding(string encodingName)
    {
        return encodingName switch
        {
            "cl100k_base" => Cl100K.Value,
            "o200k_base" => O200K.Value,
            "p50k_base" => new P50KBase(),
            "p50k_edit" => new P50KEdit(),
            "r50k_base" => new R50KBase(),
            _ => null,
        };
    }
}
