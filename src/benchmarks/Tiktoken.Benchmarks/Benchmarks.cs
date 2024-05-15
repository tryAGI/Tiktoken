using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Microsoft.DeepDev;
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
    private readonly GptEncoding _sharpToken = GptEncoding.GetEncoding("cl100k_base");
    private readonly TikToken _tiktokenSharp = TikToken.GetEncoding("cl100k_base");
    private readonly Encoder _tiktoken = new(new Cl100KBase());
    private ITokenizer? _tokenizerLib;
    
    [Params(Strings.HelloWorld, Strings.KingLear, Strings.Bitcoin)]
    public string Data = string.Empty;

    [GlobalSetup]
    public async Task GlobalSetup()
    {
        _tokenizerLib = await TokenizerBuilder.CreateByModelNameAsync("gpt-4");
    }
    
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Encode")]
    public List<int> SharpTokenV2_0_1_Encode() => _sharpToken.Encode(Data);
    
    [Benchmark]
    [BenchmarkCategory("Encode")]
    public List<int> TiktokenSharpV1_0_9_Encode() => _tiktokenSharp.Encode(Data);
    
    [Benchmark]
    [BenchmarkCategory("Encode")]
    public IReadOnlyCollection<int> TokenizerLibV1_3_3_Encode() => _tokenizerLib!.Encode(Data, ArraySegment<string>.Empty);
    
    [Benchmark]
    [BenchmarkCategory("Encode")]
    public IReadOnlyCollection<int> Tiktoken_Encode() => _tiktoken.Encode(Data);
    
    
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("CountTokens")]
    public int SharpTokenV2_0_1_() => _sharpToken.Encode(Data).Count;
    
    [Benchmark]
    [BenchmarkCategory("CountTokens")]
    public int TiktokenSharpV1_0_9_() => _tiktokenSharp.Encode(Data).Count;
    
    [Benchmark]
    [BenchmarkCategory("CountTokens")]
    public int TokenizerLibV1_3_3_() => _tokenizerLib!.Encode(Data, ArraySegment<string>.Empty).Count;
    
    [Benchmark]
    [BenchmarkCategory("CountTokens")]
    public int Tiktoken_() => _tiktoken.CountTokens(Data);
}