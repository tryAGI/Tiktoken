namespace Tiktoken.Services;

// ReSharper disable InconsistentNaming

internal static class Helpers
{
    private static Dictionary<string, string> ModelToEncoding { get; } = new()
    {
        // chat
        { "gpt-4", Encodings.Cl100KBase },
        { "gpt-3.5-turbo", Encodings.Cl100KBase },
        { "gpt-35-turbo", Encodings.Cl100KBase }, // Azure deployment name
        // text
        { "text-davinci-003", Encodings.P50KBase },
        { "text-davinci-002", Encodings.P50KBase },
        { "text-davinci-001", Encodings.R50KBase },
        { "text-curie-001", Encodings.R50KBase },
        { "text-babbage-001", Encodings.R50KBase },
        { "text-ada-001", Encodings.R50KBase },
        { "davinci", Encodings.R50KBase },
        { "curie", Encodings.R50KBase },
        { "babbage", Encodings.R50KBase },
        { "ada", Encodings.R50KBase },
        // code
        { "code-davinci-002", Encodings.P50KBase },
        { "code-davinci-001", Encodings.P50KBase },
        { "code-cushman-002", Encodings.P50KBase },
        { "code-cushman-001", Encodings.P50KBase },
        { "davinci-codex", Encodings.P50KBase },
        { "cushman-codex", Encodings.P50KBase },
        // edit
        { "text-davinci-edit-001", Encodings.P50KEdit },
        { "code-davinci-edit-001", Encodings.P50KEdit },
        // embeddings
        { "text-embedding-ada-002", Encodings.Cl100KBase },
        // old embeddings
        { "text-similarity-davinci-001", Encodings.R50KBase },
        { "text-similarity-curie-001", Encodings.R50KBase },
        { "text-similarity-babbage-001", Encodings.R50KBase },
        { "text-similarity-ada-001", Encodings.R50KBase },
        { "text-search-davinci-doc-001", Encodings.R50KBase },
        { "text-search-curie-doc-001", Encodings.R50KBase },
        { "text-search-babbage-doc-001", Encodings.R50KBase },
        { "text-search-ada-doc-001", Encodings.R50KBase },
        { "code-search-babbage-code-001", Encodings.R50KBase },
        { "code-search-ada-code-001", Encodings.R50KBase },
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