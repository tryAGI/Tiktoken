using System.Globalization;
using System.Reflection;
using Tiktoken.Models;
using Tiktoken.Utilities;

namespace Tiktoken.Services;

internal static class EncodingManager
{
    const string EndOfText = "<|endoftext|>";
    const string FimPrefix = "<|fim_prefix|>";
    const string FimMiddle = "<|fim_middle|>";
    const string FimSuffix = "<|fim_suffix|>";
    const string EndOfPrompt = "<|endofprompt|>";

    private static Dictionary<string, string> ModelToEncoding { get; } = new()
    {
        // chat
        { "gpt-4", "cl100k_base" },
        { "gpt-3.5-turbo", "cl100k_base" },
        { "gpt-35-turbo", "cl100k_base" }, // Azure deployment name
        // text
        { "text-davinci-003", "p50k_base" },
        { "text-davinci-002", "p50k_base" },
        { "text-davinci-001", "r50k_base" },
        { "text-curie-001", "r50k_base" },
        { "text-babbage-001", "r50k_base" },
        { "text-ada-001", "r50k_base" },
        { "davinci", "r50k_base" },
        { "curie", "r50k_base" },
        { "babbage", "r50k_base" },
        { "ada", "r50k_base" },
        // code
        { "code-davinci-002", "p50k_base" },
        { "code-davinci-001", "p50k_base" },
        { "code-cushman-002", "p50k_base" },
        { "code-cushman-001", "p50k_base" },
        { "davinci-codex", "p50k_base" },
        { "cushman-codex", "p50k_base" },
        // edit
        { "text-davinci-edit-001", "p50k_edit" },
        { "code-davinci-edit-001", "p50k_edit" },
        // embeddings
        { "text-embedding-ada-002", "cl100k_base" },
        // old embeddings
        { "text-similarity-davinci-001", "r50k_base" },
        { "text-similarity-curie-001", "r50k_base" },
        { "text-similarity-babbage-001", "r50k_base" },
        { "text-similarity-ada-001", "r50k_base" },
        { "text-search-davinci-doc-001", "r50k_base" },
        { "text-search-curie-doc-001", "r50k_base" },
        { "text-search-babbage-doc-001", "r50k_base" },
        { "text-search-ada-doc-001", "r50k_base" },
        { "code-search-babbage-code-001", "r50k_base" },
        { "code-search-ada-code-001", "r50k_base" },
        // open source
        { "gpt2", "gpt2" },
    };

    /// <summary>
    /// Get encoding setting with model name.
    /// </summary>
    /// <param name="modelName">gpt-4 gpt-3.5-turbo ...</param>
    /// <returns></returns>
    public static EncodingSettingModel GetEncodingSettingsForModel(string modelName)
    {
        var encodingName = ModelToEncoding
            .FirstOrDefault(a => a.Key.StartsWith(modelName, StringComparison.Ordinal)).Value ??
            throw new InvalidOperationException($"Model name {modelName} is not supported.");

        return GetEncoding(encodingName);
    }

    /// <summary>
    /// Get encoding setting with encoding name.
    /// </summary>
    /// <param name="encodingName">cl100k_base p50k_base ...</param>
    /// <returns></returns>
    internal static EncodingSettingModel GetEncoding(string encodingName)
    {
        if (string.IsNullOrEmpty(encodingName))
        {
            throw new ArgumentException("encodingName is null or empty", nameof(encodingName));
        }

        return encodingName switch
        {
            "gpt2" => throw new NotImplementedException("Unsupported encoding"),
            "r50k_base" => r50k_base(),
            "p50k_base" => p50k_base(),
            "p50k_edit" => p50k_edit(),
            "cl100k_base" => cl100k_base(),
            _ => throw new NotImplementedException("Unsupported encoding"),
        };
    }

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

    private static EncodingSettingModel r50k_base()
    {
        return new EncodingSettingModel()
        {
            Name = "p50k_base",
            ExplicitNVocab = 50257,
            PatStr = @"'s|'t|'re|'ve|'m|'ll|'d| ?\p{L}+| ?\p{N}+| ?[^\s\p{L}\p{N}]+|\s+(?!\S)|\s+",
            MergeableRanks = Load("r50k_base.tiktoken"),
            SpecialTokens = new Dictionary<string, int>
            {
                [EndOfText] = 50256,
            },
        };
    }

    private static EncodingSettingModel p50k_base()
    {
        return new EncodingSettingModel
        {
            Name = "p50k_base",
            ExplicitNVocab = 50281,
            PatStr = @"'s|'t|'re|'ve|'m|'ll|'d| ?\p{L}+| ?\p{N}+| ?[^\s\p{L}\p{N}]+|\s+(?!\S)|\s+",
            MergeableRanks = Load("p50k_base.tiktoken"),
            SpecialTokens = new Dictionary<string, int>
            {
                [EndOfText] = 50256,
            },
        };
    }

    private static EncodingSettingModel p50k_edit()
    {
        return new EncodingSettingModel
        {
            Name = "p50k_edit",
            ExplicitNVocab = 50281,
            PatStr = @"'s|'t|'re|'ve|'m|'ll|'d| ?\p{L}+| ?\p{N}+| ?[^\s\p{L}\p{N}]+|\s+(?!\S)|\s+",
            MergeableRanks = Load("p50k_base.tiktoken"),
            SpecialTokens = new Dictionary<string, int>
            {
                [EndOfText] = 50256,
                [FimPrefix] = 50281,
                [FimMiddle] = 50282,
                [FimSuffix] = 50283,
            },
        };
    }
    
    private static EncodingSettingModel cl100k_base()
    {
        return new EncodingSettingModel
        {
            Name = "cl100k_base",
            PatStr = @"(?i:'s|'t|'re|'ve|'m|'ll|'d)|[^\r\n\p{L}\p{N}]?\p{L}+|\p{N}{1,3}| ?[^\s\p{L}\p{N}]+[\r\n]*|\s*[\r\n]+|\s+(?!\S)|\s+",
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
    }
}