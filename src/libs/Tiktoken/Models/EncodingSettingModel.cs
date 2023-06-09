namespace Tiktoken.Models;

/// <summary>
/// 
/// </summary>
public class EncodingSettingModel
{
    public string Name { get; set; }

    /// <summary>
    /// regex
    /// </summary>
    public string PatStr { get; set; }


    public int? ExplicitNVocab { get; set; }

    /// <summary>
    /// tiktoken file
    /// </summary>
    public Dictionary<byte[], int> MergeableRanks { get; set; }

    public Dictionary<string, int> SpecialTokens { get; set; }


    public int MaxTokenValue { 
        get {
            return Math.Max(MergeableRanks.Values.Max(), SpecialTokens.Values.Max());
        } 
    }

    public EncodingSettingModel() { }

}