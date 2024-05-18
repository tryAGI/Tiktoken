using Tiktoken.Encodings;

namespace Tiktoken;

/// <summary>
/// 
/// </summary>
public static class Encoders
{
    /// <summary>
    /// Returns encoder by model name.
    /// </summary>
    /// <param name="modelName">gpt-3.5-turbo</param>
    /// <returns></returns>
    public static Encoder ForModel(string modelName)
    {
        return new Encoder(GetEncodingByModel(modelName));
    }
    
    /// <summary>
    /// Returns encoder by model name or null.
    /// </summary>
    /// <param name="modelName">gpt-3.5-turbo</param>
    /// <returns></returns>
    public static Encoder? TryForModel(string modelName)
    {
        var encoding = TryGetEncodingByModel(modelName);
        
        return encoding == null
            ? null
            : new Encoder(encoding);
    }
    
    private static Dictionary<string, Encoding> ModelToEncoding { get; } = new()
    {
        // chat
        { "gpt-4o", new O200KBase() },
        { "gpt-4", new Cl100KBase() },
        { "gpt-3.5-turbo", new Cl100KBase() },
        { "gpt-35-turbo", new Cl100KBase() }, // Azure deployment name
        
        // embeddings
        { "text-embedding-ada-002", new Cl100KBase() },
        { "text-embedding-3-small", new Cl100KBase() },
        { "text-embedding-3-large", new Cl100KBase() },
    };

    /// <summary>
    /// Returns encoding by model name or null.
    /// </summary>
    /// <param name="modelName">gpt-4 gpt-3.5-turbo ...</param>
    /// <exception cref="ArgumentException"></exception>
    /// <returns></returns>
    public static Encoding? TryGetEncodingByModel(string modelName)
    {
        return ModelToEncoding
            .FirstOrDefault(a => modelName.StartsWith(a.Key, StringComparison.Ordinal)).Value;
    }

    /// <summary>
    /// Returns encoding by model name or throws exception.
    /// </summary>
    /// <param name="modelName">gpt-4 gpt-3.5-turbo ...</param>
    /// <exception cref="ArgumentException"></exception>
    /// <returns></returns>
    public static Encoding GetEncodingByModel(string modelName)
    {
        return TryGetEncodingByModel(modelName) ??
               throw new ArgumentException($"Model name {modelName} is not supported.");
    }
}