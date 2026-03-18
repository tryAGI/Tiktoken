namespace Tiktoken;

/// <summary>
/// Convenience entry point for creating token encoders.
/// Provides a more intuitive API alternative to <see cref="ModelToEncoder"/>.
/// </summary>
public static class TikTokenEncoder
{
    /// <summary>
    /// Creates a cached encoder for the specified model.
    /// Encoder instances are shared across models that use the same encoding.
    /// </summary>
    /// <example>
    /// <code>
    /// var encoder = TikTokenEncoder.CreateForModel("gpt-4o");
    /// var tokens = encoder.Encode("hello world");
    /// </code>
    /// </example>
    /// <param name="modelName">Model name (e.g., "gpt-4o", "gpt-4", "gpt-3.5-turbo").</param>
    /// <returns>A cached <see cref="Encoder"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when the model name is not supported.</exception>
    public static Encoder CreateForModel(string modelName)
    {
        return ModelToEncoder.For(modelName);
    }

    /// <summary>
    /// Creates a cached encoder for the specified model, or returns null if not found.
    /// </summary>
    /// <param name="modelName">Model name.</param>
    /// <returns>A cached <see cref="Encoder"/> or null.</returns>
    public static Encoder? TryCreateForModel(string modelName)
    {
        return ModelToEncoder.TryFor(modelName);
    }
}
