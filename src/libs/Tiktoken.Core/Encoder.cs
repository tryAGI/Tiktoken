using Tiktoken.Encodings;

namespace Tiktoken;

/// <summary>
/// 
/// </summary>
public class Encoder
{
    private readonly CoreBpe _corePbe;
    private readonly HashSet<string> _specialTokensSet;
    private static readonly HashSet<string> EmptyHashSet = [];
    
    /// <summary>
    /// Enable cache for fast encoding.
    /// Default: true.
    /// </summary>
    public bool EnableCache
    {
        get => _corePbe.EnableCache;
        set => _corePbe.EnableCache = value;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="encoding"></param>
    public Encoder(Encoding encoding)
    {
        encoding = encoding ?? throw new ArgumentNullException(nameof(encoding));
        
        _corePbe = new CoreBpe(encoding.MergeableRanks, encoding.SpecialTokens, encoding.Pattern, encoding.CompiledRegex, encoding.CompiledSpecialRegex);
        _specialTokensSet = [..encoding.SpecialTokens.Keys];
    }

    /// <summary>
    /// Counts tokens in fast mode. Does not take into account special tokens.
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    public int CountTokens(string text)
    {
        return _corePbe.CountTokensNative(text);
    }

#if NET8_0_OR_GREATER
    /// <summary>
    /// Counts tokens from a span without requiring a string allocation.
    /// Does not take into account special tokens.
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    public int CountTokens(ReadOnlySpan<char> text)
    {
        return _corePbe.CountTokensNative(text);
    }

    /// <summary>
    /// Counts tokens directly from UTF-8 bytes, avoiding caller-side string allocation.
    /// Converts to chars internally using stackalloc for small inputs, ArrayPool for large ones.
    /// </summary>
    /// <param name="utf8Text"></param>
    /// <returns></returns>
    public int CountTokens(ReadOnlySpan<byte> utf8Text)
    {
        return _corePbe.CountTokensFromUtf8(utf8Text);
    }

    /// <summary>
    /// Encodes UTF-8 text directly into a caller-provided token buffer for zero-allocation encode.
    /// Use <see cref="CountTokens(ReadOnlySpan{byte})"/> to determine the required buffer size.
    /// </summary>
    /// <param name="utf8Text">The UTF-8 encoded text to tokenize.</param>
    /// <param name="tokenDestination">The destination buffer for token IDs.</param>
    /// <returns>The number of tokens written to <paramref name="tokenDestination"/>.</returns>
    /// <exception cref="ArgumentException">The destination buffer is too small.</exception>
    public int EncodeUtf8(ReadOnlySpan<byte> utf8Text, Span<int> tokenDestination)
    {
        return _corePbe.EncodeFromUtf8(utf8Text, tokenDestination, _specialTokensSet);
    }
#endif
    
    /// <summary>
    ///
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public IReadOnlyCollection<int> Encode(string text)
    {
        return EncodeWithAllDisallowedSpecial(text);
    }

#if NET8_0_OR_GREATER
    /// <summary>
    /// Encodes text from a span without string allocation (on NET9+ uses zero-copy dictionary lookups).
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public IReadOnlyCollection<int> Encode(ReadOnlySpan<char> text)
    {
        return _corePbe.EncodeNativeAllDisallowed(text, _specialTokensSet);
    }
#endif
    
    /// <summary>
    /// Returns tokens from the processing stage as a list of strings.
    /// This would enhance visibility over the tokenization process, facilitate token manipulation,
    /// and could serve as a useful tool for educational purposes.
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public IReadOnlyCollection<string> Explore(string text)
    {
        return _corePbe.Explore(
            text,
            allowedSpecial: _specialTokensSet,
            disallowedSpecial: EmptyHashSet);
    }
    
    /// <summary>
    /// Returns tokens from the processing stage as a list of strings.
    /// This would enhance visibility over the tokenization process, facilitate token manipulation,
    /// and could serve as a useful tool for educational purposes.
    /// Unlike <see cref="Explore"/> this method returns token in a printable manner, in which each token is encoded as one more tokens.
    /// For example, Cl100KBase can encode 🤚🏾 (Raised Back of Hand: Dark Skin Tone) with as much as 6 tokens.
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    public IReadOnlyCollection<UtfToken> ExploreUtfSafe(string text)
    {
        return _corePbe.ExploreUtfSafe(
            text,
            allowedSpecial: _specialTokensSet,
            disallowedSpecial: EmptyHashSet);
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public IReadOnlyCollection<int> EncodeWithAllAllowedSpecial(string text)
    {
        return _corePbe.EncodeNative(
            text,
            allowedSpecial: _specialTokensSet,
            disallowedSpecial: EmptyHashSet);
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public IReadOnlyCollection<int> EncodeWithAllDisallowedSpecial(string text)
    {
#if NET8_0_OR_GREATER
        return _corePbe.EncodeNativeAllDisallowed(text.AsSpan(), _specialTokensSet);
#else
        return _corePbe.EncodeNative(
            text,
            allowedSpecial: EmptyHashSet,
            disallowedSpecial: _specialTokensSet);
#endif
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="text"></param>
    /// <param name="allowedSpecial"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public IReadOnlyCollection<int> EncodeWithAllowedSpecial(
        string text,
        IReadOnlyCollection<string> allowedSpecial)
    {
        allowedSpecial = allowedSpecial ?? throw new ArgumentNullException(nameof(allowedSpecial));
        
        return _corePbe.EncodeNative(
            text,
            allowedSpecial: [..allowedSpecial],
            disallowedSpecial: [.._specialTokensSet.Except(allowedSpecial)]);
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="text"></param>
    /// <param name="disallowedSpecial"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public IReadOnlyCollection<int> EncodeWithDisallowedSpecial(
        string text,
        IReadOnlyCollection<string> disallowedSpecial)
    {
        disallowedSpecial = disallowedSpecial ?? throw new ArgumentNullException(nameof(disallowedSpecial));
        
        return _corePbe.EncodeNative(
            text,
            allowedSpecial: [.._specialTokensSet.Except(disallowedSpecial)],
            disallowedSpecial: [..disallowedSpecial]);
    }

    /// <summary>
    /// Counts total tokens for a list of chat messages using OpenAI's token counting formula.
    /// Each message adds <paramref name="tokensPerMessage"/> overhead tokens (default 3).
    /// If a message has a <see cref="ChatMessage.Name"/>, <paramref name="tokensPerName"/> extra tokens are added (default 1).
    /// An additional 3 tokens are added at the end for reply priming.
    /// </summary>
    /// <remarks>
    /// Based on the official OpenAI token counting cookbook:
    /// https://cookbook.openai.com/examples/how_to_count_tokens_with_tiktoken
    /// <para>
    /// The default values (tokensPerMessage=3, tokensPerName=1) are correct for
    /// gpt-4o, gpt-4, gpt-3.5-turbo, and all newer models.
    /// </para>
    /// </remarks>
    /// <param name="messages">The chat messages to count tokens for.</param>
    /// <param name="tokensPerMessage">Overhead tokens added per message (default: 3).</param>
    /// <param name="tokensPerName">Extra tokens when a message has a name (default: 1).</param>
    /// <returns>The total token count including message overhead and reply priming.</returns>
    public int CountMessageTokens(
        IReadOnlyList<ChatMessage> messages,
        int tokensPerMessage = 3,
        int tokensPerName = 1)
    {
        messages = messages ?? throw new ArgumentNullException(nameof(messages));

        var count = 0;
        for (var i = 0; i < messages.Count; i++)
        {
            var message = messages[i];
            count += tokensPerMessage;
            count += CountTokens(message.Role);
            count += CountTokens(message.Content);
            if (message.Name != null)
            {
                count += CountTokens(message.Name);
                count += tokensPerName;
            }
        }

        count += 3; // every reply is primed with <|start|>assistant<|message|>
        return count;
    }

    /// <summary>
    /// Counts total tokens for chat messages plus function/tool definitions.
    /// </summary>
    /// <remarks>
    /// Tool token counting is based on reverse-engineered formulas calibrated against OpenAI's
    /// /v1/responses/input_tokens endpoint. OpenAI internally converts function definitions to a
    /// TypeScript namespace format before tokenizing. Constants: sectionOverhead=10, perFunction=7,
    /// propInit=4, propKey=2.
    /// <para>
    /// For exact counts, use OpenAI's server-side token counting API.
    /// </para>
    /// </remarks>
    /// <param name="messages">The chat messages.</param>
    /// <param name="tools">The function/tool definitions.</param>
    /// <param name="tokensPerMessage">Overhead tokens per message (default: 3).</param>
    /// <param name="tokensPerName">Extra tokens when a message has a name (default: 1).</param>
    /// <returns>The total token count for messages and tools.</returns>
    public int CountMessageTokens(
        IReadOnlyList<ChatMessage> messages,
        IReadOnlyList<ChatFunction> tools,
        int tokensPerMessage = 3,
        int tokensPerName = 1)
    {
        tools = tools ?? throw new ArgumentNullException(nameof(tools));

        var count = CountMessageTokens(messages, tokensPerMessage, tokensPerName);
        count += CountToolTokens(tools);
        return count;
    }

    /// <summary>
    /// Counts tokens consumed by function/tool definitions.
    /// Adds a one-time section overhead (10 tokens) plus per-function overhead (7 tokens each)
    /// plus encoded name:description and property costs.
    /// </summary>
    /// <remarks>
    /// Constants calibrated against OpenAI's /v1/responses/input_tokens endpoint:
    /// sectionOverhead=10, perFunction=7, propInit=4, propKey=2, enumInit=-3, enumItem=2.
    /// </remarks>
    /// <param name="tools">The function/tool definitions.</param>
    /// <returns>The token count for the tool definitions.</returns>
    public int CountToolTokens(IReadOnlyList<ChatFunction> tools)
    {
        tools = tools ?? throw new ArgumentNullException(nameof(tools));

        // Overhead constants calibrated against OpenAI's /v1/responses/input_tokens endpoint.
        // The tools section has a one-time wrapper overhead (namespace declaration)
        // plus per-function overhead for each tool definition.
        const int sectionOverhead = 10; // namespace functions { ... } // namespace functions
        const int perFunction = 7;      // type name = (_: { ... }) => any;

        if (tools.Count == 0)
        {
            return 0;
        }

        var count = sectionOverhead;
        for (var i = 0; i < tools.Count; i++)
        {
            var tool = tools[i];
            count += perFunction;

            // Tokenize "name:description" (trailing period stripped per OpenAI behavior)
            var desc = tool.Description.TrimEnd('.');
            count += CountTokens(tool.Name + ":" + desc);

            if (tool.Parameters != null && tool.Parameters.Count > 0)
            {
                count += CountParameterTokens(tool.Parameters);
            }
        }

        return count;
    }

    private int CountParameterTokens(IReadOnlyList<FunctionParameter> parameters)
    {
        const int propInit = 4;
        const int propKey = 2;
        const int enumInit = -3;
        const int enumItem = 2;

        var count = propInit;

        for (var i = 0; i < parameters.Count; i++)
        {
            var param = parameters[i];
            count += propKey;

            // Tokenize "key:type:description" (trailing period stripped)
            var paramDesc = param.Description.TrimEnd('.');
            string type;

            if (param.AnyOf != null && param.AnyOf.Count > 0)
            {
                // anyOf union: "string | number"
                type = string.Join(" | ", param.AnyOf);
            }
            else if (param.Type == "array" && param.ArrayItemType != null)
            {
                // Array with item type: "string[]"
                type = param.ArrayItemType + "[]";
            }
            else
            {
                type = param.Type;
            }

            count += CountTokens(param.Name + ":" + type + ":" + paramDesc);

            if (param.EnumValues != null && param.EnumValues.Count > 0)
            {
                count += enumInit;
                for (var k = 0; k < param.EnumValues.Count; k++)
                {
                    count += enumItem;
                    count += CountTokens(param.EnumValues[k]);
                }
            }

            // Recurse into nested object properties
            if (param.Properties != null && param.Properties.Count > 0)
            {
                count += CountParameterTokens(param.Properties);
            }
        }

        return count;
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="tokens"></param>
    /// <returns></returns>
    public string Decode(IReadOnlyCollection<int> tokens)
    {
#if NET8_0_OR_GREATER
        return _corePbe.DecodeToString(tokens);
#else
        var bytes = _corePbe.DecodeNative(tokens);

        return System.Text.Encoding.UTF8.GetString(bytes);
#endif
    }

#if NET8_0_OR_GREATER
    /// <summary>
    /// Decodes tokens to string using span-based iteration for maximum performance.
    /// </summary>
    /// <param name="tokens"></param>
    /// <returns></returns>
    public string Decode(ReadOnlySpan<int> tokens)
    {
        return _corePbe.DecodeToString(tokens);
    }

    /// <summary>
    /// Decodes tokens directly to UTF-8 bytes in a caller-provided buffer for zero-allocation decode.
    /// </summary>
    /// <param name="tokens">The tokens to decode.</param>
    /// <param name="utf8Destination">The destination buffer for UTF-8 bytes.</param>
    /// <returns>The number of bytes written to <paramref name="utf8Destination"/>.</returns>
    /// <exception cref="ArgumentException">The destination buffer is too small.</exception>
    public int DecodeToUtf8(ReadOnlySpan<int> tokens, Span<byte> utf8Destination)
    {
        return _corePbe.DecodeToUtf8(tokens, utf8Destination);
    }

    /// <summary>
    /// Returns the number of UTF-8 bytes required to decode the given tokens.
    /// Use this to determine the required buffer size for <see cref="DecodeToUtf8"/>.
    /// </summary>
    /// <param name="tokens">The tokens to measure.</param>
    /// <returns>The number of UTF-8 bytes required.</returns>
    public int GetDecodedUtf8ByteCount(ReadOnlySpan<int> tokens)
    {
        return _corePbe.GetDecodedUtf8ByteCount(tokens);
    }
#endif
}