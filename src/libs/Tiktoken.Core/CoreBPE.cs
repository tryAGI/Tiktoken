using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using Tiktoken.Core;
#if NET8_0_OR_GREATER
using System.Text.Unicode;
#endif

namespace Tiktoken;

/// <summary>
/// 
/// </summary>
public class CoreBpe
{
    private IReadOnlyDictionary<string, int> SpecialTokensEncoder { get; set; }
    private IReadOnlyDictionary<byte[], int> Encoder { get; set; }
    private Dictionary<string, int> FastEncoder { get; set; }

    internal bool EnableCache { get; set; } = true;
    private ConcurrentDictionary<string, IReadOnlyCollection<int>> FastCache { get; set; } = new();
    private ConcurrentDictionary<string, int> FastCacheCounts { get; set; } = new(
#if NET9_0_OR_GREATER
        new AlternateStringComparer()
#endif
        );

    private Regex SpecialRegex { get; set; }
    private Regex Regex { get; set; }

    private Dictionary<int, byte[]> Decoder { get; set; }
    private Dictionary<int, string> SpecialTokensDecoder { get; set; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="encoder"></param>
    /// <param name="specialTokensEncoder"></param>
    /// <param name="pattern"></param>
    public CoreBpe(
        IReadOnlyDictionary<byte[], int> encoder,
        IReadOnlyDictionary<string, int> specialTokensEncoder,
        string pattern)
    {
        encoder = encoder ?? throw new ArgumentNullException(nameof(encoder));
        specialTokensEncoder = specialTokensEncoder ?? throw new ArgumentNullException(nameof(specialTokensEncoder));
        pattern = pattern ?? throw new ArgumentNullException(nameof(pattern));
        
        Encoder = encoder;
        FastEncoder = Encoder
            .ToDictionary(
#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
                static x =>
                {
                    Span<char> chars = stackalloc char[x.Key.Length];
                    for (var i = 0; i < x.Key.Length; i++)
                    {
                        chars[i] = (char)x.Key[i];
                    }
                    return new string(chars);
                },
#else
                static x => new string(x.Key.Select(static y => (char) y).ToArray()),
#endif
                static x => x.Value
#if NET9_0_OR_GREATER
                , new AlternateStringComparer()
#endif
                );
        SpecialTokensEncoder = specialTokensEncoder;
        
        Regex = new Regex(pattern, RegexOptions.Compiled);
        SpecialRegex = new Regex("(" + string.Join("|", specialTokensEncoder.Keys.Select(Regex.Escape)) + ")", RegexOptions.Compiled);

        Decoder = Encoder
            .ToDictionary(
                static x => x.Value,
                static x => x.Key);
        SpecialTokensDecoder = specialTokensEncoder
            .ToDictionary(
                static x => x.Value,
                static x => x.Key);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    public int CountTokensNative(string text)
    {
        text = text ?? throw new ArgumentNullException(nameof(text));
        
        var tokens = 0;
#if NET7_0_OR_GREATER
        var textSpan = text.AsSpan();
        Span<byte> pieceBytes = stackalloc byte[128];
#endif
#if NET9_0_OR_GREATER
        var fastEncoderLookup = FastEncoder.GetAlternateLookup<ReadOnlySpan<char>>();
        var fastCacheCountLookup = FastCacheCounts.GetAlternateLookup<ReadOnlySpan<char>>();
#endif

#if NET7_0_OR_GREATER
        foreach (var match in Regex.EnumerateMatches(textSpan))
        {
#if NET9_0_OR_GREATER
            var fastKey = textSpan.Slice(match.Index, match.Length);
#else
            var fastKey = new string(textSpan.Slice(match.Index, match.Length));
#endif
#else
        foreach (Match match in Regex.Matches(text))
        {
            var matchValue = match.Value;
            var fastKey = matchValue;
#endif

#if NET9_0_OR_GREATER
            if (fastEncoderLookup.ContainsKey(fastKey))
#else
            if (FastEncoder.ContainsKey(fastKey))
#endif
            {
                tokens++;
                continue;
            }
#if NET9_0_OR_GREATER
            if (EnableCache && fastCacheCountLookup.TryGetValue(fastKey, out var fastNumberOfTokens))
#else
            if (EnableCache && FastCacheCounts.TryGetValue(fastKey, out var fastNumberOfTokens))
#endif
            {
                tokens += fastNumberOfTokens;
                continue;
            }

#if NET7_0_OR_GREATER
            var piece = GetUtf8Bytes(textSpan.Slice(match.Index, match.Length), pieceBytes);
#else
            var piece = System.Text.Encoding.UTF8.GetBytes(matchValue);
#endif
            if (Encoder.ContainsKey(piece))
            {
                tokens++;
                continue;
            }
            
            var numberOfTokens = BytePairEncoding.BytePairEncodeCountTokens(piece, Encoder);
            tokens += numberOfTokens;

            if (EnableCache)
            {
#if NET9_0_OR_GREATER
                fastCacheCountLookup[fastKey] = numberOfTokens;
#else
                FastCacheCounts[fastKey] = numberOfTokens;
#endif
            }
        }

        return tokens;
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="text"></param>
    /// <param name="allowedSpecial"></param>
    /// <param name="disallowedSpecial"></param>
    /// <returns></returns>
    public IReadOnlyCollection<int> EncodeNative(
        string text,
        HashSet<string> allowedSpecial,
        HashSet<string> disallowedSpecial)
    {
        text = text ?? throw new ArgumentNullException(nameof(text));
        allowedSpecial = allowedSpecial ?? throw new ArgumentNullException(nameof(allowedSpecial));
        disallowedSpecial = disallowedSpecial ?? throw new ArgumentNullException(nameof(disallowedSpecial));
        
        var tokens = new List<int>();
#if NET7_0_OR_GREATER
        var textSpan = text.AsSpan();
        Span<byte> pieceBytes = stackalloc byte[128];
#endif

        var specialTokens = new List<(int Index, int Length)>(capacity: 32);
#if NET7_0_OR_GREATER
        foreach (var match in SpecialRegex.EnumerateMatches(textSpan))
        {
            var value = textSpan.Slice(start: match.Index, length: match.Length).ToString();
#else
        foreach (Match match in SpecialRegex.Matches(text))
        {
            var value = match.Value;
#endif
            if (disallowedSpecial.Contains(value))
            {
                throw new InvalidOperationException(value);
            }
            if (allowedSpecial.Contains(value))
            {
                specialTokens.Add((match.Index, match.Length));
            }
            else
            {
                throw new InvalidOperationException("Invalid special token sets");
            }
        }
        specialTokens.Add((Index: text.Length, Length: 0));

        var start = 0;
        foreach (var (specialStart, specialLength) in specialTokens)
        {
#if NET7_0_OR_GREATER
            foreach (var match in Regex.EnumerateMatches(textSpan[start..specialStart]))
            {
                var fastKey = new string(textSpan.Slice(match.Index, match.Length));
#else
            foreach (Match match in Regex.Matches(text[start..specialStart]))
            {
                var matchValue = match.Value;
                var fastKey = matchValue;
#endif
                if (FastEncoder.TryGetValue(fastKey, out var fastToken))
                {
                    tokens.Add(fastToken);
                    continue;
                }
                if (EnableCache && FastCache.TryGetValue(fastKey, out var fastTokens))
                {
                    tokens.AddRange(fastTokens);
                    continue;
                }

#if NET7_0_OR_GREATER
                var piece = GetUtf8Bytes(textSpan.Slice(match.Index, match.Length), pieceBytes);
#else
                var piece = System.Text.Encoding.UTF8.GetBytes(matchValue);
#endif
                if (Encoder.TryGetValue(piece, out var token))
                {
                    tokens.Add(token);
                    continue;
                }
                
                var pair = BytePairEncoding.BytePairEncode(piece, Encoder);
                tokens.AddRange(pair);

                if (EnableCache)
                {
                    FastCache[fastKey] = pair;
                }
            }

            if (specialLength != 0)
            {
                start = specialStart + specialLength;
                
#if NET7_0_OR_GREATER
                var piece = new string(textSpan.Slice(specialStart, specialLength));
#else
                var piece = text.Substring(specialStart, specialLength);
#endif
                var token = SpecialTokensEncoder[piece];
                tokens.Add(token);
            }
        }

        return tokens;
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="text"></param>
    /// <param name="allowedSpecial"></param>
    /// <param name="disallowedSpecial"></param>
    /// <returns></returns>
    public IReadOnlyCollection<string> Explore(
        string text,
        HashSet<string> allowedSpecial,
        HashSet<string> disallowedSpecial)
    {
        text = text ?? throw new ArgumentNullException(nameof(text));
        allowedSpecial = allowedSpecial ?? throw new ArgumentNullException(nameof(allowedSpecial));
        disallowedSpecial = disallowedSpecial ?? throw new ArgumentNullException(nameof(disallowedSpecial));
        
        var values = new List<string>();
#if NET7_0_OR_GREATER
        var textSpan = text.AsSpan();
#endif

        var specialTokens = new List<(int Index, int Length)>(capacity: 32);
#if NET7_0_OR_GREATER
        foreach (var match in SpecialRegex.EnumerateMatches(textSpan))
        {
            var value = textSpan.Slice(start: match.Index, length: match.Length).ToString();
#else
        foreach (Match match in SpecialRegex.Matches(text))
        {
            var value = match.Value;
#endif
            if (disallowedSpecial.Contains(value))
            {
                throw new InvalidOperationException(value);
            }
            if (allowedSpecial.Contains(value))
            {
                specialTokens.Add((match.Index, match.Length));
            }
            else
            {
                throw new InvalidOperationException("Invalid special token sets");
            }
        }
        specialTokens.Add((Index: text.Length, Length: 0));

        var start = 0;
        foreach (var (specialStart, specialLength) in specialTokens)
        {
#if NET7_0_OR_GREATER
            foreach (var match in Regex.EnumerateMatches(textSpan[start..specialStart]))
            {
                var matchValue = textSpan.Slice(match.Index, match.Length).ToArray();
                var fastKey = new string(textSpan.Slice(match.Index, match.Length));
#else
            foreach (Match match in Regex.Matches(text[start..specialStart]))
            {
                var matchValue = match.Value;
                var fastKey = matchValue;
#endif

                var piece = System.Text.Encoding.UTF8.GetBytes(matchValue);
                if (Encoder.ContainsKey(piece))
                {
                    values.Add(fastKey);
                    continue;
                }
                
                var pair = BytePairEncoding.BytePairExplore(piece, Encoder);
                foreach (var bytes in pair)
                {
                    var value = System.Text.Encoding.UTF8.GetString(bytes);
                    
                    values.Add(value);
                }
            }

            if (specialLength != 0)
            {
                start = specialStart + specialLength;
                
#if NET7_0_OR_GREATER
                var piece = new string(textSpan.Slice(specialStart, specialLength));
#else
                var piece = text.Substring(specialStart, specialLength);
#endif
                values.Add(piece);
            }
        }

        return values;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="text"></param>
    /// <param name="allowedSpecial"></param>
    /// <param name="disallowedSpecial"></param>
    /// <returns></returns>
    public IReadOnlyCollection<UtfToken> ExploreUtfSafe(
        string text,
        HashSet<string> allowedSpecial,
        HashSet<string> disallowedSpecial)
    {
        text = text ?? throw new ArgumentNullException(nameof(text));
        allowedSpecial = allowedSpecial ?? throw new ArgumentNullException(nameof(allowedSpecial));
        disallowedSpecial = disallowedSpecial ?? throw new ArgumentNullException(nameof(disallowedSpecial));
        
        var values = new List<UtfToken>();
#if NET7_0_OR_GREATER
        var textSpan = text.AsSpan();
#endif
        var specialTokens = new List<(int Index, int Length)>(capacity: 32);
        
        int accuCount = 1;
        bool highSurrogate = false;
        int surrogateCount = 0;
        string highSurrogatePair = string.Empty;

#if NET7_0_OR_GREATER
        foreach (var match in SpecialRegex.EnumerateMatches(textSpan))
        {
            var value = textSpan.Slice(start: match.Index, length: match.Length).ToString();
#else
        foreach (Match match in SpecialRegex.Matches(text))
        {
            var value = match.Value;
#endif
            if (disallowedSpecial.Contains(value))
            {
                throw new InvalidOperationException(value);
            }
            if (allowedSpecial.Contains(value))
            {
                if (highSurrogate)
                {
                    values.Add(new UtfToken(highSurrogatePair, surrogateCount));
                    surrogateCount = 0;
                    highSurrogate = false;
                }
                
                specialTokens.Add((match.Index, match.Length));
            }
            else
            {
                throw new InvalidOperationException("Invalid special token sets");
            }
        }
        specialTokens.Add((Index: text.Length, Length: 0));

        List<byte> accumulatedBytes = [];
        
        #if !NET8_0_OR_GREATER
        var encoding = new UTF8Encoding(false, true);
        #endif
        
        var start = 0;
        foreach (var (specialStart, specialLength) in specialTokens)
        {
#if NET7_0_OR_GREATER
            foreach (var match in Regex.EnumerateMatches(textSpan[start..specialStart]))
            {
                var matchValue = textSpan.Slice(match.Index, match.Length).ToArray();
                var fastKey = new string(textSpan.Slice(match.Index, match.Length));
#else
            foreach (Match match in Regex.Matches(text[start..specialStart]))
            {
                var matchValue = match.Value;
                var fastKey = matchValue;
#endif

                var piece = System.Text.Encoding.UTF8.GetBytes(matchValue);
                if (Encoder.ContainsKey(piece))
                {
                    if (highSurrogate)
                    {
                        values.Add(new UtfToken(highSurrogatePair, surrogateCount));
                        surrogateCount = 0;
                        highSurrogate = false;
                    }
                    
                    values.Add(new UtfToken(fastKey, 1));
                    continue;
                }
                
                var pair = BytePairEncoding.BytePairExplore(piece, Encoder);
                
                accumulatedBytes.Clear();

                for (int i = 0; i < pair.Count; i++)
                {
                    byte[] currentPair = pair[i];
                    accumulatedBytes.AddRange(currentPair);
                    byte[] accuArr = [.. accumulatedBytes];

                    #if NET8_0_OR_GREATER
                    bool isValid = Utf8.IsValid(accuArr);
                    #else
                    bool isValid = true;

                    try
                    {
                        var _ = encoding.GetString(accuArr);
                    }
                    catch (ArgumentException)
                    {
                        isValid = false;
                    }
                    #endif
                    
                    if (isValid)
                    {
                        string value = System.Text.Encoding.UTF8.GetString(accuArr);

                        if (highSurrogate)
                        {
                            string possiblePair = $"{highSurrogatePair}{value}";
                            
                            if (char.IsSurrogatePair(possiblePair, 0))
                            {
                                values.Add(new UtfToken($"{highSurrogatePair}{value}", accuCount + surrogateCount));    
                            }
                            else
                            {
                                values.Add(new UtfToken(highSurrogatePair, accuCount));    
                                values.Add(new UtfToken(value, surrogateCount));    
                            }
                            
                            highSurrogate = false;
                            highSurrogatePair = string.Empty;
                        }
                        else
                        {
                            if (char.IsHighSurrogate(value[0]))
                            {
                                highSurrogate = true;
                                highSurrogatePair = value;
                                surrogateCount = accuCount;
                            }
                            else
                            {
                                values.Add(new UtfToken(value, accuCount));
                            }   
                        }
                        
                        accumulatedBytes.Clear();
                        accuCount = 1;   
                    }
                    else
                    {
                        accuCount++;
                    }
                }

                if (accumulatedBytes.Count > 0)
                {
                    var value = System.Text.Encoding.UTF8.GetString([.. accumulatedBytes]);
                    values.Add(new UtfToken(value, accuCount));
                    accumulatedBytes.Clear();
                }
            }

            if (specialLength != 0)
            {
                start = specialStart + specialLength;
                
#if NET7_0_OR_GREATER
                var piece = new string(textSpan.Slice(specialStart, specialLength));
#else
                var piece = text.Substring(specialStart, specialLength);
#endif
                values.Add(new UtfToken(piece, 1));
            }
        }

        return values;
    }
            
    /// <summary>
    /// 
    /// </summary>
    /// <param name="tokens"></param>
    /// <returns></returns>
    public byte[] DecodeNative(IReadOnlyCollection<int> tokens)
    {
        tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
        
        var ret = new List<byte>(tokens.Count * 2);
        foreach (var token in tokens)
        {
            byte[] tokenBytes = Array.Empty<byte>();
            if (Decoder.TryGetValue(token, out var value))
            {
                tokenBytes = value;
            } 
            else
            {
                if (SpecialTokensDecoder.TryGetValue(token, out var valueS))
                {
                    tokenBytes = System.Text.Encoding.UTF8.GetBytes(valueS);
                }
            }

            if (tokenBytes.Length > 0)
            {
                ret.AddRange(tokenBytes);
            } 
        }
        return ret.ToArray();
    }

#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte[] GetUtf8Bytes(ReadOnlySpan<char> text, Span<byte> scratch)
    {
        // check if text can be decoded into the buffer; each UTF-16 char can become at most 3 UTF-8 bytes
        if (text.Length * 3 < scratch.Length)
        {
            return scratch[..System.Text.Encoding.UTF8.GetBytes(text, scratch)].ToArray();
        }
        else
        {
            return System.Text.Encoding.UTF8.GetBytes(text.ToArray());
        }
    }
#endif

#if NET9_0_OR_GREATER
    private sealed class AlternateStringComparer : IEqualityComparer<string>,
        IAlternateEqualityComparer<ReadOnlySpan<char>, string>
    {
        public string Create(ReadOnlySpan<char> alternate)
        {
            return new(alternate);
        }

        public bool Equals(string? x, string? y)
        {
            return string.Equals(x, y, StringComparison.Ordinal);
        }

        public bool Equals(ReadOnlySpan<char> alternate, string other)
        {
            return other?.AsSpan().SequenceEqual(alternate) ?? false;
        }

        public int GetHashCode([DisallowNull] string str)
        {
            return str is null ? 0 : GetHashCode(str.AsSpan());
        }

        public int GetHashCode(ReadOnlySpan<char> alternate)
        {
            // use the djb2 hash function for simplicity: http://www.cse.yorku.ca/~oz/hash.html
            uint hash = 5381;
            foreach (var ch in alternate)
                hash = hash * 33u + ch;
            return (int)hash;
        }
    }
#endif
}