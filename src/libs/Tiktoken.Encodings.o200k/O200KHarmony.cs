using System.Globalization;
using System.Text.RegularExpressions;

namespace Tiktoken.Encodings;

/// <summary>Harmony control tokens over the o200k_base ordinary-text vocabulary.</summary>
public class O200KHarmony : O200KBase
{
    /// <summary>Creates the Harmony encoding, including all upstream reserved tokens.</summary>
    public O200KHarmony()
    {
        Name = "o200k_harmony";
        var tokens = SpecialTokens.ToDictionary(static x => x.Key, static x => x.Value);
        tokens["<|startoftext|>"] = 199998;
        string[] names = ["reserved_200000", "reserved_200001", "return", "constrain",
            "reserved_200004", "channel", "start", "end", "message", "reserved_200009",
            "reserved_200010", "reserved_200011", "call"];
        for (var i = 0; i < names.Length; i++)
        {
            tokens["<|" + names[i] + "|>"] = 200000 + i;
        }
        for (var i = 200013; i < 201088; i++)
        {
            tokens["<|reserved_" + i.ToString(CultureInfo.InvariantCulture) + "|>"] = i;
        }
        SpecialTokens = tokens;
        CompiledSpecialRegex = new Regex("(" + string.Join("|", tokens.Keys.Select(Regex.Escape)) + ")", RegexOptions.Compiled);
    }
}
