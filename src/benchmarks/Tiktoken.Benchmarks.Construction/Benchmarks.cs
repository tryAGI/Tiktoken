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

    // Pre-warmed encoder for ASCII lookup benchmarks (cache disabled to isolate lookup path)
    private Encoder _noCacheEncoder = null!;

    [GlobalSetup]
    public void Setup()
    {
        _o200kDict = new O200KBase().MergeableRanks;
        _cl100kDict = new Cl100KBase().MergeableRanks;
        _noCacheEncoder = new Encoder(new O200KBase()) { EnableCache = false };
        // Warm up to ensure no first-call effects
        _noCacheEncoder.CountTokens("warmup");
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


    // ASCII fast-path: short ASCII string hits TokenEncoder.TryGetValueAscii directly
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("AsciiLookup")]
    public int AsciiLookup_CountTokens() => _noCacheEncoder.CountTokens("Hello, World!");

    // Non-ASCII comparison: forces UTF-8 byte path (no ASCII fast-path)
    [Benchmark]
    [BenchmarkCategory("AsciiLookup")]
    public int NonAsciiLookup_CountTokens() => _noCacheEncoder.CountTokens("Привет мир!");

    // ASCII encode: measures per-token ASCII lookup via Encode
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("AsciiEncode")]
    public IReadOnlyCollection<int> AsciiLookup_Encode() => _noCacheEncoder.Encode("Hello, World!");

    // Non-ASCII encode comparison
    [Benchmark]
    [BenchmarkCategory("AsciiEncode")]
    public IReadOnlyCollection<int> NonAsciiLookup_Encode() => _noCacheEncoder.Encode("Привет мир!");


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
