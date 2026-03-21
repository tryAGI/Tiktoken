using System.Reflection;
#if NET8_0_OR_GREATER
using System.Text.RegularExpressions;
#endif
using static Tiktoken.Encodings.EncodingConstants;

namespace Tiktoken.Encodings;

/// <inheritdoc />
#if NET8_0_OR_GREATER
public partial class O200KBase : Encoding
#else
public class O200KBase : Encoding
#endif
{
    /// <inheritdoc />
    public O200KBase() : base(
        name: "o200k_base",
        patterns: [
            @"[^\r\n\p{L}\p{N}]?[\p{Lu}\p{Lt}\p{Lm}\p{Lo}\p{M}]*[\p{Ll}\p{Lm}\p{Lo}\p{M}]+(?i:'s|'t|'re|'ve|'m|'ll|'d)?",
            @"[^\r\n\p{L}\p{N}]?[\p{Lu}\p{Lt}\p{Lm}\p{Lo}\p{M}]+[\p{Ll}\p{Lm}\p{Lo}\p{M}]*(?i:'s|'t|'re|'ve|'m|'ll|'d)?",
            @"\p{N}{1,3}",
            @" ?[^\s\p{L}\p{N}]+[\r\n/]*",
            @"\s*[\r\n]+",
            @"\s+(?!\S)",
            @"\s+",
        ],
        mergeableRanks: Assembly.GetExecutingAssembly().LoadEncodingFromManifestResource("o200k_base.tiktoken"),
        specialTokens: new Dictionary<string, int>
        {
            [EndOfText] = 199999,
            [EndOfPrompt] = 200018,
        })
    {
#if NET8_0_OR_GREATER
        CompiledRegex = TokenPattern();
        CompiledSpecialRegex = SpecialTokenPattern();
#endif
    }

#if NET8_0_OR_GREATER
    [GeneratedRegex(@"[^\r\n\p{L}\p{N}]?[\p{Lu}\p{Lt}\p{Lm}\p{Lo}\p{M}]*[\p{Ll}\p{Lm}\p{Lo}\p{M}]+(?i:'s|'t|'re|'ve|'m|'ll|'d)?|[^\r\n\p{L}\p{N}]?[\p{Lu}\p{Lt}\p{Lm}\p{Lo}\p{M}]+[\p{Ll}\p{Lm}\p{Lo}\p{M}]*(?i:'s|'t|'re|'ve|'m|'ll|'d)?|\p{N}{1,3}| ?[^\s\p{L}\p{N}]+[\r\n/]*|\s*[\r\n]+|\s+(?!\S)|\s+")]
    private static partial Regex TokenPattern();

    [GeneratedRegex(@"(<\|endoftext\|>|<\|endofprompt\|>)")]
    private static partial Regex SpecialTokenPattern();
#endif
}
