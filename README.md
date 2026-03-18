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
    "https://huggingface.co/openai-community/gpt2/raw/main/tokenizer.json",
    httpClient,
    name: "gpt2");

// Custom regex patterns (optional — auto-detected by default)
var encoding = TokenizerJsonLoader.FromFile("tokenizer.json", patterns: myPatterns);
```

**Supported pre-tokenizer types:**
- `ByteLevel` — GPT-2 and similar models
- `Split` with regex pattern — direct regex-based splitting
- `Sequence[Split, ByteLevel]` — Llama 3, Qwen2, DeepSeek, and other modern models

### Benchmarks
You can view the reports for each version [here](benchmarks)

<!--BENCHMARKS_START-->
```

BenchmarkDotNet v0.15.8, macOS Tahoe 26.3.1 (25D2128) [Darwin 25.3.0]
Apple M4 Max, 1 CPU, 16 logical and 16 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), Arm64 RyuJIT armv8.0-a
  DefaultJob : .NET 10.0.5 (10.0.5, 10.0.526.15411), Arm64 RyuJIT armv8.0-a


```
| Method                            | Categories        | Data                | Mean          | Ratio | Gen0     | Gen1    | Allocated | Alloc Ratio |
|---------------------------------- |------------------ |-------------------- |--------------:|------:|---------:|--------:|----------:|------------:|
| **SharpTokenV2_0_3_**                 | **CountTokens**       | **1. (...)57. [19866]** | **371,053.29 ns** |  **1.00** |   **1.9531** |       **-** |   **20112 B** |        **1.00** |
| TiktokenSharpV1_1_5_              | CountTokens       | 1. (...)57. [19866] | 251,531.04 ns |  0.68 |   7.8125 |  0.4883 |   65968 B |        3.28 |
| MicrosoftMLTokenizerV1_0_0_       | CountTokens       | 1. (...)57. [19866] | 259,868.29 ns |  0.70 |        - |       - |     304 B |        0.02 |
| TokenizerLibV1_3_3_               | CountTokens       | 1. (...)57. [19866] | 502,554.08 ns |  1.35 | 184.5703 | 75.1953 | 1547672 B |       76.95 |
| Tiktoken_                         | CountTokens       | 1. (...)57. [19866] | 164,658.42 ns |  0.44 |        - |       - |         - |        0.00 |
|                                   |                   |                     |               |       |          |         |           |             |
| **SharpTokenV2_0_3_**                 | **CountTokens**       | **Hello, World!**       |     **239.14 ns** |  **1.00** |   **0.0305** |       **-** |     **256 B** |        **1.00** |
| TiktokenSharpV1_1_5_              | CountTokens       | Hello, World!       |     170.46 ns |  0.71 |   0.0238 |       - |     200 B |        0.78 |
| MicrosoftMLTokenizerV1_0_0_       | CountTokens       | Hello, World!       |     208.69 ns |  0.87 |   0.0124 |       - |     104 B |        0.41 |
| TokenizerLibV1_3_3_               | CountTokens       | Hello, World!       |     316.45 ns |  1.32 |   0.1769 |  0.0005 |    1480 B |        5.78 |
| Tiktoken_                         | CountTokens       | Hello, World!       |     103.31 ns |  0.43 |        - |       - |         - |        0.00 |
|                                   |                   |                     |               |       |          |         |           |             |
| **SharpTokenV2_0_3_**                 | **CountTokens**       | **King(...)edy. [275]** |   **4,055.29 ns** |  **1.00** |   **0.0610** |       **-** |     **520 B** |        **1.00** |
| TiktokenSharpV1_1_5_              | CountTokens       | King(...)edy. [275] |   2,552.89 ns |  0.63 |   0.0916 |       - |     776 B |        1.49 |
| MicrosoftMLTokenizerV1_0_0_       | CountTokens       | King(...)edy. [275] |   2,270.94 ns |  0.56 |   0.0114 |       - |     104 B |        0.20 |
| TokenizerLibV1_3_3_               | CountTokens       | King(...)edy. [275] |   4,846.66 ns |  1.20 |   2.3117 |  0.0992 |   19344 B |       37.20 |
| Tiktoken_                         | CountTokens       | King(...)edy. [275] |   1,418.07 ns |  0.35 |   0.0038 |       - |      32 B |        0.06 |
|                                   |                   |                     |               |       |          |         |           |             |
| **Tiktoken_CountTokens_String**       | **CountTokensSpan**   | **1. (...)57. [19866]** | **159,648.81 ns** |  **1.00** |        **-** |       **-** |         **-** |          **NA** |
| Tiktoken_CountTokens_Span         | CountTokensSpan   | 1. (...)57. [19866] | 161,476.23 ns |  1.01 |        - |       - |         - |          NA |
|                                   |                   |                     |               |       |          |         |           |             |
| **Tiktoken_CountTokens_String**       | **CountTokensSpan**   | **Hello, World!**       |     **104.46 ns** |  **1.00** |        **-** |       **-** |         **-** |          **NA** |
| Tiktoken_CountTokens_Span         | CountTokensSpan   | Hello, World!       |     104.17 ns |  1.00 |        - |       - |         - |          NA |
|                                   |                   |                     |               |       |          |         |           |             |
| **Tiktoken_CountTokens_String**       | **CountTokensSpan**   | **King(...)edy. [275]** |   **1,383.41 ns** |  **1.00** |   **0.0038** |       **-** |      **32 B** |        **1.00** |
| Tiktoken_CountTokens_Span         | CountTokensSpan   | King(...)edy. [275] |   1,390.51 ns |  1.01 |   0.0038 |       - |      32 B |        1.00 |
|                                   |                   |                     |               |       |          |         |           |             |
| **Tiktoken_CountTokens_FromString**   | **CountTokensUtf8**   | **1. (...)57. [19866]** | **152,452.67 ns** |  **1.00** |        **-** |       **-** |         **-** |          **NA** |
| Tiktoken_CountTokens_FromUtf8     | CountTokensUtf8   | 1. (...)57. [19866] | 157,322.51 ns |  1.03 |        - |       - |         - |          NA |
|                                   |                   |                     |               |       |          |         |           |             |
| **Tiktoken_CountTokens_FromString**   | **CountTokensUtf8**   | **Hello, World!**       |      **97.91 ns** |  **1.00** |        **-** |       **-** |         **-** |          **NA** |
| Tiktoken_CountTokens_FromUtf8     | CountTokensUtf8   | Hello, World!       |     105.07 ns |  1.07 |        - |       - |         - |          NA |
|                                   |                   |                     |               |       |          |         |           |             |
| **Tiktoken_CountTokens_FromString**   | **CountTokensUtf8**   | **King(...)edy. [275]** |   **1,364.91 ns** |  **1.00** |   **0.0038** |       **-** |      **32 B** |        **1.00** |
| Tiktoken_CountTokens_FromUtf8     | CountTokensUtf8   | King(...)edy. [275] |   1,411.43 ns |  1.03 |   0.0038 |       - |      32 B |        1.00 |
|                                   |                   |                     |               |       |          |         |           |             |
| **Tiktoken_cl100k_CountTokens**       | **CountTokens_o200k** | **1. (...)57. [19866]** | **151,196.85 ns** |  **1.00** |        **-** |       **-** |         **-** |          **NA** |
| Tiktoken_o200k_CountTokens        | CountTokens_o200k | 1. (...)57. [19866] | 176,174.81 ns |  1.17 |        - |       - |         - |          NA |
|                                   |                   |                     |               |       |          |         |           |             |
| **Tiktoken_cl100k_CountTokens**       | **CountTokens_o200k** | **Hello, World!**       |      **98.78 ns** |  **1.00** |        **-** |       **-** |         **-** |          **NA** |
| Tiktoken_o200k_CountTokens        | CountTokens_o200k | Hello, World!       |     110.40 ns |  1.12 |        - |       - |         - |          NA |
|                                   |                   |                     |               |       |          |         |           |             |
| **Tiktoken_cl100k_CountTokens**       | **CountTokens_o200k** | **King(...)edy. [275]** |   **1,368.74 ns** |  **1.00** |   **0.0038** |       **-** |      **32 B** |        **1.00** |
| Tiktoken_o200k_CountTokens        | CountTokens_o200k | King(...)edy. [275] |   1,491.49 ns |  1.09 |   0.0038 |       - |      32 B |        1.00 |
|                                   |                   |                     |               |       |          |         |           |             |
| **SharpTokenV2_0_3_Decode**           | **Decode**            | **1. (...)57. [19866]** |  **46,445.49 ns** |  **1.00** |  **14.8926** |       **-** |  **125232 B** |        **1.00** |
| TiktokenSharpV1_1_5_Decode        | Decode            | 1. (...)57. [19866] |  35,023.46 ns |  0.75 |  15.8691 |  2.6245 |  133400 B |        1.07 |
| MicrosoftMLTokenizerV1_0_0_Decode | Decode            | 1. (...)57. [19866] |  68,659.89 ns |  1.48 |   4.6387 |       - |   39800 B |        0.32 |
| TokenizerLibV1_3_3_Decode         | Decode            | 1. (...)57. [19866] |  46,817.85 ns |  1.01 |  28.0151 |  2.9297 |  234680 B |        1.87 |
| Tiktoken_Decode                   | Decode            | 1. (...)57. [19866] |  27,567.26 ns |  0.59 |   4.7302 |       - |   39760 B |        0.32 |
|                                   |                   |                     |               |       |          |         |           |             |
| **SharpTokenV2_0_3_Decode**           | **Decode**            | **Hello, World!**       |      **60.40 ns** |  **1.00** |   **0.0564** |       **-** |     **472 B** |        **1.00** |
| TiktokenSharpV1_1_5_Decode        | Decode            | Hello, World!       |      42.18 ns |  0.70 |   0.0105 |       - |      88 B |        0.19 |
| MicrosoftMLTokenizerV1_0_0_Decode | Decode            | Hello, World!       |      46.69 ns |  0.77 |   0.0105 |       - |      88 B |        0.19 |
| TokenizerLibV1_3_3_Decode         | Decode            | Hello, World!       |      46.06 ns |  0.76 |   0.0344 |       - |     288 B |        0.61 |
| Tiktoken_Decode                   | Decode            | Hello, World!       |      22.35 ns |  0.37 |   0.0057 |       - |      48 B |        0.10 |
|                                   |                   |                     |               |       |          |         |           |             |
| **SharpTokenV2_0_3_Decode**           | **Decode**            | **King(...)edy. [275]** |     **552.07 ns** |  **1.00** |   **0.2146** |       **-** |    **1800 B** |        **1.00** |
| TiktokenSharpV1_1_5_Decode        | Decode            | King(...)edy. [275] |     455.47 ns |  0.83 |   0.0734 |       - |     616 B |        0.34 |
| MicrosoftMLTokenizerV1_0_0_Decode | Decode            | King(...)edy. [275] |     561.06 ns |  1.02 |   0.0734 |       - |     616 B |        0.34 |
| TokenizerLibV1_3_3_Decode         | Decode            | King(...)edy. [275] |     442.90 ns |  0.80 |   0.3901 |  0.0005 |    3264 B |        1.81 |
| Tiktoken_Decode                   | Decode            | King(...)edy. [275] |     238.58 ns |  0.43 |   0.0687 |       - |     576 B |        0.32 |
|                                   |                   |                     |               |       |          |         |           |             |
| **Tiktoken_Decode_Baseline**          | **DecodeToUtf8**      | **1. (...)57. [19866]** |  **27,383.51 ns** |  **1.00** |   **4.7302** |       **-** |   **39760 B** |        **1.00** |
| Tiktoken_DecodeToUtf8             | DecodeToUtf8      | 1. (...)57. [19866] |  27,897.29 ns |  1.02 |        - |       - |         - |        0.00 |
|                                   |                   |                     |               |       |          |         |           |             |
| **Tiktoken_Decode_Baseline**          | **DecodeToUtf8**      | **Hello, World!**       |      **22.86 ns** |  **1.00** |   **0.0057** |       **-** |      **48 B** |        **1.00** |
| Tiktoken_DecodeToUtf8             | DecodeToUtf8      | Hello, World!       |      14.60 ns |  0.64 |        - |       - |         - |        0.00 |
|                                   |                   |                     |               |       |          |         |           |             |
| **Tiktoken_Decode_Baseline**          | **DecodeToUtf8**      | **King(...)edy. [275]** |     **238.69 ns** |  **1.00** |   **0.0687** |       **-** |     **576 B** |        **1.00** |
| Tiktoken_DecodeToUtf8             | DecodeToUtf8      | King(...)edy. [275] |     241.59 ns |  1.01 |        - |       - |         - |        0.00 |
|                                   |                   |                     |               |       |          |         |           |             |
| **Tiktoken_cl100k_Decode**            | **Decode_o200k**      | **1. (...)57. [19866]** |  **27,031.98 ns** |  **1.00** |   **4.7302** |       **-** |   **39760 B** |        **1.00** |
| Tiktoken_o200k_Decode             | Decode_o200k      | 1. (...)57. [19866] |  27,957.89 ns |  1.03 |   4.7302 |       - |   39760 B |        1.00 |
|                                   |                   |                     |               |       |          |         |           |             |
| **Tiktoken_cl100k_Decode**            | **Decode_o200k**      | **Hello, World!**       |      **21.63 ns** |  **1.00** |   **0.0057** |       **-** |      **48 B** |        **1.00** |
| Tiktoken_o200k_Decode             | Decode_o200k      | Hello, World!       |      22.86 ns |  1.06 |   0.0057 |       - |      48 B |        1.00 |
|                                   |                   |                     |               |       |          |         |           |             |
| **Tiktoken_cl100k_Decode**            | **Decode_o200k**      | **King(...)edy. [275]** |     **235.36 ns** |  **1.00** |   **0.0687** |       **-** |     **576 B** |        **1.00** |
| Tiktoken_o200k_Decode             | Decode_o200k      | King(...)edy. [275] |     237.36 ns |  1.01 |   0.0687 |       - |     576 B |        1.00 |
|                                   |                   |                     |               |       |          |         |           |             |
| **SharpTokenV2_0_3_Encode**           | **Encode**            | **1. (...)57. [19866]** | **345,110.21 ns** |  **1.00** |   **1.9531** |       **-** |   **20112 B** |        **1.00** |
| TiktokenSharpV1_1_5_Encode        | Encode            | 1. (...)57. [19866] | 240,838.63 ns |  0.70 |   7.8125 |  0.7324 |   65968 B |        3.28 |
| MicrosoftMLTokenizerV1_0_0_Encode | Encode            | 1. (...)57. [19866] | 247,064.98 ns |  0.72 |   7.8125 |  0.4883 |   66144 B |        3.29 |
| TokenizerLibV1_3_3_Encode         | Encode            | 1. (...)57. [19866] | 467,095.12 ns |  1.35 | 184.5703 | 75.1953 | 1547672 B |       76.95 |
| Tiktoken_Encode                   | Encode            | 1. (...)57. [19866] | 164,373.43 ns |  0.48 |   7.8125 |  0.7324 |   65840 B |        3.27 |
|                                   |                   |                     |               |       |          |         |           |             |
| **SharpTokenV2_0_3_Encode**           | **Encode**            | **Hello, World!**       |     **230.38 ns** |  **1.00** |   **0.0305** |       **-** |     **256 B** |        **1.00** |
| TiktokenSharpV1_1_5_Encode        | Encode            | Hello, World!       |     160.05 ns |  0.70 |   0.0238 |       - |     200 B |        0.78 |
| MicrosoftMLTokenizerV1_0_0_Encode | Encode            | Hello, World!       |     202.58 ns |  0.88 |   0.0210 |       - |     176 B |        0.69 |
| TokenizerLibV1_3_3_Encode         | Encode            | Hello, World!       |     289.62 ns |  1.26 |   0.1769 |  0.0005 |    1480 B |        5.78 |
| Tiktoken_Encode                   | Encode            | Hello, World!       |     124.12 ns |  0.54 |   0.0086 |       - |      72 B |        0.28 |
|                                   |                   |                     |               |       |          |         |           |             |
| **SharpTokenV2_0_3_Encode**           | **Encode**            | **King(...)edy. [275]** |   **3,846.77 ns** |  **1.00** |   **0.0610** |       **-** |     **520 B** |        **1.00** |
| TiktokenSharpV1_1_5_Encode        | Encode            | King(...)edy. [275] |   2,484.64 ns |  0.65 |   0.0916 |       - |     776 B |        1.49 |
| MicrosoftMLTokenizerV1_0_0_Encode | Encode            | King(...)edy. [275] |   2,169.79 ns |  0.56 |   0.0877 |       - |     752 B |        1.45 |
| TokenizerLibV1_3_3_Encode         | Encode            | King(...)edy. [275] |   4,689.55 ns |  1.22 |   2.3117 |  0.0992 |   19344 B |       37.20 |
| Tiktoken_Encode                   | Encode            | King(...)edy. [275] |   1,506.18 ns |  0.39 |   0.0801 |       - |     680 B |        1.31 |
|                                   |                   |                     |               |       |          |         |           |             |
| **Tiktoken_Encode_String**            | **EncodeSpan**        | **1. (...)57. [19866]** | **170,807.87 ns** |  **1.00** |   **7.8125** |  **0.7324** |   **65840 B** |        **1.00** |
| Tiktoken_Encode_Span              | EncodeSpan        | 1. (...)57. [19866] | 166,968.48 ns |  0.98 |   7.8125 |  0.7324 |   65840 B |        1.00 |
|                                   |                   |                     |               |       |          |         |           |             |
| **Tiktoken_Encode_String**            | **EncodeSpan**        | **Hello, World!**       |     **125.93 ns** |  **1.00** |   **0.0086** |       **-** |      **72 B** |        **1.00** |
| Tiktoken_Encode_Span              | EncodeSpan        | Hello, World!       |     126.25 ns |  1.00 |   0.0086 |       - |      72 B |        1.00 |
|                                   |                   |                     |               |       |          |         |           |             |
| **Tiktoken_Encode_String**            | **EncodeSpan**        | **King(...)edy. [275]** |   **1,515.16 ns** |  **1.00** |   **0.0801** |       **-** |     **680 B** |        **1.00** |
| Tiktoken_Encode_Span              | EncodeSpan        | King(...)edy. [275] |   1,493.72 ns |  0.99 |   0.0801 |       - |     680 B |        1.00 |
|                                   |                   |                     |               |       |          |         |           |             |
| **Tiktoken_Encode_Baseline**          | **EncodeUtf8**        | **1. (...)57. [19866]** | **169,092.49 ns** |  **1.00** |   **7.8125** |  **0.7324** |   **65840 B** |        **1.00** |
| Tiktoken_EncodeUtf8               | EncodeUtf8        | 1. (...)57. [19866] | 364,039.90 ns |  2.15 |   7.8125 |  0.4883 |   65880 B |        1.00 |
|                                   |                   |                     |               |       |          |         |           |             |
| **Tiktoken_Encode_Baseline**          | **EncodeUtf8**        | **Hello, World!**       |     **125.21 ns** |  **1.00** |   **0.0086** |       **-** |      **72 B** |        **1.00** |
| Tiktoken_EncodeUtf8               | EncodeUtf8        | Hello, World!       |     268.86 ns |  2.15 |   0.0134 |       - |     112 B |        1.56 |
|                                   |                   |                     |               |       |          |         |           |             |
| **Tiktoken_Encode_Baseline**          | **EncodeUtf8**        | **King(...)edy. [275]** |   **1,520.22 ns** |  **1.00** |   **0.0801** |       **-** |     **680 B** |        **1.00** |
| Tiktoken_EncodeUtf8               | EncodeUtf8        | King(...)edy. [275] |   3,234.26 ns |  2.13 |   0.0877 |       - |     752 B |        1.11 |
|                                   |                   |                     |               |       |          |         |           |             |
| **Tiktoken_cl100k_Encode**            | **Encode_o200k**      | **1. (...)57. [19866]** | **172,138.88 ns** |  **1.00** |   **7.8125** |  **0.7324** |   **65840 B** |        **1.00** |
| Tiktoken_o200k_Encode             | Encode_o200k      | 1. (...)57. [19866] | 197,942.14 ns |  1.15 |   7.8125 |  0.7324 |   65840 B |        1.00 |
|                                   |                   |                     |               |       |          |         |           |             |
| **Tiktoken_cl100k_Encode**            | **Encode_o200k**      | **Hello, World!**       |     **127.83 ns** |  **1.00** |   **0.0086** |       **-** |      **72 B** |        **1.00** |
| Tiktoken_o200k_Encode             | Encode_o200k      | Hello, World!       |     138.49 ns |  1.08 |   0.0086 |       - |      72 B |        1.00 |
|                                   |                   |                     |               |       |          |         |           |             |
| **Tiktoken_cl100k_Encode**            | **Encode_o200k**      | **King(...)edy. [275]** |   **1,511.29 ns** |  **1.00** |   **0.0801** |       **-** |     **680 B** |        **1.00** |
| Tiktoken_o200k_Encode             | Encode_o200k      | King(...)edy. [275] |   1,632.33 ns |  1.08 |   0.0801 |       - |     680 B |        1.00 |
|                                   |                   |                     |               |       |          |         |           |             |
| **Tiktoken_Explore**                  | **Explore**           | **1. (...)57. [19866]** | **317,515.51 ns** |  **1.00** |  **63.9648** | **20.9961** |  **538312 B** |        **1.00** |
| Tiktoken_ExploreUtfSafe           | Explore           | 1. (...)57. [19866] | 352,407.43 ns |  1.11 |  83.0078 | 30.2734 |  696384 B |        1.29 |
|                                   |                   |                     |               |       |          |         |           |             |
| **Tiktoken_Explore**                  | **Explore**           | **Hello, World!**       |     **201.20 ns** |  **1.00** |   **0.0772** |       **-** |     **648 B** |        **1.00** |
| Tiktoken_ExploreUtfSafe           | Explore           | Hello, World!       |     222.43 ns |  1.11 |   0.0966 |  0.0002 |     808 B |        1.25 |
|                                   |                   |                     |               |       |          |         |           |             |
| **Tiktoken_Explore**                  | **Explore**           | **King(...)edy. [275]** |   **3,055.34 ns** |  **1.00** |   **0.8278** |  **0.0076** |    **6952 B** |        **1.00** |
| Tiktoken_ExploreUtfSafe           | Explore           | King(...)edy. [275] |   3,391.64 ns |  1.11 |   1.0910 |  0.0191 |    9128 B |        1.31 |

<!--BENCHMARKS_END-->

## Support

Priority place for bugs: https://github.com/tryAGI/LangChain/issues  
Priority place for ideas and general questions: https://github.com/tryAGI/LangChain/discussions  
Discord: https://discord.gg/Ca2xhfBf3v  