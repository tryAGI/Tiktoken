namespace Tiktoken.Encodings;

/// <summary>
/// 
/// </summary>
public class Encoding
{
    /// <summary>
    /// 
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public IReadOnlyList<string> Patterns { get; set; }

    /// <summary>
    /// Regex pattern
    /// </summary>
    public string Pattern => string.Join("|", Patterns);

    /// <summary>
    /// tiktoken file
    /// </summary>
    public IReadOnlyDictionary<byte[], int> MergeableRanks { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public IReadOnlyDictionary<string, int> SpecialTokens { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public bool CompiledRegex { get; set; } = true;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    /// <param name="patterns"></param>
    /// <param name="mergeableRanks"></param>
    /// <param name="specialTokens"></param>
    public Encoding(
        string name,
        IReadOnlyList<string> patterns,
        IReadOnlyDictionary<byte[], int> mergeableRanks,
        IReadOnlyDictionary<string, int>? specialTokens = null)
    {
        Name = name;
        Patterns = patterns;
        MergeableRanks = mergeableRanks;
        SpecialTokens = specialTokens ?? new Dictionary<string, int>();
    }
}