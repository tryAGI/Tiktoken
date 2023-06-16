namespace Tiktoken.Services;

// ReSharper disable InconsistentNaming

internal static class Helpers
{
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
    /// Returns encoding name by model name or throws exception.
    /// </summary>
    /// <param name="modelName">gpt-4 gpt-3.5-turbo ...</param>
    /// <exception cref="ArgumentException"></exception>
    /// <returns></returns>
    public static string GetNameByModel(string modelName)
    {
        return ModelToEncoding
            .FirstOrDefault(a => modelName.StartsWith(a.Key, StringComparison.Ordinal)).Value ??
            throw new ArgumentException($"Model name {modelName} is not supported.");
    }
}