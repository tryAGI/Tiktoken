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
        
        _corePbe = new CoreBpe(encoding.MergeableRanks, encoding.SpecialTokens, encoding.Pattern, encoding.CompiledRegex);
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
        return _corePbe.EncodeNative(
            text,
            allowedSpecial: EmptyHashSet,
            disallowedSpecial: _specialTokensSet);
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
        var bytes = _corePbe.DecodeNative(tokens);
        
        return System.Text.Encoding.UTF8.GetString(bytes);
    }
}