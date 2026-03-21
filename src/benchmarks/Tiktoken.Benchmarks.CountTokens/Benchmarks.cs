using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Microsoft.ML.Tokenizers;
using SharpToken;
using Tiktoken.Encodings;
using TiktokenSharp;

namespace Tiktoken.Benchmarks;

// ReSharper disable UnassignedField.Global
[MemoryDiagnoser]
[MarkdownExporterAttribute.GitHub]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
[HideColumns("Error", "StdDev", "StdDev", "RatioSD")]
public class Benchmarks
{
    private readonly GptEncoding _sharpToken = GptEncoding.GetEncoding("o200k_base");
    private readonly TikToken _tiktokenSharp = TikToken.GetEncoding("o200k_base");
    private readonly Encoder _tiktoken = new(new O200KBase());
    private readonly Encoder _tiktokenNoCache = new(new O200KBase()) { EnableCache = false };
    private readonly Tokenizer _microsoftMlTiktoken = TiktokenTokenizer.CreateForModel("gpt-4o");

    [Params(Strings.HelloWorld, Strings.Code, Strings.Multilingual, Strings.MultilingualLong, Strings.Bitcoin, Strings.CjkHeavy)]
    public string Data = string.Empty;

    private byte[] _dataUtf8 = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _dataUtf8 = System.Text.Encoding.UTF8.GetBytes(Data);
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("CountTokens")]
    public int SharpTokenV2_0_4_() => _sharpToken.Encode(Data).Count;

    [Benchmark]
    [BenchmarkCategory("CountTokens")]
    public int TiktokenSharpV1_2_1_() => _tiktokenSharp.Encode(Data).Count;

    [Benchmark]
    [BenchmarkCategory("CountTokens")]
    public int MicrosoftMLTokenizerV3_0_0_() => _microsoftMlTiktoken.CountTokens(Data.AsSpan());

    [Benchmark]
    [BenchmarkCategory("CountTokens")]
    public int Tiktoken_() => _tiktoken.CountTokens(Data);


    [Benchmark(Baseline = true)]
    [BenchmarkCategory("CountTokensColdCache")]
    public int Tiktoken_CountTokens_Cached() => _tiktoken.CountTokens(Data);

    [Benchmark]
    [BenchmarkCategory("CountTokensColdCache")]
    public int Tiktoken_CountTokens_NoCache() => _tiktokenNoCache.CountTokens(Data);


    [Benchmark(Baseline = true)]
    [BenchmarkCategory("CountTokensSpan")]
    public int Tiktoken_CountTokens_String() => _tiktoken.CountTokens(Data);

    [Benchmark]
    [BenchmarkCategory("CountTokensSpan")]
    public int Tiktoken_CountTokens_Span() => _tiktoken.CountTokens(Data.AsSpan());


    [Benchmark(Baseline = true)]
    [BenchmarkCategory("CountTokensUtf8")]
    public int Tiktoken_CountTokens_FromString() => _tiktoken.CountTokens(Data);

    [Benchmark]
    [BenchmarkCategory("CountTokensUtf8")]
    public int Tiktoken_CountTokens_FromUtf8() => _tiktoken.CountTokens(_dataUtf8.AsSpan());
}
