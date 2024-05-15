namespace Tiktoken;

/// <summary>
/// 
/// </summary>
/// <param name="token"></param>
/// <param name="encodedTokens"></param>
public class UtfToken(
    string token,
    int encodedTokens)
{
    /// <summary>
    /// 
    /// </summary>
    public string Token { get; private set; } = token;

    /// <summary>
    /// 
    /// </summary>
    public int EncodedTokens { get; internal set; } = encodedTokens;
}