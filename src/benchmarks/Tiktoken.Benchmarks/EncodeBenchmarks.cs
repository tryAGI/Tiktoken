using BenchmarkDotNet.Attributes;
using SharpToken;
using TiktokenSharp;

namespace Tiktoken.Benchmarks;

// ReSharper disable UnassignedField.Global
[MemoryDiagnoser]
[MarkdownExporterAttribute.GitHub]
public class EncodeBenchmarks
{
    private readonly GptEncoding _sharpToken = GptEncoding.GetEncoding("cl100k_base");
    private readonly TikToken _tiktokenSharp = TikToken.GetEncoding("cl100k_base");
    private readonly Encoding _tiktoken = Encoding.Get("cl100k_base");
    
    [Params(
        "Hello, World!",
        @"King Lear, one of Shakespeare's darkest and most savage plays, tells the story of the foolish and Job-like Lear, who divides his kingdom, as he does his affections, according to vanity and whim. Lear’s failure as a father engulfs himself and his world in turmoil and tragedy.")]
    public string Data = string.Empty;
    
    [Benchmark(Baseline = true)]
    public List<int> SharpTokenV1_0_28() => _sharpToken.Encode(Data);
    
    [Benchmark]
    public List<int> TiktokenSharpV1_0_5() => _tiktokenSharp.Encode(Data);
    
    [Benchmark]
    public IReadOnlyCollection<int> Tiktoken() => _tiktoken.Encode(Data);
}