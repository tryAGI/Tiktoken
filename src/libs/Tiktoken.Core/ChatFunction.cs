namespace Tiktoken;

/// <summary>
/// Represents a function/tool definition for token counting.
/// Maps to OpenAI's function calling schema.
/// </summary>
public class ChatFunction
{
    /// <summary>
    /// The name of the function.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// A description of what the function does.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// The function parameters. Null or empty if the function takes no parameters.
    /// </summary>
    public IReadOnlyList<FunctionParameter>? Parameters { get; set; }

    /// <summary>
    /// Creates a new function definition.
    /// </summary>
    public ChatFunction()
    {
    }

    /// <summary>
    /// Creates a new function definition with the specified properties.
    /// </summary>
    public ChatFunction(string name, string description, IReadOnlyList<FunctionParameter>? parameters = null)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        Parameters = parameters;
    }
}

/// <summary>
/// Represents a single parameter in a function definition.
/// </summary>
public class FunctionParameter
{
    /// <summary>
    /// The parameter name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The parameter type (e.g., "string", "number", "boolean", "integer", "array", "object").
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// A description of the parameter.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Whether this parameter is required.
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// Optional enum values for the parameter.
    /// </summary>
    public IReadOnlyList<string>? EnumValues { get; set; }

    /// <summary>
    /// Creates a new function parameter.
    /// </summary>
    public FunctionParameter()
    {
    }

    /// <summary>
    /// Creates a new function parameter with the specified properties.
    /// </summary>
    public FunctionParameter(
        string name,
        string type,
        string description = "",
        bool isRequired = false,
        IReadOnlyList<string>? enumValues = null)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Type = type ?? throw new ArgumentNullException(nameof(type));
        Description = description ?? string.Empty;
        IsRequired = isRequired;
        EnumValues = enumValues;
    }
}
