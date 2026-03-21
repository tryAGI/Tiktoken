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
    [BenchmarkCategory("Encode")]
    public List<int> SharpTokenV2_0_4_Encode() => _sharpToken.Encode(Data);

    [Benchmark]
    [BenchmarkCategory("Encode")]
    public List<int> TiktokenSharpV1_2_1_Encode() => _tiktokenSharp.Encode(Data);

    [Benchmark]
    [BenchmarkCategory("Encode")]
    public IReadOnlyCollection<int> MicrosoftMLTokenizerV3_0_0_Encode() => _microsoftMlTiktoken.EncodeToIds(Data.AsSpan());

    [Benchmark]
    [BenchmarkCategory("Encode")]
    public IReadOnlyCollection<int> Tiktoken_Encode() => _tiktoken.Encode(Data);


    [Benchmark(Baseline = true)]
    [BenchmarkCategory("EncodeColdCache")]
    public IReadOnlyCollection<int> Tiktoken_Encode_Cached() => _tiktoken.Encode(Data);

    [Benchmark]
    [BenchmarkCategory("EncodeColdCache")]
    public IReadOnlyCollection<int> Tiktoken_Encode_NoCache() => _tiktokenNoCache.Encode(Data);


    [Benchmark(Baseline = true)]
    [BenchmarkCategory("EncodeSpan")]
    public IReadOnlyCollection<int> Tiktoken_Encode_String() => _tiktoken.Encode(Data);

    [Benchmark]
    [BenchmarkCategory("EncodeSpan")]
    public IReadOnlyCollection<int> Tiktoken_Encode_Span() => _tiktoken.Encode(Data.AsSpan());


    [Benchmark(Baseline = true)]
    [BenchmarkCategory("EncodeUtf8")]
    public IReadOnlyCollection<int> Tiktoken_Encode_Baseline() => _tiktoken.Encode(Data);

    [Benchmark]
    [BenchmarkCategory("EncodeUtf8")]
    public int Tiktoken_EncodeUtf8()
    {
        var tokenCount = _tiktoken.CountTokens(_dataUtf8.AsSpan());
        if (tokenCount <= 1024)
        {
            Span<int> buffer = stackalloc int[tokenCount];
            return _tiktoken.EncodeUtf8(_dataUtf8.AsSpan(), buffer);
        }

        var rented = System.Buffers.ArrayPool<int>.Shared.Rent(tokenCount);
        try
        {
            return _tiktoken.EncodeUtf8(_dataUtf8.AsSpan(), rented.AsSpan(0, tokenCount));
        }
        finally
        {
            System.Buffers.ArrayPool<int>.Shared.Return(rented);
        }
    }
}
