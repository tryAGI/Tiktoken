using System.Collections.Concurrent;
#if NET8_0_OR_GREATER
using System.Collections.Frozen;
#endif
using System.Runtime.CompilerServices;
#if NET8_0_OR_GREATER
using System.Runtime.InteropServices;
#endif
using System.Text;
using System.Text.RegularExpressions;
using Tiktoken.Core;
using Tiktoken.Encodings;
#if NET8_0_OR_GREATER
using System.Text.Unicode;
#endif

namespace Tiktoken;

/// <summary>
/// 
/// </summary>
public class CoreBpe
{
#if NET8_0_OR_GREATER
    private FrozenDictionary<string, int> SpecialTokensEncoder { get; set; }
    private FrozenDictionary<byte[], int> Encoder { get; set; }
#else
    private IReadOnlyDictionary<string, int> SpecialTokensEncoder { get; set; }
    private IReadOnlyDictionary<byte[], int> Encoder { get; set; }
#endif
#if NET8_0_OR_GREATER
    private FrozenDictionary<string, int> FastEncoder { get; set; }
#else
    private Dictionary<string, int> FastEncoder { get; set; }
#endif

    internal bool EnableCache { get; set; } = true;
    private ConcurrentDictionary<string, int[]> FastCache { get; set; } = new(StringComparer.Ordinal);
    private ConcurrentDictionary<string, int> FastCacheCounts { get; set; } = new(StringComparer.Ordinal);

    private Regex SpecialRegex { get; set; }
    private Regex Regex { get; set; }

#if NET8_0_OR_GREATER
    private FrozenDictionary<int, byte[]> Decoder => _lazyDecoder.Value;
    private FrozenDictionary<int, byte[]> SpecialTokensDecoderBytes => _lazySpecialTokensDecoderBytes.Value;
    private Lazy<FrozenDictionary<int, byte[]>> _lazyDecoder = null!;
    private Lazy<FrozenDictionary<int, byte[]>> _lazySpecialTokensDecoderBytes = null!;
#else
    private Dictionary<int, byte[]> Decoder => _lazyDecoder.Value;
    private Dictionary<int, byte[]> SpecialTokensDecoderBytes => _lazySpecialTokensDecoderBytes.Value;
    private Lazy<Dictionary<int, byte[]>> _lazyDecoder = null!;
    private Lazy<Dictionary<int, byte[]>> _lazySpecialTokensDecoderBytes = null!;
#endif

