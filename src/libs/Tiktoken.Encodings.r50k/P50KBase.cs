using System.Reflection;
using static Tiktoken.Encodings.EncodingConstants;

namespace Tiktoken.Encodings;

/// <inheritdoc />
public class R50KBase : Encoding
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
    }
}