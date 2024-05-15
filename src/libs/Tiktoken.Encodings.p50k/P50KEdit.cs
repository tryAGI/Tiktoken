using System.Reflection;
using static Tiktoken.Encodings.EncodingConstants;

namespace Tiktoken.Encodings;

/// <inheritdoc />
public class P50KEdit : Encoding
{
    /// <inheritdoc />
    public P50KEdit() : base(
        name: "p50k_base",
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
        mergeableRanks: Assembly.GetExecutingAssembly().LoadEncodingFromManifestResource("p50k_base.tiktoken"),
        specialTokens: new Dictionary<string, int>
        {
            [EndOfText] = 50256,
            [FimPrefix] = 50281,
            [FimMiddle] = 50282,
            [FimSuffix] = 50283,
        })
    {
    }
}