using System.Diagnostics;
using Tiktoken.Models;
using Tiktoken.Services;

namespace Tiktoken;

/// <summary>
/// 
/// </summary>
public class Encoding
{
    /// <summary>
    /// Returns encoding by model name.
    /// </summary>
    /// <param name="modelName">gpt-3.5-turbo</param>
    /// <returns></returns>
    public static Encoding ForModel(string modelName)
    {
        return Get(Helpers.GetNameByModel(modelName));
    }

    /// <summary>
    /// Returns encoding by name.
    /// </summary>
    /// <param name="encodingName">cl100k_base</param>
    /// <returns></returns>
    public static Encoding Get(string encodingName)
    {
        if (string.IsNullOrEmpty(encodingName))
        {
            throw new ArgumentException("encodingName is null or empty", nameof(encodingName));
        }

        var setting = EncodingManager.Get(encodingName);
        
        return new Encoding(setting);
    }

    private readonly CoreBpe _corePbe;
    private readonly EncodingSettingModel _setting;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="setting"></param>
    public Encoding(EncodingSettingModel setting)
    {
        setting = setting ?? throw new ArgumentNullException(nameof(setting));
        
        if (setting.ExplicitNVocab != null)
        {
            Debug.Assert(setting.SpecialTokens.Count + setting.MergeableRanks.Count == setting.ExplicitNVocab);
            Debug.Assert(Math.Max(setting.MergeableRanks.Values.Max(), setting.SpecialTokens.Values.Max()) == setting.ExplicitNVocab - 1);
        }

        _corePbe = new CoreBpe(setting.MergeableRanks, setting.SpecialTokens, setting.Pattern);
        _setting = setting;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public HashSet<string> SpecialTokensSet()
    {
        return new HashSet<string>(_setting.SpecialTokens.Keys);
    }

    /// <summary>
    /// TODO: optimize
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    public int CountTokens(string text)
    {
        return Encode(text, allowedSpecial: "all").Count;
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="text"></param>
    /// <param name="allowedSpecial"></param>
    /// <param name="disallowedSpecial"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public IReadOnlyCollection<int> Encode(
        string text,
        object? allowedSpecial = null,
        object? disallowedSpecial = null)
    {
        allowedSpecial ??= new HashSet<string>();
        disallowedSpecial ??= "all";

        var allowedSpecialSet = allowedSpecial.Equals("all")
            ? SpecialTokensSet()
            : new HashSet<string>((IEnumerable<string>)allowedSpecial);
        var disallowedSpecialSet = disallowedSpecial.Equals("all")
            ? new HashSet<string>(SpecialTokensSet().Except(allowedSpecialSet))
            : new HashSet<string>((IEnumerable<string>)disallowedSpecial);

        return _corePbe.EncodeNative(text, allowedSpecialSet, disallowedSpecialSet);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="tokens"></param>
    /// <returns></returns>
    public string Decode(IReadOnlyCollection<int> tokens)
    {
        var bytes = _corePbe.DecodeNative(tokens);
        
        return System.Text.Encoding.UTF8.GetString(bytes);
    }
}