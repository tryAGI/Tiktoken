using System.Reflection;
#if NET8_0_OR_GREATER
using System.Text.RegularExpressions;
#endif
using static Tiktoken.Encodings.EncodingConstants;

namespace Tiktoken.Encodings;

/// <inheritdoc />
#if NET8_0_OR_GREATER
public partial class R50KBase : Encoding
#else
public class R50KBase : Encoding
#endif
{
    /// <inheritdoc />
    public R50KBase() : base(
        name: "r50k_base",
        patterns: [
            "'s",
            "'t",
            "'re",
            "'ve",
            "'m",
            "'ll",
            "'d",
            @" ?\p{L}+",
            @" ?\p{N}+",
            @" ?[^\s\p{L}\p{N}]+",
            @"\s+(?!\S)",
            @"\s+",
        ],
        mergeableRanks: Assembly.GetExecutingAssembly().LoadEncodingFromManifestResource("r50k_base.tiktoken"),
        specialTokens: new Dictionary<string, int>
        {
            [EndOfText] = 50256,
        })
    {
#if NET8_0_OR_GREATER
        CompiledRegex = TokenPattern();
        CompiledSpecialRegex = SpecialTokenPattern();
#endif
    }

#if NET8_0_OR_GREATER
    [GeneratedRegex(@"'s|'t|'re|'ve|'m|'ll|'d| ?\p{L}+| ?\p{N}+| ?[^\s\p{L}\p{N}]+|\s+(?!\S)|\s+")]
    private static partial Regex TokenPattern();

    [GeneratedRegex(@"(<\|endoftext\|>)")]
    private static partial Regex SpecialTokenPattern();
#endif
}
