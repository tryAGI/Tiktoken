using System.Text.Json;

namespace Tiktoken.Encodings;

/// <summary>
/// Loads a HuggingFace tokenizer.json file and converts it to a Tiktoken <see cref="Encoding"/>.
/// </summary>
public static class TokenizerJsonLoader
{
    // GPT-2 bytes_to_unicode mapping.
    // Printable ASCII + Latin-1 supplement bytes map to themselves.
    // Non-printable bytes map to unicode chars starting at U+0100.
    private static readonly Dictionary<char, byte> UnicodeToByte = BuildUnicodeToByte();

    /// <summary>
    /// Default regex pattern for GPT-2 ByteLevel pre-tokenizer.
    /// </summary>
    private static readonly string[] Gpt2Patterns =
    [
        @"'s|'t|'re|'ve|'m|'ll|'d",
        @" ?\p{L}+",
        @" ?\p{N}+",
        @" ?[^\s\p{L}\p{N}]+",
        @"\s+(?!\S)",
        @"\s+",
    ];

    /// <summary>
    /// Loads an <see cref="Encoding"/> from a tokenizer.json file path.
    /// </summary>
    /// <param name="path">Path to the tokenizer.json file.</param>
    /// <param name="name">Optional encoding name.</param>
    /// <param name="patterns">Optional regex patterns. If null, auto-detected from pre-tokenizer config.</param>
    /// <returns></returns>
    public static Encoding FromFile(string path, string? name = null, IReadOnlyList<string>? patterns = null)
    {
        path = path ?? throw new ArgumentNullException(nameof(path));

        var json = File.ReadAllText(path);

        return FromJson(json, name ?? Path.GetFileNameWithoutExtension(path), patterns);
    }

#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
    /// <summary>
    /// Loads an <see cref="Encoding"/> from a tokenizer.json file path asynchronously.
    /// </summary>
    /// <param name="path">Path to the tokenizer.json file.</param>
    /// <param name="name">Optional encoding name.</param>
    /// <param name="patterns">Optional regex patterns. If null, auto-detected from pre-tokenizer config.</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async System.Threading.Tasks.Task<Encoding> FromFileAsync(
        string path,
        string? name = null,
        IReadOnlyList<string>? patterns = null,
        System.Threading.CancellationToken cancellationToken = default)
    {
        path = path ?? throw new ArgumentNullException(nameof(path));

        var json = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);

        return FromJson(json, name ?? Path.GetFileNameWithoutExtension(path), patterns);
    }
#endif

    /// <summary>
    /// Loads an <see cref="Encoding"/> from a tokenizer.json string.
    /// </summary>
    /// <param name="json">The tokenizer.json content.</param>
    /// <param name="name">Encoding name.</param>
    /// <param name="patterns">Optional regex patterns. If null, auto-detected from pre-tokenizer config.</param>
    /// <returns></returns>
    public static Encoding FromJson(string json, string name = "tokenizer", IReadOnlyList<string>? patterns = null)
    {
        json = json ?? throw new ArgumentNullException(nameof(json));

        var tokenizer =
            JsonSerializer.Deserialize(json, SourceGenerationContext.Default.TokenizerJson) ??
            throw new InvalidOperationException("Failed to deserialize tokenizer.json.");

        return ToEncoding(tokenizer, name, patterns);
    }

    /// <summary>
    /// Loads an <see cref="Encoding"/> from a stream containing tokenizer.json data.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    /// <param name="name">Encoding name.</param>
    /// <param name="patterns">Optional regex patterns. If null, auto-detected from pre-tokenizer config.</param>
    /// <returns></returns>
    public static Encoding FromStream(Stream stream, string name = "tokenizer", IReadOnlyList<string>? patterns = null)
    {
        stream = stream ?? throw new ArgumentNullException(nameof(stream));

        var tokenizer =
            JsonSerializer.Deserialize(stream, SourceGenerationContext.Default.TokenizerJson) ??
            throw new InvalidOperationException("Failed to deserialize tokenizer.json.");

        return ToEncoding(tokenizer, name, patterns);
    }

