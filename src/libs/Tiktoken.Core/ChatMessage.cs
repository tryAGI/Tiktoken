namespace Tiktoken;

/// <summary>
/// Represents a chat message for token counting purposes.
/// Compatible with OpenAI's message format.
/// </summary>
public class ChatMessage
{
    /// <summary>
    /// The role of the message sender (e.g., "system", "user", "assistant").
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// The text content of the message.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Optional name of the sender. When present, adds 1 extra token.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Creates a new chat message.
    /// </summary>
    public ChatMessage()
    {
    }

    /// <summary>
    /// Creates a new chat message with the specified role and content.
    /// </summary>
    public ChatMessage(string role, string content, string? name = null)
    {
        Role = role ?? throw new ArgumentNullException(nameof(role));
        Content = content ?? throw new ArgumentNullException(nameof(content));
        Name = name;
    }
}
