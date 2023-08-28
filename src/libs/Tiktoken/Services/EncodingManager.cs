using System.Globalization;
using System.Reflection;
using Tiktoken.Models;
using Tiktoken.Utilities;

namespace Tiktoken.Services;

// ReSharper disable InconsistentNaming

internal static class EncodingManager
{
    private const string EndOfText = "<|endoftext|>";
    private const string FimPrefix = "<|fim_prefix|>";
    private const string FimMiddle = "<|fim_middle|>";
    private const string FimSuffix = "<|fim_suffix|>";
    private const string EndOfPrompt = "<|endofprompt|>";

    /// <summary>
    /// Returns encoding setting by encoding name.
    /// </summary>
    /// <param name="encodingName">cl100k_base p50k_base ...</param>
    /// <returns></returns>
    internal static EncodingSettingModel Get(string encodingName) => encodingName switch
    {
        "gpt2" => throw new NotImplementedException("Unsupported encoding"),
        Encodings.R50KBase => r50k_base,
        Encodings.P50KBase => p50k_base,
        Encodings.P50KEdit => p50k_edit,
        Encodings.Cl100KBase => cl100k_base,
        _ => throw new NotImplementedException("Unsupported encoding"),
    };

    private static EncodingSettingModel r50k_base => new()
    {
        Name = Encodings.R50KBase,
        ExplicitNVocab = 50257,
        Pattern = @"'s|'t|'re|'ve|'m|'ll|'d| ?\p{L}+| ?\p{N}+| ?[^\s\p{L}\p{N}]+|\s+(?!\S)|\s+",
        MergeableRanks = Load("r50k_base.tiktoken"),
        SpecialTokens = new Dictionary<string, int>
        {
            [EndOfText] = 50256,
        },
    };

    private static EncodingSettingModel p50k_base => new()
    {
        Name = Encodings.P50KBase,
        ExplicitNVocab = 50281,
        Pattern = @"'s|'t|'re|'ve|'m|'ll|'d| ?\p{L}+| ?\p{N}+| ?[^\s\p{L}\p{N}]+|\s+(?!\S)|\s+",
        MergeableRanks = Load("p50k_base.tiktoken"),
        SpecialTokens = new Dictionary<string, int>
        {
            [EndOfText] = 50256,
        },
    };

    private static EncodingSettingModel p50k_edit => new()
    {
        Name = Encodings.P50KEdit,
        ExplicitNVocab = 50281,
        Pattern = @"'s|'t|'re|'ve|'m|'ll|'d| ?\p{L}+| ?\p{N}+| ?[^\s\p{L}\p{N}]+|\s+(?!\S)|\s+",
        MergeableRanks = Load("p50k_base.tiktoken"),
        SpecialTokens = new Dictionary<string, int>
        {
            [EndOfText] = 50256,
            [FimPrefix] = 50281,
            [FimMiddle] = 50282,
            [FimSuffix] = 50283,
        },
    };
    
    private static EncodingSettingModel cl100k_base => new()
    {
        Name = Encodings.Cl100KBase,
        Pattern = @"(?i:'s|'t|'re|'ve|'m|'ll|'d)|[^\r\n\p{L}\p{N}]?\p{L}+|\p{N}{1,3}| ?[^\s\p{L}\p{N}]+[\r\n]*|\s*[\r\n]+|\s+(?!\S)|\s+",
        MergeableRanks = Load("cl100k_base.tiktoken"),
        SpecialTokens = new Dictionary<string, int>
        {
            [EndOfText] = 100257,
            [FimPrefix] = 100258,
            [FimMiddle] = 100259,
            [FimSuffix] = 100260,
            [EndOfPrompt] = 100276,
        },
    };

    private static Dictionary<byte[], int> Load(string name)
    {
        var bpeDict = new Dictionary<byte[], int>(new ByteArrayComparer());

        var assembly = Assembly.GetExecutingAssembly();
        var resourcePath = assembly
            .GetManifestResourceNames()
            .Single(str => str.EndsWith(name, StringComparison.OrdinalIgnoreCase));

        using var stream =
            assembly.GetManifestResourceStream(resourcePath) ??
            throw new InvalidOperationException("Resource not found.");
        using var reader = new StreamReader(stream);
        
        while (reader.ReadLine() is { } line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var tokens = line.Split(' ');
            if (tokens.Length != 2)
            {
                throw new FormatException($"Invalid file format: {name}");
            }

            var tokenBytes = Convert.FromBase64String(tokens[0]);
            var rank = int.Parse(tokens[1], CultureInfo.InvariantCulture);
            bpeDict[tokenBytes] = rank;
        }

        return bpeDict;
    }
}