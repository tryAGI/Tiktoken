# Tiktoken

[![Nuget package](https://img.shields.io/nuget/vpre/Tiktoken)](https://www.nuget.org/packages/Tiktoken/)
[![dotnet](https://github.com/tryAGI/Tiktoken/actions/workflows/dotnet.yml/badge.svg?branch=main)](https://github.com/tryAGI/Tiktoken/actions/workflows/dotnet.yml)
[![License: MIT](https://img.shields.io/github/license/tryAGI/Tiktoken)](https://github.com/tryAGI/Tiktoken/blob/main/LICENSE.txt)
[![Discord](https://img.shields.io/discord/1115206893015662663?label=Discord&logo=discord&logoColor=white&color=d82679)](https://discord.gg/Ca2xhfBf3v)
[![Throughput](https://img.shields.io/badge/throughput-618_MiB%2Fs-brightgreen)](https://github.com/tryAGI/Tiktoken#benchmarks)

One of the fastest BPE tokenizers in any language — the fastest in .NET, competitive with pure Rust implementations.
Zero-allocation token counting, built-in multilingual cache, and up to **42x faster** than other .NET tokenizers.
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

| Input | SharpToken | TiktokenSharp | Microsoft.ML | **Tiktoken** | **Throughput** | **Speedup** |
|-------|-----------|---------------|-------------|-------------|:-----------:|:-----------:|
| Hello, World! (13 B) | 217 ns | 164 ns | 319 ns | **88 ns** | 141 MiB/s | 1.9-3.6x |
| Multilingual (382 B, 12 scripts) | 14.7 us | 9.5 us | 5.1 us | **1.1 us** | 339 MiB/s | 4.7-13.6x |
| CJK-heavy (1,676 B, 6 scripts) | 109.4 us | 65.6 us | 37.0 us | **2.6 us** | 618 MiB/s | 14.3-42.3x |
| Python code (879 B) | 13.1 us | 9.7 us | 21.6 us | **5.5 us** | 153 MiB/s | 1.8-4.0x |
| Multilingual long (4,312 B) | 283.1 us | 155.7 us | 71.0 us | **9.0 us** | 458 MiB/s | 7.9-31.6x |
| Bitcoin whitepaper (19,884 B) | 400.3 us | 255.4 us | 321.3 us | **105.1 us** | 180 MiB/s | 2.4-3.8x |

> **Zero allocation** across all inputs (0 B). Tiktoken's advantage is most pronounced on multilingual/CJK text — up to **42x faster** than competitors. Throughput on cached multilingual text reaches **618 MiB/s**, competitive with the fastest Rust tokenizers.

#### Cache effect on CountTokens

Built-in token cache dramatically accelerates repeated non-ASCII patterns:

| Input | No cache | Cached | Cache speedup |
|-------|---------|--------|:-------------:|
| Hello, World! (13 B) | 88 ns | 86 ns | — |
| Multilingual (382 B) | 5.4 us | 1.1 us | **4.9x** |
| CJK-heavy (1,676 B) | 33.7 us | 2.6 us | **13.1x** |
| Python code (879 B) | 5.6 us | 5.5 us | — |
| Multilingual long (4,312 B) | 78.0 us | 9.0 us | **8.6x** |
| Bitcoin whitepaper (19,884 B) | 122.7 us | 104.9 us | 1.2x |

> Cache has no effect on ASCII-dominant inputs (already on fast path). On multilingual/CJK text, cache provides **5-13x speedup** by skipping UTF-8 conversion and BPE on subsequent calls. Cold-path performance was significantly improved by the O(n log n) min-heap BPE merge optimization.

#### Encode — returns token IDs

| Input | SharpToken | TiktokenSharp | Microsoft.ML | **Tiktoken** | **Throughput** | **Speedup** |
|-------|-----------|---------------|-------------|-------------|:-----------:|:-----------:|
| Hello, World! (13 B) | 214 ns | 163 ns | 316 ns | **109 ns** | 114 MiB/s | 1.5-2.9x |
| Multilingual (382 B, 12 scripts) | 14.5 us | 9.4 us | 5.2 us | **1.3 us** | 287 MiB/s | 4.1-11.4x |
| CJK-heavy (1,676 B, 6 scripts) | 107.9 us | 64.7 us | 37.0 us | **3.3 us** | 484 MiB/s | 11.2-32.7x |
| Python code (879 B) | 13.1 us | 9.7 us | 23.6 us | **5.8 us** | 145 MiB/s | 1.7-4.1x |
| Multilingual long (4,312 B) | 276.4 us | 151.3 us | 70.7 us | **10.9 us** | 376 MiB/s | 6.5-25.2x |
| Bitcoin whitepaper (19,884 B) | 366.1 us | 245.5 us | 317.7 us | **111.8 us** | 170 MiB/s | 2.2-3.3x |

> Same performance characteristics as CountTokens, with additional allocation for the output `int[]` array.

#### Construction — encoder initialization

| Encoding | Time | Description |
|----------|------|-------------|
| **o200k_base** | **0.78 ms** | GPT-4o (200K vocab, pre-computed hash table, lazy FastEncoder) |
| **cl100k_base** | **0.46 ms** | GPT-3.5/4 (100K vocab) |

> Encoder construction includes loading embedded binary data, building hash tables, and compiling regex. FastEncoder and Decoder dictionaries are lazy-initialized on first use only. Reuse `Encoder` instances across calls for best performance.

#### Cross-language context

All numbers below measured on **Apple M4 Max** with **identical inputs** and **o200k_base** encoding. See [`benchmarks/cross-language/results/`](benchmarks/cross-language/results/) for full reports.

| Implementation | Language | Encode Throughput | CountTokens Throughput | Notes |
|---------------|----------|:-----------------:|:----------------------:|-------|
| **Tiktoken** (cached) | **.NET/C#** | **114-484 MiB/s** | **141-618 MiB/s** | **Zero-alloc counting; cache gives 5-13x on multilingual** |
| **Tiktoken** (no cache) | **.NET/C#** | **44-145 MiB/s** | **47-155 MiB/s** | **Cold/first-call with O(n log n) min-heap BPE merge** |
| [`tiktoken`](https://lib.rs/crates/tiktoken) v3 | Rust | 34-88 MiB/s | — | Pure Rust, arena-based |
| GitHub [`bpe`](https://github.com/github/rust-gems) v0.3 | Rust | 33-64 MiB/s | 29-66 MiB/s | Aho-Corasick, O(n) worst case |
| [OpenAI tiktoken](https://github.com/openai/tiktoken) 0.12 | Python | 7-20 MiB/s | — | Rust core, but Python FFI overhead |

> .NET Tiktoken's token cache makes it dramatically faster than native Rust on repeated/multilingual text — up to **7x faster** than the fastest Rust tokenizer on CJK text. Even without the cache, .NET is competitive with or faster than both Rust crates on most inputs thanks to the O(n log n) min-heap BPE merge optimization.

You can view the full raw BenchmarkDotNet reports for each version [here](benchmarks).

## Support

Priority place for bugs: https://github.com/tryAGI/LangChain/issues  
Priority place for ideas and general questions: https://github.com/tryAGI/LangChain/discussions  
Discord: https://discord.gg/Ca2xhfBf3v  