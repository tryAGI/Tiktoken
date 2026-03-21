using System.Reflection;
#if NET8_0_OR_GREATER
using System.Text.RegularExpressions;
#endif
using static Tiktoken.Encodings.EncodingConstants;

namespace Tiktoken.Encodings;

/// <inheritdoc />
#if NET8_0_OR_GREATER
public partial class Cl100KBase : Encoding
#else
public class Cl100KBase : Encoding
#endif
{
    /// <inheritdoc />
    public Cl100KBase() : base(
        name: "cl100k_base",
        patterns: [
            "(?i:'s|'t|'re|'ve|'m|'ll|'d)",
            @"[^\r\n\p{L}\p{N}]?\p{L}+",
            @"\p{N}{1,3}",
            @" ?[^\s\p{L}\p{N}]+[\r\n]*",
            @"\s*[\r\n]+",
            @"\s+(?!\S)",
            @"\s+",
        ],
        mergeableRanks: Assembly.GetExecutingAssembly().LoadEncodingFromManifestResource("cl100k_base.tiktoken"),
        specialTokens: new Dictionary<string, int>
        {
            [EndOfText] = 100257,
            [FimPrefix] = 100258,
            [FimMiddle] = 100259,
            [FimSuffix] = 100260,
            [EndOfPrompt] = 100276,
        })
    {
#if NET8_0_OR_GREATER
        CompiledRegex = TokenPattern();
        CompiledSpecialRegex = SpecialTokenPattern();
#endif
    }

#if NET8_0_OR_GREATER
    [GeneratedRegex(@"(?i:'s|'t|'re|'ve|'m|'ll|'d)|[^\r\n\p{L}\p{N}]?\p{L}+|\p{N}{1,3}| ?[^\s\p{L}\p{N}]+[\r\n]*|\s*[\r\n]+|\s+(?!\S)|\s+")]
    private static partial Regex TokenPattern();

    [GeneratedRegex(@"(<\|endoftext\|>|<\|fim_prefix\|>|<\|fim_middle\|>|<\|fim_suffix\|>|<\|endofprompt\|>)")]
    private static partial Regex SpecialTokenPattern();
#endif
}
