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
        
        _corePbe = new CoreBpe(encoding.MergeableRanks, encoding.SpecialTokens, encoding.Pattern);
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

#if NET7_0_OR_GREATER
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

#if NET7_0_OR_GREATER
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
#if NET7_0_OR_GREATER
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