    /// <summary>
    ///
    /// </summary>
    /// <param name="encoder"></param>
    /// <param name="specialTokensEncoder"></param>
    /// <param name="pattern"></param>
    /// <param name="compiledRegex"></param>
    /// <param name="compiledSpecialRegex"></param>
    public CoreBpe(
        IReadOnlyDictionary<byte[], int> encoder,
        IReadOnlyDictionary<string, int> specialTokensEncoder,
        string pattern,
        Regex? compiledRegex = null,
        Regex? compiledSpecialRegex = null)
    {
        encoder = encoder ?? throw new ArgumentNullException(nameof(encoder));
        specialTokensEncoder = specialTokensEncoder ?? throw new ArgumentNullException(nameof(specialTokensEncoder));
        pattern = pattern ?? throw new ArgumentNullException(nameof(pattern));

#if NET8_0_OR_GREATER
        // Build FastEncoder dict in a single pass over encoder entries
        var fastEncoderDict = new Dictionary<string, int>(encoder.Count, StringComparer.Ordinal);
        Span<char> charBuf = stackalloc char[256];
        foreach (var kvp in encoder)
        {
            var chars = kvp.Key.Length <= 256 ? charBuf[..kvp.Key.Length] : new char[kvp.Key.Length];
            for (var i = 0; i < kvp.Key.Length; i++)
            {
                chars[i] = (char)kvp.Key[i];
            }
            fastEncoderDict[new string(chars)] = kvp.Value;
        }

        // Freeze Encoder and FastEncoder in parallel
        var comparer = new ByteArrayComparer();
        var frozenEncoderTask = System.Threading.Tasks.Task.Run(() => encoder.ToFrozenDictionary(comparer));
        var frozenFastEncoderTask = System.Threading.Tasks.Task.Run(() => fastEncoderDict.ToFrozenDictionary(StringComparer.Ordinal));

        // Build small dictionaries and compile regex while large dicts freeze in parallel
        SpecialTokensEncoder = specialTokensEncoder.ToFrozenDictionary(StringComparer.Ordinal);

        Regex = compiledRegex ?? new Regex(pattern, RegexOptions.Compiled);
        SpecialRegex = compiledSpecialRegex ?? new Regex("(" + string.Join("|", specialTokensEncoder.Keys.Select(Regex.Escape)) + ")", RegexOptions.Compiled);

        // Wait for parallel frozen dictionary construction
        Encoder = frozenEncoderTask.Result;
        FastEncoder = frozenFastEncoderTask.Result;

        // Lazy-init Decoder and SpecialTokensDecoderBytes (only built on first Decode call)
        var capturedEncoder = Encoder;
        _lazyDecoder = new Lazy<FrozenDictionary<int, byte[]>>(() =>
            capturedEncoder.ToDictionary(static x => x.Value, static x => x.Key).ToFrozenDictionary());
        var capturedSpecialTokensEncoder = specialTokensEncoder;
        _lazySpecialTokensDecoderBytes = new Lazy<FrozenDictionary<int, byte[]>>(() =>
            capturedSpecialTokensEncoder.ToFrozenDictionary(
                static x => x.Value, static x => System.Text.Encoding.UTF8.GetBytes(x.Key)));
#else
        Encoder = encoder;

#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
        FastEncoder = Encoder
            .ToDictionary(
                static x =>
                {
                    Span<char> chars = stackalloc char[x.Key.Length];
                    for (var i = 0; i < x.Key.Length; i++)
                    {
                        chars[i] = (char)x.Key[i];
                    }
                    return new string(chars);
                },
                static x => x.Value,
                StringComparer.Ordinal);
#else
        FastEncoder = Encoder
            .ToDictionary(
                static x => new string(x.Key.Select(static y => (char) y).ToArray()),
                static x => x.Value,
                StringComparer.Ordinal);
#endif

        SpecialTokensEncoder = specialTokensEncoder;

        Regex = compiledRegex ?? new Regex(pattern, RegexOptions.Compiled);
        SpecialRegex = compiledSpecialRegex ?? new Regex("(" + string.Join("|", specialTokensEncoder.Keys.Select(Regex.Escape)) + ")", RegexOptions.Compiled);

        // Lazy-init Decoder and SpecialTokensDecoderBytes (only built on first Decode call)
        var capturedEncoder = Encoder;
        _lazyDecoder = new Lazy<Dictionary<int, byte[]>>(() =>
            capturedEncoder.ToDictionary(static x => x.Value, static x => x.Key));
        var capturedSpecialTokensEncoder = specialTokensEncoder;
        _lazySpecialTokensDecoderBytes = new Lazy<Dictionary<int, byte[]>>(() =>
            capturedSpecialTokensEncoder.ToDictionary(
                static x => x.Value, static x => System.Text.Encoding.UTF8.GetBytes(x.Key)));
#endif
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    public int CountTokensNative(string text)
    {
        text = text ?? throw new ArgumentNullException(nameof(text));

#if NET7_0_OR_GREATER
        return CountTokensNative(text.AsSpan());
#else
        var tokens = 0;
        foreach (Match match in Regex.Matches(text))
        {
            var matchValue = match.Value;
            var fastKey = matchValue;

            if (IsAscii(fastKey) && FastEncoder.ContainsKey(fastKey))
            {
                tokens++;
                continue;
            }
            if (EnableCache && FastCacheCounts.TryGetValue(fastKey, out var fastNumberOfTokens))
            {
                tokens += fastNumberOfTokens;
                continue;
            }

            var piece = System.Text.Encoding.UTF8.GetBytes(matchValue);
            if (Encoder.ContainsKey(piece))
            {
                tokens++;
                if (EnableCache)
                {
                    FastCacheCounts[fastKey] = 1;
                }
                continue;
            }

            var numberOfTokens = BytePairEncoding.BytePairEncodeCountTokens(piece, Encoder);
            tokens += numberOfTokens;

            if (EnableCache)
            {
                FastCacheCounts[fastKey] = numberOfTokens;
            }
        }

        return tokens;
#endif
    }

#if NET7_0_OR_GREATER
    /// <summary>
    /// Counts tokens using span-based input to avoid string allocation.
    /// </summary>
    internal int CountTokensNative(ReadOnlySpan<char> text)
    {
        var tokens = 0;
        Span<byte> pieceBytes = stackalloc byte[512];
#if NET9_0_OR_GREATER
        var fastEncoderLookup = FastEncoder.GetAlternateLookup<ReadOnlySpan<char>>();
        var fastCacheCountLookup = FastCacheCounts.GetAlternateLookup<ReadOnlySpan<char>>();
        var encoderSpanLookup = Encoder.GetAlternateLookup<ReadOnlySpan<byte>>();
#endif

#if NET9_0_OR_GREATER
        foreach (var match in Regex.EnumerateMatches(text))
        {
            var fastKey = text.Slice(match.Index, match.Length);

            if (IsAscii(fastKey) && fastEncoderLookup.ContainsKey(fastKey))
            {
                tokens++;
                continue;
            }
            if (EnableCache && fastCacheCountLookup.TryGetValue(fastKey, out var fastNumberOfTokens))
            {
                tokens += fastNumberOfTokens;
                continue;
            }

            var pieceSpan = GetUtf8Span(fastKey, pieceBytes);
            if (encoderSpanLookup.ContainsKey(pieceSpan))
            {
                tokens++;
                if (EnableCache)
                {
                    fastCacheCountLookup[fastKey] = 1;
                }
                continue;
            }

            var numberOfTokens = BytePairEncoding.BytePairEncodeCountTokens(pieceSpan, encoderSpanLookup);
            tokens += numberOfTokens;

            if (EnableCache)
            {
                fastCacheCountLookup[fastKey] = numberOfTokens;
            }
        }
#else
        foreach (var match in Regex.EnumerateMatches(text))
        {
            var fastKey = new string(text.Slice(match.Index, match.Length));

            if (IsAscii(fastKey) && FastEncoder.ContainsKey(fastKey))
            {
                tokens++;
                continue;
            }
            if (EnableCache && FastCacheCounts.TryGetValue(fastKey, out var fastNumberOfTokens))
            {
                tokens += fastNumberOfTokens;
                continue;
            }

            var piece = GetUtf8Bytes(text.Slice(match.Index, match.Length), pieceBytes);
            if (Encoder.ContainsKey(piece))
            {
                tokens++;
                if (EnableCache)
                {
                    FastCacheCounts[fastKey] = 1;
                }
                continue;
            }

            var numberOfTokens = BytePairEncoding.BytePairEncodeCountTokens(piece, Encoder);
            tokens += numberOfTokens;

            if (EnableCache)
            {
                FastCacheCounts[fastKey] = numberOfTokens;
            }
        }
#endif

        return tokens;
    }
#endif
    
#if NET7_0_OR_GREATER
    /// <summary>
    /// Counts tokens from UTF-8 bytes, converting to chars internally using stackalloc/ArrayPool.
    /// </summary>
    internal int CountTokensFromUtf8(ReadOnlySpan<byte> utf8Text)
    {
        var charCount = System.Text.Encoding.UTF8.GetCharCount(utf8Text);
        if (charCount <= 1024)
        {
            Span<char> chars = stackalloc char[charCount];
            System.Text.Encoding.UTF8.GetChars(utf8Text, chars);
            return CountTokensNative(chars);
        }

        var rented = System.Buffers.ArrayPool<char>.Shared.Rent(charCount);
        try
        {
            System.Text.Encoding.UTF8.GetChars(utf8Text, rented.AsSpan(0, charCount));
            return CountTokensNative(rented.AsSpan(0, charCount));
        }
        finally
        {
            System.Buffers.ArrayPool<char>.Shared.Return(rented);
        }
    }

    /// <summary>
    /// Encodes UTF-8 bytes directly into a caller-provided token buffer.
    /// Converts to chars internally using stackalloc/ArrayPool, then encodes.
    /// </summary>
    internal int EncodeFromUtf8(
        ReadOnlySpan<byte> utf8Text,
        Span<int> tokenDestination,
        HashSet<string> disallowedSpecial)
    {
        var charCount = System.Text.Encoding.UTF8.GetCharCount(utf8Text);
        char[]? rentedChars = null;
        var chars = charCount <= 1024
            ? stackalloc char[charCount]
            : (rentedChars = System.Buffers.ArrayPool<char>.Shared.Rent(charCount)).AsSpan(0, charCount);

        try
        {
            System.Text.Encoding.UTF8.GetChars(utf8Text, chars);
            var tokens = EncodeNativeAllDisallowed(chars, disallowedSpecial);

            if (tokens.Count > tokenDestination.Length)
            {
                throw new ArgumentException(
                    "Destination buffer is too small. Use CountTokens to determine the required size.",
                    nameof(tokenDestination));
            }

            var i = 0;
            foreach (var token in tokens)
            {
                tokenDestination[i++] = token;
            }
            return tokens.Count;
        }
        finally
        {
            if (rentedChars != null)
            {
                System.Buffers.ArrayPool<char>.Shared.Return(rentedChars);
            }
        }
    }
#endif

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
        Span<byte> pieceBytes = stackalloc byte[512];
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
#if NET9_0_OR_GREATER
        var fastEncoderLookup = FastEncoder.GetAlternateLookup<ReadOnlySpan<char>>();
        var fastCacheLookup = FastCache.GetAlternateLookup<ReadOnlySpan<char>>();
        var encoderSpanLookup = Encoder.GetAlternateLookup<ReadOnlySpan<byte>>();
#endif
        foreach (var (specialStart, specialLength) in specialTokens)
        {
#if NET9_0_OR_GREATER
            foreach (var match in Regex.EnumerateMatches(textSpan[start..specialStart]))
            {
                var fastKey = textSpan.Slice(match.Index, match.Length);

                if (IsAscii(fastKey) && fastEncoderLookup.TryGetValue(fastKey, out var fastToken))
                {
                    tokens.Add(fastToken);
                    continue;
                }
                if (EnableCache && fastCacheLookup.TryGetValue(fastKey, out var fastTokens))
                {
                    tokens.AddRange(fastTokens);
                    continue;
                }

                var pieceSpan = GetUtf8Span(fastKey, pieceBytes);
                if (encoderSpanLookup.TryGetValue(pieceSpan, out var token))
                {
                    tokens.Add(token);
                    continue;
                }

                if (EnableCache)
                {
                    var pair = BytePairEncoding.BytePairEncodeToArray(pieceSpan, encoderSpanLookup);
                    tokens.AddRange(pair);
                    fastCacheLookup[fastKey] = pair;
                }
                else
                {
                    BytePairEncoding.BytePairEncode(pieceSpan, encoderSpanLookup, tokens);
                }
            }
#elif NET7_0_OR_GREATER
            foreach (var match in Regex.EnumerateMatches(textSpan[start..specialStart]))
            {
                var fastKey = new string(textSpan.Slice(match.Index, match.Length));

                if (IsAscii(fastKey) && FastEncoder.TryGetValue(fastKey, out var fastToken))
                {
                    tokens.Add(fastToken);
                    continue;
                }
                if (EnableCache && FastCache.TryGetValue(fastKey, out var fastTokens))
                {
                    tokens.AddRange(fastTokens);
                    continue;
                }

                var piece = GetUtf8Bytes(textSpan.Slice(match.Index, match.Length), pieceBytes);
                if (Encoder.TryGetValue(piece, out var token))
                {
                    tokens.Add(token);
                    continue;
                }

                if (EnableCache)
                {
                    var pair = BytePairEncoding.BytePairEncodeToArray(piece, Encoder);
                    tokens.AddRange(pair);
                    FastCache[fastKey] = pair;
                }
                else
                {
                    BytePairEncoding.BytePairEncode(piece, Encoder, tokens);
                }
            }
#else
            foreach (Match match in Regex.Matches(text[start..specialStart]))
            {
                var matchValue = match.Value;
                var fastKey = matchValue;

                if (IsAscii(fastKey) && FastEncoder.TryGetValue(fastKey, out var fastToken))
                {
                    tokens.Add(fastToken);
                    continue;
                }
                if (EnableCache && FastCache.TryGetValue(fastKey, out var fastTokens))
                {
                    tokens.AddRange(fastTokens);
                    continue;
                }

                var piece = System.Text.Encoding.UTF8.GetBytes(matchValue);
                if (Encoder.TryGetValue(piece, out var token))
                {
                    tokens.Add(token);
                    continue;
                }

                if (EnableCache)
                {
                    var pair = BytePairEncoding.BytePairEncodeToArray(piece, Encoder);
                    tokens.AddRange(pair);
                    FastCache[fastKey] = pair;
                }
                else
                {
                    BytePairEncoding.BytePairEncode(piece, Encoder, tokens);
                }
            }
#endif

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
    
#if NET7_0_OR_GREATER
    /// <summary>
    /// Optimized encode path for the common case where all special tokens are disallowed.
    /// Accepts ReadOnlySpan&lt;char&gt; to avoid string allocation when input is already a span.
    /// </summary>
    internal IReadOnlyCollection<int> EncodeNativeAllDisallowed(
        ReadOnlySpan<char> text,
        HashSet<string> disallowedSpecial)
    {
        // Any special token match is an error
        foreach (var match in SpecialRegex.EnumerateMatches(text))
        {
            var value = new string(text.Slice(match.Index, match.Length));
            if (disallowedSpecial.Contains(value))
            {
                throw new InvalidOperationException(value);
            }
        }

        var tokens = new List<int>();
        Span<byte> pieceBytes = stackalloc byte[512];

#if NET9_0_OR_GREATER
        var fastEncoderLookup = FastEncoder.GetAlternateLookup<ReadOnlySpan<char>>();
        var fastCacheLookup = FastCache.GetAlternateLookup<ReadOnlySpan<char>>();
        var encoderSpanLookup = Encoder.GetAlternateLookup<ReadOnlySpan<byte>>();

        foreach (var match in Regex.EnumerateMatches(text))
        {
            var fastKey = text.Slice(match.Index, match.Length);

            if (IsAscii(fastKey) && fastEncoderLookup.TryGetValue(fastKey, out var fastToken))
            {
                tokens.Add(fastToken);
                continue;
            }
            if (EnableCache && fastCacheLookup.TryGetValue(fastKey, out var fastTokens))
            {
                tokens.AddRange(fastTokens);
                continue;
            }

            var pieceSpan = GetUtf8Span(fastKey, pieceBytes);
            if (encoderSpanLookup.TryGetValue(pieceSpan, out var token))
            {
                tokens.Add(token);
                continue;
            }

            if (EnableCache)
            {
                var pair = BytePairEncoding.BytePairEncodeToArray(pieceSpan, encoderSpanLookup);
                tokens.AddRange(pair);
                fastCacheLookup[fastKey] = pair;
            }
            else
            {
                BytePairEncoding.BytePairEncode(pieceSpan, encoderSpanLookup, tokens);
            }
        }
#else
        foreach (var match in Regex.EnumerateMatches(text))
        {
            var fastKey = new string(text.Slice(match.Index, match.Length));

            if (IsAscii(fastKey) && FastEncoder.TryGetValue(fastKey, out var fastToken))
            {
                tokens.Add(fastToken);
                continue;
            }
            if (EnableCache && FastCache.TryGetValue(fastKey, out var fastTokens))
            {
                tokens.AddRange(fastTokens);
                continue;
            }

            var piece = GetUtf8Bytes(text.Slice(match.Index, match.Length), pieceBytes);
            if (Encoder.TryGetValue(piece, out var token))
            {
                tokens.Add(token);
                continue;
            }

            if (EnableCache)
            {
                var pair = BytePairEncoding.BytePairEncodeToArray(piece, Encoder);
                tokens.AddRange(pair);
                FastCache[fastKey] = pair;
            }
            else
            {
                BytePairEncoding.BytePairEncode(piece, Encoder, tokens);
            }
        }
#endif

        return tokens;
    }
#endif

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
        Span<byte> pieceBytes = stackalloc byte[512];
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
#if NET9_0_OR_GREATER
        var encoderSpanLookup = Encoder.GetAlternateLookup<ReadOnlySpan<byte>>();
#endif
        foreach (var (specialStart, specialLength) in specialTokens)
        {
#if NET9_0_OR_GREATER
            foreach (var match in Regex.EnumerateMatches(textSpan[start..specialStart]))
            {
                var matchSpan = textSpan.Slice(match.Index, match.Length);
                var fastKey = new string(matchSpan);

                var pieceSpan = GetUtf8Span(matchSpan, pieceBytes);
                if (encoderSpanLookup.ContainsKey(pieceSpan))
                {
                    values.Add(fastKey);
                    continue;
                }

                var pair = BytePairEncoding.BytePairExplore(pieceSpan, encoderSpanLookup);
                AddExploredParts(pair, values);
            }
#elif NET7_0_OR_GREATER
            foreach (var match in Regex.EnumerateMatches(textSpan[start..specialStart]))
            {
                var matchSpan = textSpan.Slice(match.Index, match.Length);
                var fastKey = new string(matchSpan);

                var piece = GetUtf8Bytes(matchSpan, pieceBytes);
                if (Encoder.ContainsKey(piece))
                {
                    values.Add(fastKey);
                    continue;
                }

                var pair = BytePairEncoding.BytePairExplore(piece, Encoder);
                AddExploredParts(pair, values);
            }
#else
            foreach (Match match in Regex.Matches(text[start..specialStart]))
            {
                var matchValue = match.Value;
                var fastKey = matchValue;

                var piece = System.Text.Encoding.UTF8.GetBytes(matchValue);
                if (Encoder.ContainsKey(piece))
                {
                    values.Add(fastKey);
                    continue;
                }

                var pair = BytePairEncoding.BytePairExplore(piece, Encoder);
                AddExploredParts(pair, values);
            }
#endif

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
        Span<byte> pieceBytes = stackalloc byte[512];
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
                FlushHighSurrogate(values, ref highSurrogate, ref highSurrogatePair, ref surrogateCount);

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
#if NET9_0_OR_GREATER
        var encoderSpanLookup = Encoder.GetAlternateLookup<ReadOnlySpan<byte>>();
#endif
        foreach (var (specialStart, specialLength) in specialTokens)
        {
#if NET9_0_OR_GREATER
            foreach (var match in Regex.EnumerateMatches(textSpan[start..specialStart]))
            {
                var matchSpan = textSpan.Slice(match.Index, match.Length);
                var fastKey = new string(matchSpan);

                var pieceSpan = GetUtf8Span(matchSpan, pieceBytes);
                if (encoderSpanLookup.ContainsKey(pieceSpan))
                {
                    FlushHighSurrogate(values, ref highSurrogate, ref highSurrogatePair, ref surrogateCount);
                    values.Add(new UtfToken(fastKey, 1));
                    continue;
                }

                var pair = BytePairEncoding.BytePairExplore(pieceSpan, encoderSpanLookup);
#elif NET7_0_OR_GREATER
            foreach (var match in Regex.EnumerateMatches(textSpan[start..specialStart]))
            {
                var matchSpan = textSpan.Slice(match.Index, match.Length);
                var fastKey = new string(matchSpan);

                var piece = GetUtf8Bytes(matchSpan, pieceBytes);
                if (Encoder.ContainsKey(piece))
                {
                    FlushHighSurrogate(values, ref highSurrogate, ref highSurrogatePair, ref surrogateCount);
                    values.Add(new UtfToken(fastKey, 1));
                    continue;
                }

                var pair = BytePairEncoding.BytePairExplore(piece, Encoder);
#else
            foreach (Match match in Regex.Matches(text[start..specialStart]))
            {
                var matchValue = match.Value;
                var fastKey = matchValue;

                var piece = System.Text.Encoding.UTF8.GetBytes(matchValue);
                if (Encoder.ContainsKey(piece))
                {
                    FlushHighSurrogate(values, ref highSurrogate, ref highSurrogatePair, ref surrogateCount);
                    values.Add(new UtfToken(fastKey, 1));
                    continue;
                }

                var pair = BytePairEncoding.BytePairExplore(piece, Encoder);
#endif

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
            if (Decoder.TryGetValue(token, out var value))
            {
                ret.AddRange(value);
            }
            else if (SpecialTokensDecoderBytes.TryGetValue(token, out var specialBytes))
            {
                ret.AddRange(specialBytes);
            }
        }
        return ret.ToArray();
    }

#if NET8_0_OR_GREATER
    /// <summary>
    /// Decodes tokens directly to string using a pooled buffer (single-pass).
    /// Dispatches to span-based overload for List&lt;int&gt; to avoid interface dispatch.
    /// </summary>
    internal string DecodeToString(IReadOnlyCollection<int> tokens)
    {
        tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));

        if (tokens is List<int> list)
        {
            return DecodeToString(CollectionsMarshal.AsSpan(list));
        }

        if (tokens.Count == 0)
        {
            return string.Empty;
        }

        var rented = System.Buffers.ArrayPool<byte>.Shared.Rent(tokens.Count * 6);
        try
        {
            var offset = 0;
            foreach (var token in tokens)
            {
                if (Decoder.TryGetValue(token, out var value))
                {
                    EnsureCapacity(ref rented, offset, value.Length);
                    value.CopyTo(rented.AsSpan(offset));
                    offset += value.Length;
                }
                else if (SpecialTokensDecoderBytes.TryGetValue(token, out var specialBytes))
                {
                    EnsureCapacity(ref rented, offset, specialBytes.Length);
                    specialBytes.CopyTo(rented.AsSpan(offset));
                    offset += specialBytes.Length;
                }
            }

            return offset == 0
                ? string.Empty
                : System.Text.Encoding.UTF8.GetString(rented.AsSpan(0, offset));
        }
        finally
        {
            System.Buffers.ArrayPool<byte>.Shared.Return(rented);
        }
    }

    /// <summary>
    /// Decodes tokens directly to string using span-based iteration (zero interface dispatch).
    /// </summary>
    internal string DecodeToString(ReadOnlySpan<int> tokens)
    {
        if (tokens.Length == 0)
        {
            return string.Empty;
        }

        var rented = System.Buffers.ArrayPool<byte>.Shared.Rent(tokens.Length * 6);
        try
        {
            var offset = 0;
            for (var i = 0; i < tokens.Length; i++)
            {
                if (Decoder.TryGetValue(tokens[i], out var value))
                {
                    EnsureCapacity(ref rented, offset, value.Length);
                    if (value.Length == 1)
                    {
                        rented[offset] = value[0];
                    }
                    else
                    {
                        value.CopyTo(rented.AsSpan(offset));
                    }
                    offset += value.Length;
                }
                else if (SpecialTokensDecoderBytes.TryGetValue(tokens[i], out var specialBytes))
                {
                    EnsureCapacity(ref rented, offset, specialBytes.Length);
                    specialBytes.CopyTo(rented.AsSpan(offset));
                    offset += specialBytes.Length;
                }
            }

            return offset == 0
                ? string.Empty
                : System.Text.Encoding.UTF8.GetString(rented.AsSpan(0, offset));
        }
        finally
        {
            System.Buffers.ArrayPool<byte>.Shared.Return(rented);
        }
    }

    /// <summary>
    /// Writes decoded UTF-8 bytes into caller-provided buffer. Returns bytes written.
    /// </summary>
    internal int DecodeToUtf8(ReadOnlySpan<int> tokens, Span<byte> utf8Destination)
    {
        var offset = 0;
        for (var i = 0; i < tokens.Length; i++)
        {
            byte[]? value;
            if (Decoder.TryGetValue(tokens[i], out value))
            {
                if (offset + value.Length > utf8Destination.Length)
                {
                    throw new ArgumentException(
                        "Destination buffer is too small. Use GetDecodedUtf8ByteCount to determine the required size.",
                        nameof(utf8Destination));
                }

                if (value.Length == 1)
                {
                    utf8Destination[offset] = value[0];
                }
                else
                {
                    value.CopyTo(utf8Destination.Slice(offset));
                }
                offset += value.Length;
            }
            else if (SpecialTokensDecoderBytes.TryGetValue(tokens[i], out var specialBytes))
            {
                if (offset + specialBytes.Length > utf8Destination.Length)
                {
                    throw new ArgumentException(
                        "Destination buffer is too small. Use GetDecodedUtf8ByteCount to determine the required size.",
                        nameof(utf8Destination));
                }

                specialBytes.CopyTo(utf8Destination.Slice(offset));
                offset += specialBytes.Length;
            }
        }
        return offset;
    }

    /// <summary>
    /// Returns the number of UTF-8 bytes required to decode the given tokens.
    /// </summary>
    internal int GetDecodedUtf8ByteCount(ReadOnlySpan<int> tokens)
    {
        var count = 0;
        for (var i = 0; i < tokens.Length; i++)
        {
            if (Decoder.TryGetValue(tokens[i], out var value))
            {
                count += value.Length;
            }
            else if (SpecialTokensDecoderBytes.TryGetValue(tokens[i], out var specialBytes))
            {
                count += specialBytes.Length;
            }
        }
        return count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void EnsureCapacity(ref byte[] rented, int offset, int needed)
    {
        if (offset + needed <= rented.Length)
        {
            return;
        }

        var newRented = System.Buffers.ArrayPool<byte>.Shared.Rent(
            Math.Max(rented.Length * 2, offset + needed));
        rented.AsSpan(0, offset).CopyTo(newRented);
        System.Buffers.ArrayPool<byte>.Shared.Return(rented);
        rented = newRented;
    }
#endif

    // FastEncoder maps byte values to chars, which creates false matches for
    // non-ASCII characters (U+0080-U+00FF) where the char code equals a single
    // byte value in the vocabulary. E.g., 'ª' (U+00AA) would falsely match the
    // FastEncoder key for byte [0xAA], but its correct UTF-8 encoding is [0xC2, 0xAA].
    // We must only use FastEncoder for ASCII-only strings.
#if NET8_0_OR_GREATER
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsAscii(ReadOnlySpan<char> text) => System.Text.Ascii.IsValid(text);
#elif NET7_0_OR_GREATER
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsAscii(ReadOnlySpan<char> text)
    {
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] > 127) return false;
        }
        return true;
    }
#else
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsAscii(string text)
    {
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] > 127) return false;
        }
        return true;
    }
#endif

    private static void AddExploredParts(List<byte[]> pair, List<string> values)
    {
        foreach (var bytes in pair)
        {
            values.Add(System.Text.Encoding.UTF8.GetString(bytes));
        }
    }

    private static void FlushHighSurrogate(
        List<UtfToken> values,
        ref bool highSurrogate,
        ref string highSurrogatePair,
        ref int surrogateCount)
    {
        if (highSurrogate)
        {
            values.Add(new UtfToken(highSurrogatePair, surrogateCount));
            surrogateCount = 0;
            highSurrogate = false;
        }
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ReadOnlySpan<byte> GetUtf8Span(ReadOnlySpan<char> text, Span<byte> scratch)
    {
        if (text.Length * 3 < scratch.Length)
        {
            return scratch[..System.Text.Encoding.UTF8.GetBytes(text, scratch)];
        }
        else
        {
            return System.Text.Encoding.UTF8.GetBytes(text.ToArray());
        }
    }
#endif
}