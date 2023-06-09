using System.Diagnostics;
using System.Text.RegularExpressions;
using Tiktoken.Models;
using Tiktoken.Services;

namespace Tiktoken;

/// <summary>
/// 
/// </summary>
public class Encoding
{
    /// <summary>
    /// get encoding with modelName
    /// </summary>
    /// <param name="modelName">gpt-3.5-turbo</param>
    /// <returns></returns>
    public static Encoding ForModel(string modelName)
    {
        var setting = EncodingManager.GetEncodingSettingsForModel(modelName);
        return new Encoding(setting);
    }

    /// <summary>
    /// get encoding with encoding name
    /// </summary>
    /// <param name="encodingName">cl100k_base</param>
    /// <returns></returns>
    public static Encoding Get(string encodingName)
    {
        var setting = EncodingManager.GetEncoding(encodingName);
        
        return new Encoding(setting);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="tokens"></param>
    /// <returns></returns>
    public static Regex SpecialTokenRegex(HashSet<string> tokens)
    {
        var inner = string.Join("|", tokens.Select(Regex.Escape));
        return new Regex($"({inner})");
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
            Debug.Assert(setting.MaxTokenValue == setting.ExplicitNVocab - 1);
        }


        _corePbe = new CoreBpe(setting.MergeableRanks, setting.SpecialTokens, setting.PatStr);
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
        return Encode(text).Count;
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

        if (disallowedSpecialSet.Count > 0)
        {
            var specialTokenRegex = SpecialTokenRegex(disallowedSpecialSet);
            var match = specialTokenRegex.Match(text);
            if (match.Success)
            {
                throw new InvalidOperationException(match.Value);
            }
        }

        return _corePbe.EncodeNative(text, allowedSpecialSet);
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