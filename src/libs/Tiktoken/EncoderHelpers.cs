using Tiktoken.Encodings;

namespace Tiktoken;

// ReSharper disable InconsistentNaming

internal static class EncoderHelpers
{
    /// <summary>
    /// Returns encoding by model name.
    /// </summary>
    /// <param name="modelName">gpt-3.5-turbo</param>
    /// <returns></returns>
    public static Encoder ForModel(string modelName)
    {
        return new Encoder(GetNameByModel(modelName));
    }
    
    /// <summary>
    /// Returns encoding by model name or null.
    /// </summary>
    /// <param name="modelName">gpt-3.5-turbo</param>
    /// <returns></returns>
    public static Encoder? TryForModel(string modelName)
    {
        var encoding = TryGetNameByModel(modelName);
        
        return encoding == null
            ? null
            : new Encoder(encoding);
    }
    
    private static Dictionary<string, Encodings.Encoding> ModelToEncoding { get; } = new()
    {
        // chat
        { "gpt-4o", new O200KBase() },
        { "gpt-4", new Cl100KBase() },
        { "gpt-3.5-turbo", new Cl100KBase() },
        { "gpt-35-turbo", new Cl100KBase() }, // Azure deployment name
        // embeddings
        { "text-embedding-ada-002", new Cl100KBase() },
    };

    /// <summary>
    /// Returns encoding name by model name or null.
    /// </summary>
    /// <param name="modelName">gpt-4 gpt-3.5-turbo ...</param>
    /// <exception cref="ArgumentException"></exception>
    /// <returns></returns>
    public static Encoding? TryGetNameByModel(string modelName)
    {
        return ModelToEncoding
            .FirstOrDefault(a => modelName.StartsWith(a.Key, StringComparison.Ordinal)).Value;
    }

    /// <summary>
    /// Returns encoding name by model name or throws exception.
    /// </summary>
    /// <param name="modelName">gpt-4 gpt-3.5-turbo ...</param>
    /// <exception cref="ArgumentException"></exception>
    /// <returns></returns>
    public static Encoding GetNameByModel(string modelName)
    {
        return TryGetNameByModel(modelName) ??
               throw new ArgumentException($"Model name {modelName} is not supported.");
    }
}