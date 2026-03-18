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

    /// <summary>
    /// Creates an encoder for the specified encoding name (e.g., "cl100k_base", "o200k_base").
    /// </summary>
    /// <param name="encodingName">Encoding name (cl100k_base, o200k_base, p50k_base, p50k_edit, r50k_base).</param>
    /// <returns>An <see cref="Encoder"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when the encoding name is not supported.</exception>
    public static Encoder CreateForEncoding(string encodingName)
    {
        return new Encoder(ModelToEncoding.ForEncoding(encodingName));
    }

    /// <summary>
    /// Counts total tokens for chat messages using the specified model's encoding.
    /// </summary>
    /// <example>
    /// <code>
    /// var messages = new List&lt;ChatMessage&gt;
    /// {
    ///     new("system", "You are a helpful assistant."),
    ///     new("user", "What is 2+2?"),
    /// };
    /// int count = TikTokenEncoder.CountMessageTokens("gpt-4o", messages);
    /// </code>
    /// </example>
    /// <param name="modelName">Model name (e.g., "gpt-4o").</param>
    /// <param name="messages">The chat messages.</param>
    /// <returns>The total token count.</returns>
    public static int CountMessageTokens(string modelName, IReadOnlyList<ChatMessage> messages)
    {
        return CreateForModel(modelName).CountMessageTokens(messages);
    }

    /// <summary>
    /// Counts total tokens for chat messages and tool definitions using the specified model's encoding.
    /// </summary>
    /// <param name="modelName">Model name (e.g., "gpt-4o").</param>
    /// <param name="messages">The chat messages.</param>
    /// <param name="tools">The function/tool definitions.</param>
    /// <returns>The total token count including messages and tools.</returns>
    public static int CountMessageTokens(
        string modelName,
        IReadOnlyList<ChatMessage> messages,
        IReadOnlyList<ChatFunction> tools)
    {
        return CreateForModel(modelName).CountMessageTokens(messages, tools);
    }
}
