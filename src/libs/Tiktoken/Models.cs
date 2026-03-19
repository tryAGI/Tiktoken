namespace Tiktoken;

/// <summary>
/// Well-known model name constants for use with <see cref="TikTokenEncoder"/> and <see cref="ModelToEncoder"/>.
/// </summary>
public static class Models
{
    // o-series reasoning models (o200k_base)

    /// <summary>o4-mini (uses o200k_base encoding).</summary>
    public const string O4Mini = "o4-mini";

    /// <summary>o3 (uses o200k_base encoding).</summary>
    public const string O3 = "o3";

    /// <summary>o3-mini (uses o200k_base encoding).</summary>
    public const string O3Mini = "o3-mini";

    /// <summary>o3-pro (uses o200k_base encoding).</summary>
    public const string O3Pro = "o3-pro";

    /// <summary>o1 (uses o200k_base encoding).</summary>
    public const string O1 = "o1";

    /// <summary>o1-mini (uses o200k_base encoding).</summary>
    public const string O1Mini = "o1-mini";

    // GPT-4o / GPT-4.x family (o200k_base)

    /// <summary>GPT-4o (uses o200k_base encoding).</summary>
    public const string Gpt4o = "gpt-4o";

    /// <summary>GPT-4o mini (uses o200k_base encoding).</summary>
    public const string Gpt4oMini = "gpt-4o-mini";

    /// <summary>GPT-4.5 preview (uses o200k_base encoding).</summary>
    public const string Gpt45Preview = "gpt-4.5-preview";

    /// <summary>GPT-4.1 (uses o200k_base encoding).</summary>
    public const string Gpt41 = "gpt-4.1";

    /// <summary>GPT-4.1 mini (uses o200k_base encoding).</summary>
    public const string Gpt41Mini = "gpt-4.1-mini";

    /// <summary>GPT-4.1 nano (uses o200k_base encoding).</summary>
    public const string Gpt41Nano = "gpt-4.1-nano";

    /// <summary>ChatGPT-4o latest (uses o200k_base encoding).</summary>
    public const string Chatgpt4oLatest = "chatgpt-4o-latest";

    // GPT-4 family (cl100k_base)

    /// <summary>GPT-4 Turbo (uses cl100k_base encoding).</summary>
    public const string Gpt4Turbo = "gpt-4-turbo";

    /// <summary>GPT-4 (uses cl100k_base encoding).</summary>
    public const string Gpt4 = "gpt-4";

    // GPT-3.5 family (cl100k_base)

    /// <summary>GPT-3.5 Turbo (uses cl100k_base encoding).</summary>
    public const string Gpt35Turbo = "gpt-3.5-turbo";

    /// <summary>GPT-3.5 Turbo Azure deployment name (uses cl100k_base encoding).</summary>
    public const string Gpt35TurboAzure = "gpt-35-turbo";

    // Embeddings (cl100k_base)

    /// <summary>Text Embedding Ada 002 (uses cl100k_base encoding).</summary>
    public const string TextEmbeddingAda002 = "text-embedding-ada-002";

    /// <summary>Text Embedding 3 Small (uses cl100k_base encoding).</summary>
    public const string TextEmbedding3Small = "text-embedding-3-small";

    /// <summary>Text Embedding 3 Large (uses cl100k_base encoding).</summary>
    public const string TextEmbedding3Large = "text-embedding-3-large";
}
