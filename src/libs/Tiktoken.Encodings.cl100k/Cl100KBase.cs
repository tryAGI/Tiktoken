using System.Reflection;
using static Tiktoken.Encodings.EncodingConstants;

namespace Tiktoken.Encodings;

/// <inheritdoc />
public class Cl100KBase : Encoding
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
    }
}