#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
    /// <summary>
    /// Loads an <see cref="Encoding"/> from a stream containing tokenizer.json data asynchronously.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    /// <param name="name">Encoding name.</param>
    /// <param name="patterns">Optional regex patterns. If null, auto-detected from pre-tokenizer config.</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async System.Threading.Tasks.Task<Encoding> FromStreamAsync(
        Stream stream,
        string name = "tokenizer",
        IReadOnlyList<string>? patterns = null,
        System.Threading.CancellationToken cancellationToken = default)
    {
        stream = stream ?? throw new ArgumentNullException(nameof(stream));

        var tokenizer =
            await JsonSerializer.DeserializeAsync(stream, SourceGenerationContext.Default.TokenizerJson, cancellationToken).ConfigureAwait(false) ??
            throw new InvalidOperationException("Failed to deserialize tokenizer.json.");

        return ToEncoding(tokenizer, name, patterns);
    }

    /// <summary>
    /// Loads an <see cref="Encoding"/> from a URL (e.g., HuggingFace Hub) asynchronously.
    /// Example: <c>https://huggingface.co/{model}/raw/main/tokenizer.json</c>
    /// </summary>
    /// <param name="url">URL to download tokenizer.json from.</param>
    /// <param name="httpClient">HttpClient instance to use for downloading.</param>
    /// <param name="name">Encoding name.</param>
    /// <param name="patterns">Optional regex patterns. If null, auto-detected from pre-tokenizer config.</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async System.Threading.Tasks.Task<Encoding> FromUrlAsync(
        string url,
        System.Net.Http.HttpClient httpClient,
        string name = "tokenizer",
        IReadOnlyList<string>? patterns = null,
        System.Threading.CancellationToken cancellationToken = default)
    {
        url = url ?? throw new ArgumentNullException(nameof(url));
        httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

        using var response = await httpClient.GetAsync(new Uri(url), cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(
#if NET5_0_OR_GREATER
            cancellationToken
#endif
        ).ConfigureAwait(false);

        var tokenizer =
            await JsonSerializer.DeserializeAsync(stream, SourceGenerationContext.Default.TokenizerJson, cancellationToken).ConfigureAwait(false) ??
            throw new InvalidOperationException("Failed to deserialize tokenizer.json.");

        return ToEncoding(tokenizer, name, patterns);
    }
#endif

    /// <summary>
    /// Converts a parsed <see cref="TokenizerJson"/> to a Tiktoken <see cref="Encoding"/>.
    /// </summary>
    /// <param name="tokenizer">The parsed tokenizer.</param>
    /// <param name="name">Encoding name.</param>
    /// <param name="patterns">Optional regex patterns. If null, auto-detected from pre-tokenizer config.</param>
    /// <returns></returns>
    public static Encoding ToEncoding(TokenizerJson tokenizer, string name = "tokenizer", IReadOnlyList<string>? patterns = null)
    {
        tokenizer = tokenizer ?? throw new ArgumentNullException(nameof(tokenizer));

        if (tokenizer.Model?.Vocab is null)
        {
            throw new InvalidOperationException("Tokenizer model or vocab is not defined.");
        }

        var isByteLevel = HasByteLevel(tokenizer.PreTokenizer);

        // Convert vocab to mergeableRanks (byte[] -> token id)
        var mergeableRanks = new Dictionary<byte[], int>(new ByteArrayComparer());
        foreach (var pair in tokenizer.Model.Vocab)
        {
            var bytes = isByteLevel
                ? DecodeByteLevel(pair.Key)
                : System.Text.Encoding.UTF8.GetBytes(pair.Key);

            mergeableRanks[bytes] = pair.Value;
        }

        // Extract special tokens from added_tokens
        var specialTokens = new Dictionary<string, int>();
        foreach (var token in tokenizer.AddedTokens)
        {
            if (token.Special)
            {
                specialTokens[token.Content] = token.Id;
            }
        }

        // Use provided patterns, or auto-detect from pre-tokenizer config
        patterns ??= DetectPatterns(tokenizer.PreTokenizer);

        return new Encoding(
            name: name,
            patterns: patterns,
            mergeableRanks: mergeableRanks,
            specialTokens: specialTokens);
    }

    /// <summary>
    /// Checks whether the pre-tokenizer (or any nested pre-tokenizer) includes ByteLevel.
    /// </summary>
    private static bool HasByteLevel(TokenizerPreTokenizer? preTokenizer)
    {
        if (preTokenizer is null)
        {
            return false;
        }

        if (string.Equals(preTokenizer.Type, "ByteLevel", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.Equals(preTokenizer.Type, "Sequence", StringComparison.OrdinalIgnoreCase) &&
            preTokenizer.PreTokenizers is not null)
        {
            foreach (var nested in preTokenizer.PreTokenizers)
            {
                if (string.Equals(nested.Type, "ByteLevel", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Auto-detects regex patterns from the pre-tokenizer configuration.
    /// Supports ByteLevel (GPT-2), Split (with regex), and Sequence (Llama 3, Qwen2, etc.).
    /// </summary>
    private static IReadOnlyList<string> DetectPatterns(TokenizerPreTokenizer? preTokenizer)
    {
        if (preTokenizer is null)
        {
            return Gpt2Patterns;
        }

        // Direct Split with regex pattern
        if (string.Equals(preTokenizer.Type, "Split", StringComparison.OrdinalIgnoreCase))
        {
            var regex = preTokenizer.Pattern?.Regex;
            if (!string.IsNullOrEmpty(regex))
            {
                return [regex!];
            }
        }

        // Sequence: look for Split components with regex patterns (Llama 3, Qwen2, DeepSeek)
        if (string.Equals(preTokenizer.Type, "Sequence", StringComparison.OrdinalIgnoreCase) &&
            preTokenizer.PreTokenizers is not null)
        {
            var patterns = new List<string>();
            foreach (var nested in preTokenizer.PreTokenizers)
            {
                if (string.Equals(nested.Type, "Split", StringComparison.OrdinalIgnoreCase))
                {
                    var regex = nested.Pattern?.Regex;
                    if (!string.IsNullOrEmpty(regex))
                    {
                        patterns.Add(regex!);
                    }
                }
            }

            if (patterns.Count > 0)
            {
                return patterns;
            }
        }

        // Default to GPT-2 patterns for ByteLevel and other types
        return Gpt2Patterns;
    }

    /// <summary>
    /// Decodes a string from GPT-2's bytes_to_unicode encoding back to raw bytes.
    /// </summary>
    private static byte[] DecodeByteLevel(string text)
    {
        var bytes = new byte[text.Length];
        var count = 0;

        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];
            if (UnicodeToByte.TryGetValue(ch, out var b))
            {
                bytes[count++] = b;
            }
            else
            {
                // Characters outside the mapping are encoded as UTF-8.
                // This shouldn't happen in well-formed ByteLevel tokenizer vocabs.
                var utf8 = System.Text.Encoding.UTF8.GetBytes(new[] { ch });
                if (count + utf8.Length > bytes.Length)
                {
                    Array.Resize(ref bytes, bytes.Length + utf8.Length);
                }
                Array.Copy(utf8, 0, bytes, count, utf8.Length);
                count += utf8.Length;
            }
        }

        if (count != bytes.Length)
        {
            Array.Resize(ref bytes, count);
        }

        return bytes;
    }

    /// <summary>
    /// Builds the reverse of GPT-2's bytes_to_unicode() mapping.
    /// Maps unicode characters back to their original byte values.
    /// </summary>
    private static Dictionary<char, byte> BuildUnicodeToByte()
    {
        var result = new Dictionary<char, byte>(256);
        var n = 0;

        for (var b = 0; b < 256; b++)
        {
            if (IsPrintableByte(b))
            {
                // Printable bytes map to themselves: byte b -> char (char)b
                result[(char)b] = (byte)b;
            }
            else
            {
                // Non-printable bytes map to chars starting at U+0100
                result[(char)(256 + n)] = (byte)b;
                n++;
            }
        }

        return result;
    }

    /// <summary>
    /// Returns true if the byte is in GPT-2's "printable" range.
    /// These bytes map to themselves in the bytes_to_unicode mapping.
    /// Ranges: 33-126 (! to ~), 161-172 (¡ to ¬), 174-255 (® to ÿ)
    /// </summary>
    private static bool IsPrintableByte(int b)
    {
        return (b >= 33 && b <= 126) ||
               (b >= 161 && b <= 172) ||
               (b >= 174 && b <= 255);
    }
}
