# Tiktoken

[![Nuget package](https://img.shields.io/nuget/vpre/Tiktoken)](https://www.nuget.org/packages/Tiktoken/)
[![dotnet](https://github.com/tryAGI/Tiktoken/actions/workflows/dotnet.yml/badge.svg?branch=main)](https://github.com/tryAGI/Tiktoken/actions/workflows/dotnet.yml)
[![License: MIT](https://img.shields.io/github/license/tryAGI/Tiktoken)](https://github.com/tryAGI/Tiktoken/blob/main/LICENSE.txt)
[![Discord](https://img.shields.io/discord/1115206893015662663?label=Discord&logo=discord&logoColor=white&color=d82679)](https://discord.gg/Ca2xhfBf3v)

This implementation aims for maximum performance, especially in the token count operation.  
There's also a benchmark console app here for easy tracking of this.  
We will be happy to accept any PR.  

### Implemented encodings
- `o200k_base`
- `cl100k_base`
- `r50k_base`
- `p50k_base`
- `p50k_edit`

### Usage
```csharp
using Tiktoken;

var encoder = TikTokenEncoder.CreateForModel(Models.Gpt4o);
var tokens = encoder.Encode("hello world"); // [15339, 1917]
var text = encoder.Decode(tokens); // hello world
var numberOfTokens = encoder.CountTokens(text); // 2
var stringTokens = encoder.Explore(text); // ["hello", " world"]

// Alternative APIs:
var encoder = ModelToEncoder.For("gpt-4o");
var encoder = new Encoder(new O200KBase());
```

### Load from HuggingFace tokenizer.json

The `Tiktoken.Encodings.Tokenizer` package enables loading any HuggingFace-format `tokenizer.json` file — supporting GPT-2, Llama 3, Qwen2, DeepSeek, and other BPE-based models.

```csharp
using Tiktoken;
using Tiktoken.Encodings;

// From a local file
var encoding = TokenizerJsonLoader.FromFile("path/to/tokenizer.json");
var encoder = new Encoder(encoding);

// From a stream (HTTP responses, embedded resources)
using var stream = File.OpenRead("tokenizer.json");
var encoding = TokenizerJsonLoader.FromStream(stream);

// From a URL (e.g., HuggingFace Hub)
using var httpClient = new HttpClient();
var encoding = await TokenizerJsonLoader.FromUrlAsync(
    new Uri("https://huggingface.co/openai-community/gpt2/raw/main/tokenizer.json"),
    httpClient,
    name: "gpt2");

// Custom regex patterns (optional — auto-detected by default)
var encoding = TokenizerJsonLoader.FromFile("tokenizer.json", patterns: myPatterns);
```

**Supported pre-tokenizer types:**
- `ByteLevel` — GPT-2 and similar models
- `Split` with regex pattern — direct regex-based splitting
- `Sequence[Split, ByteLevel]` — Llama 3, Qwen2, DeepSeek, and other modern models

### Count message tokens (OpenAI chat format)

Count tokens for chat messages using OpenAI's official formula, including support for function/tool definitions:

```csharp
using Tiktoken;

// Simple message counting
var messages = new List<ChatMessage>
{
    new("system", "You are a helpful assistant."),
    new("user", "What is the weather in Paris?"),
};
int count = TikTokenEncoder.CountMessageTokens("gpt-4o", messages);

// With tool/function definitions
var tools = new List<ChatFunction>
{
    new("get_weather", "Get the current weather", new List<FunctionParameter>
    {
        new("location", "string", "The city name", isRequired: true),
        new("unit", "string", "Temperature unit",
            enumValues: new[] { "celsius", "fahrenheit" }),
    }),
};
int countWithTools = TikTokenEncoder.CountMessageTokens("gpt-4o", messages, tools);

// Or use the Encoder instance directly
var encoder = ModelToEncoder.For("gpt-4o");
int toolTokens = encoder.CountToolTokens(tools);
```

Nested object parameters and array types are also supported:
```csharp
new FunctionParameter("address", "object", "Mailing address", properties: new List<FunctionParameter>
{
    new("street", "string", "Street address", isRequired: true),
    new("city", "string", "City name", isRequired: true),
});
```

### Custom encodings

Load encoding data from `.tiktoken` text files or `.ttkb` binary files:

```csharp
using Tiktoken.Encodings;

// Load from file (auto-detects format by extension)
var ranks = EncodingLoader.LoadEncodingFromFile("my_encoding.ttkb");
var ranks = EncodingLoader.LoadEncodingFromFile("my_encoding.tiktoken");

// Load from binary byte array (e.g., from embedded resource or network)
byte[] binaryData = File.ReadAllBytes("my_encoding.ttkb");
var ranks = EncodingLoader.LoadEncodingFromBinaryData(binaryData);

// Convert text format to binary for faster loading
var textRanks = EncodingLoader.LoadEncodingFromFile("my_encoding.tiktoken");
using var output = File.Create("my_encoding.ttkb");
EncodingLoader.WriteEncodingToBinaryStream(output, textRanks);
```

The `.ttkb` binary format loads ~30% faster than `.tiktoken` text (no base64 decoding) and is 34% smaller. See [`data/README.md`](data/README.md) for the format specification and conversion tools.

### Benchmarks

Benchmarked on Apple M4 Max, .NET 10.0, o200k_base encoding. Tested with diverse inputs: short ASCII, multilingual (12 scripts + emoji), CJK-heavy, Python code, and long documents.

#### CountTokens — zero allocation, fastest in class

| Input | SharpToken | TiktokenSharp | Microsoft.ML | **Tiktoken** | **Speedup** |
|-------|-----------|---------------|-------------|-------------|:-----------:|
| Hello, World! (13 chars) | 226 ns | 168 ns | 317 ns | **104 ns** | 1.6-3.0x |
| Multilingual (245 chars, 12 scripts) | 15.3 us | 9.8 us | 5.2 us | **1.2 us** | 4.4-12.9x |
| CJK-heavy (644 chars, 6 scripts) | 113.0 us | 68.9 us | 40.1 us | **2.8 us** | 14.5-40.8x |
| Python code (879 chars) | 13.5 us | 10.2 us | 22.5 us | **6.6 us** | 1.5-3.4x |
| Multilingual long (2249 chars) | 306.9 us | 170.3 us | 77.8 us | **9.7 us** | 8.0-31.6x |
| Bitcoin whitepaper (19866 chars) | 425.9 us | 269.8 us | 343.8 us | **132.4 us** | 2.0-3.2x |

> **Zero allocation** across all inputs (0 B). Tiktoken's advantage is most pronounced on multilingual/CJK text — up to **41x faster** than competitors.

#### Cache effect on CountTokens

Built-in token cache dramatically accelerates repeated non-ASCII patterns:

| Input | No cache | Cached | Cache speedup |
|-------|---------|--------|:-------------:|
| Hello, World! (13 chars) | 108 ns | 109 ns | — |
| Multilingual (245 chars) | 7.7 us | 1.2 us | **6.6x** |
| CJK-heavy (644 chars) | 58.1 us | 2.7 us | **21.3x** |
| Python code (879 chars) | 6.8 us | 7.2 us | — |
| Multilingual long (2249 chars) | 165.8 us | 10.7 us | **15.5x** |
| Bitcoin whitepaper (19866 chars) | 188.5 us | 169.6 us | 1.1x |

> Cache has no effect on ASCII-dominant inputs (already on fast path). On multilingual/CJK text, cache provides **6-21x speedup** by skipping UTF-8 conversion and BPE on subsequent calls.

#### Encode — returns token IDs

| Input | SharpToken | TiktokenSharp | Microsoft.ML | **Tiktoken** | **Speedup** |
|-------|-----------|---------------|-------------|-------------|:-----------:|
| Hello, World! (13 chars) | 218 ns | 168 ns | 322 ns | **128 ns** | 1.3-2.5x |
| Multilingual (245 chars, 12 scripts) | 14.7 us | 9.6 us | 5.4 us | **1.4 us** | 3.9-10.5x |
| CJK-heavy (644 chars, 6 scripts) | 111.6 us | 65.7 us | 37.9 us | **3.4 us** | 11.1-32.6x |
| Python code (879 chars) | 13.5 us | 9.8 us | 22.3 us | **7.0 us** | 1.4-3.2x |
| Multilingual long (2249 chars) | 294.2 us | 156.4 us | 72.5 us | **12.5 us** | 5.8-23.5x |
| Bitcoin whitepaper (19866 chars) | 399.8 us | 257.0 us | 335.0 us | **142.4 us** | 1.8-2.8x |

> Same performance characteristics as CountTokens, with additional allocation for the output `int[]` array.

#### Construction — encoder initialization

| Encoding | Time | Description |
|----------|------|-------------|
| **o200k_base** | **58 ms** | GPT-4o (200K vocab, binary .ttkb format, parallel FrozenDictionary) |
| **cl100k_base** | **30 ms** | GPT-3.5/4 (100K vocab) |

> Encoder construction includes loading embedded binary data, building hash tables, and compiling regex. Decoder dictionaries are lazy-initialized on first `Decode()` call only. Reuse `Encoder` instances across calls for best performance.

You can view the full raw BenchmarkDotNet reports for each version [here](benchmarks).

<!--BENCHMARKS_END-->

## Support

Priority place for bugs: https://github.com/tryAGI/LangChain/issues  
Priority place for ideas and general questions: https://github.com/tryAGI/LangChain/discussions  
Discord: https://discord.gg/Ca2xhfBf3v  