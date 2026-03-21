using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Tiktoken.Encodings;

namespace Tiktoken.Benchmarks;

[MemoryDiagnoser]
[MarkdownExporterAttribute.GitHub]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
[HideColumns("Error", "StdDev", "StdDev", "RatioSD")]
public class Benchmarks
{
    private IReadOnlyDictionary<byte[], int> _o200kDict = null!;
    private IReadOnlyDictionary<byte[], int> _cl100kDict = null!;

    [GlobalSetup]
    public void Setup()
    {
        _o200kDict = new O200KBase().MergeableRanks;
        _cl100kDict = new Cl100KBase().MergeableRanks;
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Construction")]
    public Encoder Tiktoken_Construction_o200k() => new(new O200KBase());

    [Benchmark]
    [BenchmarkCategory("Construction")]
    public Encoder Tiktoken_Construction_cl100k() => new(new Cl100KBase());


    [Benchmark(Baseline = true)]
    [BenchmarkCategory("FirstCall")]
    public int Tiktoken_FirstCall_CountTokens()
    {
        var encoder = new Encoder(new O200KBase());
        return encoder.CountTokens(Strings.Code);
    }

    [Benchmark]
    [BenchmarkCategory("FirstCall")]
    public IReadOnlyCollection<int> Tiktoken_FirstCall_Encode()
    {
        var encoder = new Encoder(new O200KBase());
        return encoder.Encode(Strings.Code);
    }


    [Benchmark(Baseline = true)]
    [BenchmarkCategory("WriteToBinary")]
    public MemoryStream Tiktoken_WriteToBinary_o200k()
    {
        var ms = new MemoryStream();
        EncodingLoader.WriteEncodingToBinaryStream(ms, _o200kDict);
        return ms;
    }

    [Benchmark]
    [BenchmarkCategory("WriteToBinary")]
    public MemoryStream Tiktoken_WriteToBinary_cl100k()
    {
        var ms = new MemoryStream();
        EncodingLoader.WriteEncodingToBinaryStream(ms, _cl100kDict);
        return ms;
    }
}
