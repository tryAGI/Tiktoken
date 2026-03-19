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
    private readonly Encoder _tiktokenCl100K = new(new Cl100KBase());
    private readonly Encoder _tiktokenNoCache = new(new O200KBase()) { EnableCache = false };
    private readonly Tokenizer _microsoftMlTiktoken = TiktokenTokenizer.CreateForModel("gpt-4o");

    [Params(Strings.HelloWorld, Strings.Code, Strings.Multilingual, Strings.MultilingualLong, Strings.Bitcoin, Strings.CjkHeavy)]
    public string Data = string.Empty;

    private List<int> _sharpTokenEncoded = null!;
    private List<int> _tiktokenSharpEncoded = null!;
    private IReadOnlyCollection<int> _tiktokenEncoded = null!;
    private IReadOnlyList<int> _microsoftMlEncoded = null!;
    private int[] _tiktokenEncodedArray = null!;
    private int[] _tiktokenCl100KEncodedArray = null!;
    private byte[] _dataUtf8 = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _sharpTokenEncoded = _sharpToken.Encode(Data);
        _tiktokenSharpEncoded = _tiktokenSharp.Encode(Data);
        _tiktokenEncoded = _tiktoken.Encode(Data);
        _microsoftMlEncoded = _microsoftMlTiktoken.EncodeToIds(Data.AsSpan());
        _tiktokenEncodedArray = _tiktokenEncoded.ToArray();
        _tiktokenCl100KEncodedArray = _tiktokenCl100K.Encode(Data).ToArray();
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
    [BenchmarkCategory("CountTokens")]
    public int SharpTokenV2_0_4_() => _sharpToken.Encode(Data).Count;

    [Benchmark]
    [BenchmarkCategory("CountTokens")]
    public int TiktokenSharpV1_2_1_() => _tiktokenSharp.Encode(Data).Count;

    [Benchmark]
    [BenchmarkCategory("CountTokens")]
    public int MicrosoftMLTokenizerV3_0_0_() => _microsoftMlTiktoken.CountTokens(Data.AsSpan());

    [Benchmark]
    [BenchmarkCategory("CountTokens")]
    public int Tiktoken_() => _tiktoken.CountTokens(Data);


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
    [BenchmarkCategory("Explore")]
    public IReadOnlyCollection<string> Tiktoken_Explore() => _tiktoken.Explore(Data);

    [Benchmark]
    [BenchmarkCategory("Explore")]
    public IReadOnlyCollection<Tiktoken.UtfToken> Tiktoken_ExploreUtfSafe() => _tiktoken.ExploreUtfSafe(Data);


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


    [Benchmark(Baseline = true)]
    [BenchmarkCategory("CountTokensSpan")]
    public int Tiktoken_CountTokens_String() => _tiktoken.CountTokens(Data);

    [Benchmark]
    [BenchmarkCategory("CountTokensSpan")]
    public int Tiktoken_CountTokens_Span() => _tiktoken.CountTokens(Data.AsSpan());


    [Benchmark(Baseline = true)]
    [BenchmarkCategory("EncodeSpan")]
    public IReadOnlyCollection<int> Tiktoken_Encode_String() => _tiktoken.Encode(Data);

    [Benchmark]
    [BenchmarkCategory("EncodeSpan")]
    public IReadOnlyCollection<int> Tiktoken_Encode_Span() => _tiktoken.Encode(Data.AsSpan());


    [Benchmark(Baseline = true)]
    [BenchmarkCategory("CountTokensUtf8")]
    public int Tiktoken_CountTokens_FromString() => _tiktoken.CountTokens(Data);

    [Benchmark]
    [BenchmarkCategory("CountTokensUtf8")]
    public int Tiktoken_CountTokens_FromUtf8() => _tiktoken.CountTokens(_dataUtf8.AsSpan());


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


    [Benchmark(Baseline = true)]
    [BenchmarkCategory("CountTokensColdCache")]
    public int Tiktoken_CountTokens_Cached() => _tiktoken.CountTokens(Data);

    [Benchmark]
    [BenchmarkCategory("CountTokensColdCache")]
    public int Tiktoken_CountTokens_NoCache() => _tiktokenNoCache.CountTokens(Data);


    [Benchmark(Baseline = true)]
    [BenchmarkCategory("EncodeColdCache")]
    public IReadOnlyCollection<int> Tiktoken_Encode_Cached() => _tiktoken.Encode(Data);

    [Benchmark]
    [BenchmarkCategory("EncodeColdCache")]
    public IReadOnlyCollection<int> Tiktoken_Encode_NoCache() => _tiktokenNoCache.Encode(Data);


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
        return encoder.CountTokens(Data);
    }

    [Benchmark]
    [BenchmarkCategory("FirstCall")]
    public IReadOnlyCollection<int> Tiktoken_FirstCall_Encode()
    {
        var encoder = new Encoder(new O200KBase());
        return encoder.Encode(Data);
    }
}
