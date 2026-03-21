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
    private readonly Tokenizer _microsoftMlTiktoken = TiktokenTokenizer.CreateForModel("gpt-4o");

    [Params(Strings.HelloWorld, Strings.Code, Strings.Multilingual, Strings.MultilingualLong, Strings.Bitcoin, Strings.CjkHeavy)]
    public string Data = string.Empty;

    private List<int> _sharpTokenEncoded = null!;
    private List<int> _tiktokenSharpEncoded = null!;
    private IReadOnlyCollection<int> _tiktokenEncoded = null!;
    private IReadOnlyList<int> _microsoftMlEncoded = null!;
    private int[] _tiktokenEncodedArray = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _sharpTokenEncoded = _sharpToken.Encode(Data);
        _tiktokenSharpEncoded = _tiktokenSharp.Encode(Data);
        _tiktokenEncoded = _tiktoken.Encode(Data);
        _microsoftMlEncoded = _microsoftMlTiktoken.EncodeToIds(Data.AsSpan());
        _tiktokenEncodedArray = _tiktokenEncoded.ToArray();
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Decode")]
    public string SharpTokenV2_0_4_Decode() => _sharpToken.Decode(_sharpTokenEncoded);

    [Benchmark]
    [BenchmarkCategory("Decode")]
    public string TiktokenSharpV1_2_1_Decode() => _tiktokenSharp.Decode(_tiktokenSharpEncoded);

    [Benchmark]
    [BenchmarkCategory("Decode")]
    public string? MicrosoftMLTokenizerV3_0_0_Decode() => _microsoftMlTiktoken.Decode(_microsoftMlEncoded);

    [Benchmark]
    [BenchmarkCategory("Decode")]
    public string Tiktoken_Decode() => _tiktoken.Decode(_tiktokenEncoded);


    [Benchmark(Baseline = true)]
    [BenchmarkCategory("DecodeToUtf8")]
    public string Tiktoken_Decode_Baseline() => _tiktoken.Decode(_tiktokenEncoded);

    [Benchmark]
    [BenchmarkCategory("DecodeToUtf8")]
    public int Tiktoken_DecodeToUtf8()
    {
        var byteCount = _tiktoken.GetDecodedUtf8ByteCount(_tiktokenEncodedArray);
        if (byteCount <= 1024)
        {
            Span<byte> buffer = stackalloc byte[byteCount];
            return _tiktoken.DecodeToUtf8(_tiktokenEncodedArray, buffer);
        }

        var rented = System.Buffers.ArrayPool<byte>.Shared.Rent(byteCount);
        try
        {
            return _tiktoken.DecodeToUtf8(_tiktokenEncodedArray, rented.AsSpan(0, byteCount));
        }
        finally
        {
            System.Buffers.ArrayPool<byte>.Shared.Return(rented);
        }
    }
}
