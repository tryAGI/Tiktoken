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
    /// </summary>
    /// <param name="modelName">gpt-4 gpt-3.5-turbo ...</param>
    /// <exception cref="ArgumentException"></exception>
    /// <returns></returns>
    public static Encoding? TryFor(string modelName)
    {
        var lazy = Dictionary
            .FirstOrDefault(a => modelName.StartsWith(a.Key, StringComparison.Ordinal)).Value;

        return lazy?.Value;
    }

    /// <summary>
    /// Returns encoding by model name or throws exception.
    /// </summary>
    /// <param name="modelName">gpt-4 gpt-3.5-turbo ...</param>
    /// <exception cref="ArgumentException"></exception>
    /// <returns></returns>
    public static Encoding For(string modelName)
    {
        return TryFor(modelName) ??
               throw new ArgumentException($"Model name {modelName} is not supported.");
    }
}
