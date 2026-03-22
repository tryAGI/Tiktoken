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
    private readonly Encoder _cached = new(new O200KBase());
    private readonly Encoder _noCache = new(new O200KBase()) { EnableCache = false };

    [Params(Strings.HelloWorld, Strings.Code, Strings.Multilingual, Strings.MultilingualLong, Strings.Bitcoin, Strings.CjkHeavy)]
    public string Data = string.Empty;

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("CountTokens")]
    public int CountTokens_Cached() => _cached.CountTokens(Data);

    [Benchmark]
    [BenchmarkCategory("CountTokens")]
    public int CountTokens_NoCache() => _noCache.CountTokens(Data);


    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Encode")]
    public IReadOnlyCollection<int> Encode_Cached() => _cached.Encode(Data);

    [Benchmark]
    [BenchmarkCategory("Encode")]
    public IReadOnlyCollection<int> Encode_NoCache() => _noCache.Encode(Data);
}
