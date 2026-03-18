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

var encoder = ModelToEncoder.For("gpt-4o"); // or explicitly using new Encoder(new O200KBase())
var tokens = encoder.Encode("hello world"); // [15339, 1917]
var text = encoder.Decode(tokens); // hello world
var numberOfTokens = encoder.CountTokens(text); // 2
var stringTokens = encoder.Explore(text); // ["hello", " world"]
```

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
| Method                            | Categories  | Data                | Mean          | Ratio | Gen0     | Gen1    | Allocated | Alloc Ratio |
|---------------------------------- |------------ |-------------------- |--------------:|------:|---------:|--------:|----------:|------------:|
| **SharpTokenV2_0_3_**                 | **CountTokens** | **1. (...)57. [19866]** | **352,748.15 ns** |  **1.00** |   **1.9531** |       **-** |   **20112 B** |        **1.00** |
| TiktokenSharpV1_1_5_              | CountTokens | 1. (...)57. [19866] | 242,255.09 ns |  0.69 |   7.8125 |  0.4883 |   65968 B |        3.28 |
| MicrosoftMLTokenizerV1_0_0_       | CountTokens | 1. (...)57. [19866] | 244,002.56 ns |  0.69 |        - |       - |     304 B |        0.02 |
| TokenizerLibV1_3_3_               | CountTokens | 1. (...)57. [19866] | 473,840.64 ns |  1.34 | 184.5703 | 75.1953 | 1547672 B |       76.95 |
| Tiktoken_                         | CountTokens | 1. (...)57. [19866] | 154,192.33 ns |  0.44 |        - |       - |         - |        0.00 |
|                                   |             |                     |               |       |          |         |           |             |
| **SharpTokenV2_0_3_**                 | **CountTokens** | **Hello, World!**       |     **226.96 ns** |  **1.00** |   **0.0305** |       **-** |     **256 B** |        **1.00** |
| TiktokenSharpV1_1_5_              | CountTokens | Hello, World!       |     165.53 ns |  0.73 |   0.0238 |       - |     200 B |        0.78 |
| MicrosoftMLTokenizerV1_0_0_       | CountTokens | Hello, World!       |     191.55 ns |  0.84 |   0.0124 |       - |     104 B |        0.41 |
| TokenizerLibV1_3_3_               | CountTokens | Hello, World!       |     292.79 ns |  1.29 |   0.1769 |  0.0005 |    1480 B |        5.78 |
| Tiktoken_                         | CountTokens | Hello, World!       |     101.90 ns |  0.45 |        - |       - |         - |        0.00 |
|                                   |             |                     |               |       |          |         |           |             |
| **SharpTokenV2_0_3_**                 | **CountTokens** | **King(...)edy. [275]** |   **3,941.04 ns** |  **1.00** |   **0.0610** |       **-** |     **520 B** |        **1.00** |
| TiktokenSharpV1_1_5_              | CountTokens | King(...)edy. [275] |   2,442.44 ns |  0.62 |   0.0916 |       - |     776 B |        1.49 |
| MicrosoftMLTokenizerV1_0_0_       | CountTokens | King(...)edy. [275] |   2,122.37 ns |  0.54 |   0.0114 |       - |     104 B |        0.20 |
| TokenizerLibV1_3_3_               | CountTokens | King(...)edy. [275] |   4,698.25 ns |  1.19 |   2.3117 |  0.0992 |   19344 B |       37.20 |
| Tiktoken_                         | CountTokens | King(...)edy. [275] |   1,382.18 ns |  0.35 |   0.0038 |       - |      32 B |        0.06 |
|                                   |             |                     |               |       |          |         |           |             |
| **SharpTokenV2_0_3_Decode**           | **Decode**      | **1. (...)57. [19866]** |  **46,848.18 ns** |  **1.00** |  **14.8926** |       **-** |  **125232 B** |        **1.00** |
| TiktokenSharpV1_1_5_Decode        | Decode      | 1. (...)57. [19866] |  35,494.59 ns |  0.76 |  15.8691 |  2.6245 |  133400 B |        1.07 |
| MicrosoftMLTokenizerV1_0_0_Decode | Decode      | 1. (...)57. [19866] |  67,996.10 ns |  1.45 |   4.6387 |       - |   39800 B |        0.32 |
| TokenizerLibV1_3_3_Decode         | Decode      | 1. (...)57. [19866] |  47,744.68 ns |  1.02 |  28.0151 |  2.9297 |  234680 B |        1.87 |
| Tiktoken_Decode                   | Decode      | 1. (...)57. [19866] |  43,773.75 ns |  0.93 |   4.6997 |       - |   39800 B |        0.32 |
|                                   |             |                     |               |       |          |         |           |             |
| **SharpTokenV2_0_3_Decode**           | **Decode**      | **Hello, World!**       |      **60.35 ns** |  **1.00** |   **0.0564** |       **-** |     **472 B** |        **1.00** |
| TiktokenSharpV1_1_5_Decode        | Decode      | Hello, World!       |      42.81 ns |  0.71 |   0.0105 |       - |      88 B |        0.19 |
| MicrosoftMLTokenizerV1_0_0_Decode | Decode      | Hello, World!       |      46.36 ns |  0.77 |   0.0105 |       - |      88 B |        0.19 |
| TokenizerLibV1_3_3_Decode         | Decode      | Hello, World!       |      46.03 ns |  0.76 |   0.0344 |       - |     288 B |        0.61 |
| Tiktoken_Decode                   | Decode      | Hello, World!       |      37.60 ns |  0.62 |   0.0105 |       - |      88 B |        0.19 |
|                                   |             |                     |               |       |          |         |           |             |
| **SharpTokenV2_0_3_Decode**           | **Decode**      | **King(...)edy. [275]** |     **556.70 ns** |  **1.00** |   **0.2146** |       **-** |    **1800 B** |        **1.00** |
| TiktokenSharpV1_1_5_Decode        | Decode      | King(...)edy. [275] |     458.64 ns |  0.82 |   0.0734 |       - |     616 B |        0.34 |
| MicrosoftMLTokenizerV1_0_0_Decode | Decode      | King(...)edy. [275] |     562.85 ns |  1.01 |   0.0734 |       - |     616 B |        0.34 |
| TokenizerLibV1_3_3_Decode         | Decode      | King(...)edy. [275] |     447.74 ns |  0.80 |   0.3901 |  0.0005 |    3264 B |        1.81 |
| Tiktoken_Decode                   | Decode      | King(...)edy. [275] |     374.09 ns |  0.67 |   0.0734 |       - |     616 B |        0.34 |
|                                   |             |                     |               |       |          |         |           |             |
| **SharpTokenV2_0_3_Encode**           | **Encode**      | **1. (...)57. [19866]** | **359,194.60 ns** |  **1.00** |   **1.9531** |       **-** |   **20112 B** |        **1.00** |
| TiktokenSharpV1_1_5_Encode        | Encode      | 1. (...)57. [19866] | 239,457.49 ns |  0.67 |   7.8125 |  0.4883 |   65968 B |        3.28 |
| MicrosoftMLTokenizerV1_0_0_Encode | Encode      | 1. (...)57. [19866] | 250,800.76 ns |  0.70 |   7.8125 |  0.4883 |   66144 B |        3.29 |
| TokenizerLibV1_3_3_Encode         | Encode      | 1. (...)57. [19866] | 489,822.50 ns |  1.36 | 184.5703 | 75.1953 | 1547672 B |       76.95 |
| Tiktoken_Encode                   | Encode      | 1. (...)57. [19866] | 168,313.91 ns |  0.47 |   7.8125 |  0.7324 |   66152 B |        3.29 |
|                                   |             |                     |               |       |          |         |           |             |
| **SharpTokenV2_0_3_Encode**           | **Encode**      | **Hello, World!**       |     **231.24 ns** |  **1.00** |   **0.0305** |       **-** |     **256 B** |        **1.00** |
| TiktokenSharpV1_1_5_Encode        | Encode      | Hello, World!       |     165.16 ns |  0.71 |   0.0238 |       - |     200 B |        0.78 |
| MicrosoftMLTokenizerV1_0_0_Encode | Encode      | Hello, World!       |     200.68 ns |  0.87 |   0.0210 |       - |     176 B |        0.69 |
| TokenizerLibV1_3_3_Encode         | Encode      | Hello, World!       |     291.37 ns |  1.26 |   0.1769 |  0.0005 |    1480 B |        5.78 |
| Tiktoken_Encode                   | Encode      | Hello, World!       |     146.61 ns |  0.63 |   0.0458 |       - |     384 B |        1.50 |
|                                   |             |                     |               |       |          |         |           |             |
| **SharpTokenV2_0_3_Encode**           | **Encode**      | **King(...)edy. [275]** |   **3,977.22 ns** |  **1.00** |   **0.0610** |       **-** |     **520 B** |        **1.00** |
| TiktokenSharpV1_1_5_Encode        | Encode      | King(...)edy. [275] |   2,452.75 ns |  0.62 |   0.0916 |       - |     776 B |        1.49 |
| MicrosoftMLTokenizerV1_0_0_Encode | Encode      | King(...)edy. [275] |   2,202.46 ns |  0.55 |   0.0877 |       - |     752 B |        1.45 |
| TokenizerLibV1_3_3_Encode         | Encode      | King(...)edy. [275] |   4,742.05 ns |  1.19 |   2.3117 |  0.0992 |   19344 B |       37.20 |
| Tiktoken_Encode                   | Encode      | King(...)edy. [275] |   1,549.81 ns |  0.39 |   0.1183 |       - |     992 B |        1.91 |

<!--BENCHMARKS_END-->

## Support

Priority place for bugs: https://github.com/tryAGI/LangChain/issues  
Priority place for ideas and general questions: https://github.com/tryAGI/LangChain/discussions  
Discord: https://discord.gg/Ca2xhfBf3v  