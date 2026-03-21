using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Tiktoken.Encodings;

namespace Tiktoken.Benchmarks;

// ReSharper disable UnassignedField.Global
[MemoryDiagnoser]
[MarkdownExporterAttribute.GitHub]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
[HideColumns("Error", "StdDev", "StdDev", "RatioSD")]
public class Benchmarks
{
    private readonly Encoder _tiktoken = new(new O200KBase());
    private readonly Encoder _tiktokenCl100K = new(new Cl100KBase());

    [Params(Strings.HelloWorld, Strings.Code, Strings.Multilingual, Strings.MultilingualLong, Strings.Bitcoin, Strings.CjkHeavy)]
    public string Data = string.Empty;

    private int[] _tiktokenEncodedArray = null!;
    private int[] _tiktokenCl100KEncodedArray = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _tiktokenEncodedArray = _tiktoken.Encode(Data).ToArray();
        _tiktokenCl100KEncodedArray = _tiktokenCl100K.Encode(Data).ToArray();
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Encode_cl100k")]
    public IReadOnlyCollection<int> Tiktoken_o200k_Encode() => _tiktoken.Encode(Data);

    [Benchmark]
    [BenchmarkCategory("Encode_cl100k")]
    public IReadOnlyCollection<int> Tiktoken_cl100k_Encode() => _tiktokenCl100K.Encode(Data);


    [Benchmark(Baseline = true)]
    [BenchmarkCategory("CountTokens_cl100k")]
    public int Tiktoken_o200k_CountTokens() => _tiktoken.CountTokens(Data);

    [Benchmark]
    [BenchmarkCategory("CountTokens_cl100k")]
    public int Tiktoken_cl100k_CountTokens() => _tiktokenCl100K.CountTokens(Data);


    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Decode_cl100k")]
    public string Tiktoken_o200k_Decode() => _tiktoken.Decode(_tiktokenEncodedArray);

    [Benchmark]
    [BenchmarkCategory("Decode_cl100k")]
    public string Tiktoken_cl100k_Decode() => _tiktokenCl100K.Decode(_tiktokenCl100KEncodedArray);
}
