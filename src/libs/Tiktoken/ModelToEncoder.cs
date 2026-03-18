using System.Collections.Concurrent;
using Tiktoken.Encodings;

namespace Tiktoken;

/// <summary>
///
/// </summary>
public static class ModelToEncoder
{
    private static readonly ConcurrentDictionary<Encoding, Encoder> Cache = new();

    /// <summary>
    /// Returns a cached encoder by model name.
    /// Encoder instances are shared across models that use the same encoding.
    /// </summary>
    /// <param name="modelName">gpt-3.5-turbo</param>
    /// <returns></returns>
    public static Encoder For(string modelName)
    {
        var encoding = ModelToEncoding.For(modelName);

        return Cache.GetOrAdd(encoding, static e => new Encoder(e));
    }

    /// <summary>
    /// Returns a cached encoder by model name or null.
    /// Encoder instances are shared across models that use the same encoding.
    /// </summary>
    /// <param name="modelName">gpt-3.5-turbo</param>
    /// <returns></returns>
    public static Encoder? TryFor(string modelName)
    {
        var encoding = ModelToEncoding.TryFor(modelName);

        return encoding == null
            ? null
            : Cache.GetOrAdd(encoding, static e => new Encoder(e));
    }
}
