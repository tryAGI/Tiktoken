namespace Tiktoken.Models;

/// <summary>
/// 
/// </summary>
public class EncodingSettingModel
{
    /// <summary>
    /// 
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// regex
    /// </summary>
    public required string Pattern { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public int? ExplicitNVocab { get; set; }

    /// <summary>
    /// tiktoken file
    /// </summary>
    public required IReadOnlyDictionary<byte[], int> MergeableRanks { get; set; } = new Dictionary<byte[], int>();

    /// <summary>
    /// 
    /// </summary>
    public IReadOnlyDictionary<string, int> SpecialTokens { get; set; } = new Dictionary<string, int>();

}