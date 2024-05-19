namespace Tiktoken;

/// <summary>
/// 
/// </summary>
public static class ModelToEncoder
{
    /// <summary>
    /// Returns encoder by model name.
    /// </summary>
    /// <param name="modelName">gpt-3.5-turbo</param>
    /// <returns></returns>
    public static Encoder For(string modelName)
    {
        return new Encoder(ModelToEncoding.For(modelName));
    }
    
    /// <summary>
    /// Returns encoder by model name or null.
    /// </summary>
    /// <param name="modelName">gpt-3.5-turbo</param>
    /// <returns></returns>
    public static Encoder? TryFor(string modelName)
    {
        var encoding = ModelToEncoding.TryFor(modelName);
        
        return encoding == null
            ? null
            : new Encoder(encoding);
    }
}