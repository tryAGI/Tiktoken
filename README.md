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

### Benchmarks

Benchmarked on Apple M4 Max, .NET 10.0, o200k_base encoding. Tested with diverse inputs: short ASCII, multilingual (12 scripts + emoji), Python code, and long documents.

#### CountTokens — zero allocation, fastest in class

| Input | SharpToken | TiktokenSharp | Microsoft.ML | TokenizerLib | **Tiktoken** | **Speedup** |
|-------|-----------|---------------|-------------|-------------|-------------|:-----------:|
| Hello, World! (13 chars) | 240 ns | 172 ns | 190 ns | 308 ns | **105 ns** | 1.6-2.9x |
| Multilingual (245 chars, 12 scripts) | 14.0 us | 9.6 us | 4.2 us | 4.6 us | **1.5 us** | 2.9-9.7x |
| Python code (879 chars) | 12.6 us | 9.3 us | 13.3 us | 24.0 us | **7.8 us** | 1.2-3.1x |
| Multilingual long (2249 chars) | 210.7 us | 141.6 us | 55.8 us | 45.5 us | **15.9 us** | 2.9-13.3x |
| Bitcoin whitepaper (19866 chars) | 388.2 us | 253.8 us | 263.9 us | 500.1 us | **169.2 us** | 1.5-3.0x |

> Tiktoken's advantage is most pronounced on multilingual text — up to **13x faster** than competitors.

You can view the full raw BenchmarkDotNet reports for each version [here](benchmarks).

<!--BENCHMARKS_END-->

## Support

Priority place for bugs: https://github.com/tryAGI/LangChain/issues  
Priority place for ideas and general questions: https://github.com/tryAGI/LangChain/discussions  
Discord: https://discord.gg/Ca2xhfBf3v  