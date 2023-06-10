using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using SharpToken;
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
    private readonly GptEncoding _sharpToken = GptEncoding.GetEncoding("cl100k_base");
    private readonly TikToken _tiktokenSharp = TikToken.GetEncoding("cl100k_base");
    private readonly Encoding _tiktoken = Encoding.Get("cl100k_base");
    
    [Params(Strings.HelloWorld, Strings.KingLear, Strings.Bitcoin)]
    public string Data = string.Empty;
    
    
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Encode")]
    public List<int> SharpTokenV1_0_28_Encode() => _sharpToken.Encode(Data);
    
    [Benchmark]
    [BenchmarkCategory("Encode")]
    public List<int> TiktokenSharpV1_0_5_Encode() => _tiktokenSharp.Encode(Data);
    
    [Benchmark]
    [BenchmarkCategory("Encode")]
    public IReadOnlyCollection<int> Tiktoken_Encode() => _tiktoken.Encode(Data);
    
    
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("CountTokens")]
    public int SharpTokenV1_0_28_() => _sharpToken.Encode(Data).Count;
    
    [Benchmark]
    [BenchmarkCategory("CountTokens")]
    public int TiktokenSharpV1_0_5_() => _tiktokenSharp.Encode(Data).Count;
    
    [Benchmark]
    [BenchmarkCategory("CountTokens")]
    public int Tiktoken_() => _tiktoken.CountTokens(Data);
}