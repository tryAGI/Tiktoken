namespace Tiktoken;

public class UtfToken
{
    public string Token { get; private set; }
    public int EncodedTokens { get; internal set; }
    
    public UtfToken(string token, int encodedTokens)
    {
        Token = token;
        EncodedTokens = encodedTokens;
    }
}