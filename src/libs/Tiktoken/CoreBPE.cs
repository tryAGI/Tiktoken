using System.Text.RegularExpressions;
using Tiktoken.Utilities;

namespace Tiktoken;

/// <summary>
/// 
/// </summary>
public class CoreBpe
{
    private IReadOnlyDictionary<string, int> SpecialTokensEncoder { get; set; }
    private IReadOnlyDictionary<byte[], int> Encoder { get; set; }

    private Regex SpecialRegex { get; set; }
    private Regex Regex { get; set; }

    private IReadOnlyDictionary<int, byte[]> Decoder { get; set; }
    private IReadOnlyDictionary<int, string> SpecialTokensDecoder { get; set; }
    
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

        var specialTokens = new List<(int Index, int Length)>(capacity: 32);
#if NET7_0_OR_GREATER
        foreach (var match in SpecialRegex.EnumerateMatches(text))
        {
            var value = text.AsSpan().Slice(start: match.Index, length: match.Length).ToString();
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
            foreach (var match in Regex.EnumerateMatches(text.AsSpan()[start..specialStart]))
            {
                var matchValue = text.AsSpan().Slice(match.Index, match.Length).ToArray();
#else
            foreach (Match match in Regex.Matches(text.Substring(start, specialStart - start)))
            {
                var matchValue = match.Value;
#endif
                var piece = System.Text.Encoding.UTF8.GetBytes(matchValue);
                if (Encoder.TryGetValue(piece, out int token))
                {
                    tokens.Add(token);
                    continue;
                }
                var pair = BytePairEncoding.BytePairEncode(piece, Encoder);
                tokens.AddRange(pair);
            }

            if (specialLength != 0)
            {
                start = specialStart + specialLength;
                
#if NET7_0_OR_GREATER
                var piece = new string(text.AsSpan().Slice(specialStart, specialLength));
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
}