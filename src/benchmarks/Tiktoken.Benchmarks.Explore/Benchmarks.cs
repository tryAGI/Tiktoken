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

    [Params(Strings.HelloWorld, Strings.Code, Strings.Multilingual, Strings.MultilingualLong, Strings.Bitcoin, Strings.CjkHeavy)]
    public string Data = string.Empty;

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Explore")]
    public IReadOnlyCollection<string> Tiktoken_Explore() => _tiktoken.Explore(Data);

    [Benchmark]
    [BenchmarkCategory("Explore")]
    public IReadOnlyCollection<Tiktoken.UtfToken> Tiktoken_ExploreUtfSafe() => _tiktoken.ExploreUtfSafe(Data);
}
