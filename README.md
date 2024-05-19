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

BenchmarkDotNet v0.13.12, macOS Sonoma 14.4.1 (23E224) [Darwin 23.4.0]
Apple M1 Pro, 1 CPU, 10 logical and 10 physical cores
.NET SDK 8.0.204
  [Host]     : .NET 8.0.4 (8.0.424.16909), Arm64 RyuJIT AdvSIMD
  DefaultJob : .NET 8.0.4 (8.0.424.16909), Arm64 RyuJIT AdvSIMD


```
| Method                     | Categories  | Data                | Mean         | Median       | Ratio | Gen0     | Gen1    | Gen2   | Allocated | Alloc Ratio |
|--------------------------- |------------ |-------------------- |-------------:|-------------:|------:|---------:|--------:|-------:|----------:|------------:|
| **SharpTokenV2_0_1_**          | **CountTokens** | **1. (...)57. [19866]** | **632,817.1 ns** | **632,257.2 ns** |  **1.00** |   **2.9297** |       **-** |      **-** |   **20115 B** |        **1.00** |
| TiktokenSharpV1_0_9_       | CountTokens | 1. (...)57. [19866] | 463,840.3 ns | 458,851.3 ns |  0.74 |  64.4531 |  3.4180 |      - |  404649 B |       20.12 |
| TokenizerLibV1_3_3_        | CountTokens | 1. (...)57. [19866] | 801,796.0 ns | 806,271.8 ns |  1.27 | 247.0703 | 98.6328 | 0.9766 | 1547675 B |       76.94 |
| Tiktoken_                  | CountTokens | 1. (...)57. [19866] | 319,697.2 ns | 319,475.1 ns |  0.50 |  49.3164 |       - |      - |  309449 B |       15.38 |
|                            |             |                     |              |              |       |          |         |        |           |             |
| **SharpTokenV2_0_1_**          | **CountTokens** | **Hello, World!**       |     **478.1 ns** |     **478.1 ns** |  **1.00** |   **0.0401** |       **-** |      **-** |     **256 B** |        **1.00** |
| TiktokenSharpV1_0_9_       | CountTokens | Hello, World!       |     275.2 ns |     275.1 ns |  0.58 |   0.0505 |       - |      - |     320 B |        1.25 |
| TokenizerLibV1_3_3_        | CountTokens | Hello, World!       |     498.1 ns |     497.4 ns |  1.04 |   0.2356 |       - |      - |    1480 B |        5.78 |
| Tiktoken_                  | CountTokens | Hello, World!       |     212.9 ns |     212.8 ns |  0.45 |   0.0420 |       - |      - |     264 B |        1.03 |
|                            |             |                     |              |              |       |          |         |        |           |             |
| **SharpTokenV2_0_1_**          | **CountTokens** | **King(...)edy. [275]** |   **6,652.5 ns** |   **6,651.9 ns** |  **1.00** |   **0.0763** |       **-** |      **-** |     **520 B** |        **1.00** |
| TiktokenSharpV1_0_9_       | CountTokens | King(...)edy. [275] |   4,774.2 ns |   4,781.1 ns |  0.72 |   0.8011 |       - |      - |    5064 B |        9.74 |
| TokenizerLibV1_3_3_        | CountTokens | King(...)edy. [275] |   7,261.6 ns |   7,241.6 ns |  1.09 |   3.0899 |  0.1450 | 0.0076 |   19344 B |       37.20 |
| Tiktoken_                  | CountTokens | King(...)edy. [275] |   3,216.1 ns |   3,189.9 ns |  0.49 |   0.6447 |       - |      - |    4064 B |        7.82 |
|                            |             |                     |              |              |       |          |         |        |           |             |
| **SharpTokenV2_0_1_Encode**    | **Encode**      | **1. (...)57. [19866]** | **613,700.9 ns** | **612,821.4 ns** |  **1.00** |   **2.9297** |       **-** |      **-** |   **20115 B** |        **1.00** |
| TiktokenSharpV1_0_9_Encode | Encode      | 1. (...)57. [19866] | 444,436.3 ns | 444,298.4 ns |  0.72 |  64.4531 |  3.4180 |      - |  404649 B |       20.12 |
| TokenizerLibV1_3_3_Encode  | Encode      | 1. (...)57. [19866] | 773,882.5 ns | 774,314.3 ns |  1.26 | 246.0938 | 85.9375 |      - | 1547673 B |       76.94 |
| Tiktoken_Encode            | Encode      | 1. (...)57. [19866] | 335,482.3 ns | 333,936.4 ns |  0.55 |  59.5703 |  2.4414 |      - |  375601 B |       18.67 |
|                            |             |                     |              |              |       |          |         |        |           |             |
| **SharpTokenV2_0_1_Encode**    | **Encode**      | **Hello, World!**       |     **443.7 ns** |     **436.8 ns** |  **1.00** |   **0.0405** |       **-** |      **-** |     **256 B** |        **1.00** |
| TiktokenSharpV1_0_9_Encode | Encode      | Hello, World!       |     300.4 ns |     299.4 ns |  0.67 |   0.0505 |       - |      - |     320 B |        1.25 |
| TokenizerLibV1_3_3_Encode  | Encode      | Hello, World!       |     504.7 ns |     498.5 ns |  1.15 |   0.2356 |  0.0010 |      - |    1480 B |        5.78 |
| Tiktoken_Encode            | Encode      | Hello, World!       |     262.4 ns |     262.6 ns |  0.58 |   0.1030 |       - |      - |     648 B |        2.53 |
|                            |             |                     |              |              |       |          |         |        |           |             |
| **SharpTokenV2_0_1_Encode**    | **Encode**      | **King(...)edy. [275]** |   **6,784.3 ns** |   **6,714.1 ns** |  **1.00** |   **0.0763** |       **-** |      **-** |     **520 B** |        **1.00** |
| TiktokenSharpV1_0_9_Encode | Encode      | King(...)edy. [275] |   4,691.2 ns |   4,690.7 ns |  0.69 |   0.8011 |       - |      - |    5064 B |        9.74 |
| TokenizerLibV1_3_3_Encode  | Encode      | King(...)edy. [275] |   7,287.9 ns |   7,290.9 ns |  1.08 |   3.0823 |  0.1373 |      - |   19344 B |       37.20 |
| Tiktoken_Encode            | Encode      | King(...)edy. [275] |   3,606.2 ns |   3,607.4 ns |  0.53 |   0.7973 |       - |      - |    5024 B |        9.66 |

<!--BENCHMARKS_END-->

## Support

Priority place for bugs: https://github.com/tryAGI/LangChain/issues  
Priority place for ideas and general questions: https://github.com/tryAGI/LangChain/discussions  
Discord: https://discord.gg/Ca2xhfBf3v  