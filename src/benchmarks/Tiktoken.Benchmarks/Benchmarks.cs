using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Microsoft.DeepDev;
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
    private readonly GptEncoding _sharpToken = GptEncoding.GetEncoding("cl100k_base");
    private readonly TikToken _tiktokenSharp = TikToken.GetEncoding("cl100k_base");
    private readonly Encoder _tiktoken = new(new Cl100KBase());
    private readonly Encoder _tiktokenO200K = new(new O200KBase());
    private readonly Tokenizer _microsoftMlTiktoken = TiktokenTokenizer.CreateForModel("gpt-4");
    private ITokenizer? _tokenizerLib;

    [Params(Strings.HelloWorld, Strings.KingLear, Strings.Bitcoin)]
    public string Data = string.Empty;

    private List<int> _sharpTokenEncoded = null!;
    private List<int> _tiktokenSharpEncoded = null!;
    private IReadOnlyCollection<int> _tiktokenEncoded = null!;
    private IReadOnlyList<int> _microsoftMlEncoded = null!;
    private int[] _tokenizerLibEncoded = null!;
    private int[] _tiktokenEncodedArray = null!;
    private int[] _tiktokenO200KEncodedArray = null!;

    [GlobalSetup]
    public async Task GlobalSetup()
    {
        _tokenizerLib = await TokenizerBuilder.CreateByModelNameAsync("gpt-4");
        _sharpTokenEncoded = _sharpToken.Encode(Data);
        _tiktokenSharpEncoded = _tiktokenSharp.Encode(Data);
        _tiktokenEncoded = _tiktoken.Encode(Data);
        _microsoftMlEncoded = _microsoftMlTiktoken.EncodeToIds(Data.AsSpan());
        _tokenizerLibEncoded = _tokenizerLib!.Encode(Data, ArraySegment<string>.Empty).ToArray();
        _tiktokenEncodedArray = _tiktokenEncoded.ToArray();
        _tiktokenO200KEncodedArray = _tiktokenO200K.Encode(Data).ToArray();
    }
    
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Encode")]
    public List<int> SharpTokenV2_0_3_Encode() => _sharpToken.Encode(Data);
    
    [Benchmark]
    [BenchmarkCategory("Encode")]
    public List<int> TiktokenSharpV1_1_5_Encode() => _tiktokenSharp.Encode(Data);
    
    [Benchmark]
    [BenchmarkCategory("Encode")]
    public IReadOnlyCollection<int> MicrosoftMLTokenizerV1_0_0_Encode() => _microsoftMlTiktoken.EncodeToIds(Data.AsSpan());
    
    [Benchmark]
    [BenchmarkCategory("Encode")]
    public IReadOnlyCollection<int> TokenizerLibV1_3_3_Encode() => _tokenizerLib!.Encode(Data, ArraySegment<string>.Empty);
    
    [Benchmark]
    [BenchmarkCategory("Encode")]
    public IReadOnlyCollection<int> Tiktoken_Encode() => _tiktoken.Encode(Data);
    
    
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("CountTokens")]
    public int SharpTokenV2_0_3_() => _sharpToken.Encode(Data).Count;
    
    [Benchmark]
    [BenchmarkCategory("CountTokens")]
    public int TiktokenSharpV1_1_5_() => _tiktokenSharp.Encode(Data).Count;
    
    [Benchmark]
    [BenchmarkCategory("CountTokens")]
    public int MicrosoftMLTokenizerV1_0_0_() => _microsoftMlTiktoken.CountTokens(Data.AsSpan());
    
    [Benchmark]
    [BenchmarkCategory("CountTokens")]
    public int TokenizerLibV1_3_3_() => _tokenizerLib!.Encode(Data, ArraySegment<string>.Empty).Count;
    
    [Benchmark]
    [BenchmarkCategory("CountTokens")]
    public int Tiktoken_() => _tiktoken.CountTokens(Data);


    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Decode")]
    public string SharpTokenV2_0_3_Decode() => _sharpToken.Decode(_sharpTokenEncoded);

    [Benchmark]
    [BenchmarkCategory("Decode")]
    public string TiktokenSharpV1_1_5_Decode() => _tiktokenSharp.Decode(_tiktokenSharpEncoded);

    [Benchmark]
    [BenchmarkCategory("Decode")]
    public string? MicrosoftMLTokenizerV1_0_0_Decode() => _microsoftMlTiktoken.Decode(_microsoftMlEncoded);

    [Benchmark]
    [BenchmarkCategory("Decode")]
    public string TokenizerLibV1_3_3_Decode() => _tokenizerLib!.Decode(_tokenizerLibEncoded);

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
    [BenchmarkCategory("Encode_o200k")]
    public IReadOnlyCollection<int> Tiktoken_cl100k_Encode() => _tiktoken.Encode(Data);

    [Benchmark]
    [BenchmarkCategory("Encode_o200k")]
    public IReadOnlyCollection<int> Tiktoken_o200k_Encode() => _tiktokenO200K.Encode(Data);


    [Benchmark(Baseline = true)]
    [BenchmarkCategory("CountTokens_o200k")]
    public int Tiktoken_cl100k_CountTokens() => _tiktoken.CountTokens(Data);

    [Benchmark]
    [BenchmarkCategory("CountTokens_o200k")]
    public int Tiktoken_o200k_CountTokens() => _tiktokenO200K.CountTokens(Data);


    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Decode_o200k")]
    public string Tiktoken_cl100k_Decode() => _tiktoken.Decode(_tiktokenEncodedArray);

    [Benchmark]
    [BenchmarkCategory("Decode_o200k")]
    public string Tiktoken_o200k_Decode() => _tiktokenO200K.Decode(_tiktokenO200KEncodedArray);


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
}