namespace Tiktoken.Models;

/// <summary>
/// 
/// </summary>
public class EncodingSettingModel
{
    /// <summary>
    /// 
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// regex
    /// </summary>
    public string PatStr { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    public int? ExplicitNVocab { get; set; }

    /// <summary>
    /// tiktoken file
    /// </summary>
    public IReadOnlyDictionary<byte[], int> MergeableRanks { get; set; } = new Dictionary<byte[], int>();

    /// <summary>
    /// 
    /// </summary>
    public IReadOnlyDictionary<string, int> SpecialTokens { get; set; } = new Dictionary<string, int>();

    /// <summary>
    /// 
    /// </summary>
    public int MaxTokenValue => Math.Max(MergeableRanks.Values.Max(), SpecialTokens.Values.Max());

}