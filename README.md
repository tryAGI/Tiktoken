# Tiktoken

[![Nuget package](https://img.shields.io/nuget/vpre/Tiktoken)](https://www.nuget.org/packages/Tiktoken/)
[![dotnet](https://github.com/tryAGI/Tiktoken/actions/workflows/dotnet.yml/badge.svg?branch=main)](https://github.com/tryAGI/Tiktoken/actions/workflows/dotnet.yml)
[![License: MIT](https://img.shields.io/github/license/tryAGI/Tiktoken)](https://github.com/tryAGI/Tiktoken/blob/main/LICENSE.txt)
[![Discord](https://img.shields.io/discord/1115206893015662663?label=Discord&logo=discord&logoColor=white&color=d82679)](https://discord.gg/Ca2xhfBf3v)

This implementation aims for maximum performance, especially in the token count operation.  
There's also a benchmark console app here for easy tracking of this.  
We will be happy to accept any PR.  

### Implemented encodings
- `cl100k_base`
- `r50k_base`
- `p50k_base`
- `p50k_edit`

### Usage
```csharp
var encoding = Tiktoken.Encoding.ForModel("gpt-4");
var tokens = encoding.Encode("hello world"); // [15339, 1917]
var text = encoding.Decode(tokens); // hello world
var numberOfTokens = encoding.CountTokens(text); // 2
var stringTokens = encoding.Explore(text); // ["hello", " world"]

var encoding = Tiktoken.Encoding.Get("p50k_base");
var tokens = encoding.Encode("hello world"); // [31373, 995]
var text = encoding.Decode(tokens); // hello world
```

### Benchmarks
You can view the reports for each version [here](benchmarks)

<!--BENCHMARKS_START-->
```

BenchmarkDotNet v0.13.6, macOS Ventura 13.4.1 (c) (22F770820d) [Darwin 22.5.0]
Apple M1 Pro, 1 CPU, 10 logical and 10 physical cores
.NET SDK 7.0.304
  [Host]     : .NET 7.0.7 (7.0.723.27404), Arm64 RyuJIT AdvSIMD
  Job-DVSVKO : .NET 7.0.7 (7.0.723.27404), Arm64 RyuJIT AdvSIMD DEBUG

BuildConfiguration=Debug  

```
|                     Method |  Categories |                Data |           Mean | Ratio |     Gen0 |     Gen1 |   Gen2 | Allocated | Alloc Ratio |
|--------------------------- |------------ |-------------------- |---------------:|------:|---------:|---------:|-------:|----------:|------------:|
|          **SharpTokenV1_2_2_** | **CountTokens** | **1. (...)57. [19866]** | **1,447,044.6 ns** |  **1.00** | **292.9688** | **146.4844** |      **-** | **1846187 B** |        **1.00** |
|       TiktokenSharpV1_0_6_ | CountTokens | 1. (...)57. [19866] |   965,806.7 ns |  0.67 | 250.0000 | 125.0000 |      - | 1571155 B |        0.85 |
|        TokenizerLibV1_3_2_ | CountTokens | 1. (...)57. [19866] |   842,828.4 ns |  0.58 | 246.0938 |  83.9844 |      - | 1547673 B |        0.84 |
|                  Tiktoken_ | CountTokens | 1. (...)57. [19866] |   414,936.5 ns |  0.29 |  49.3164 |        - |      - |  309449 B |        0.17 |
|                            |             |                     |                |       |          |          |        |           |             |
|          **SharpTokenV1_2_2_** | **CountTokens** |       **Hello, World!** |     **1,715.3 ns** |  **1.00** |   **0.5264** |        **-** |      **-** |    **3304 B** |        **1.00** |
|       TiktokenSharpV1_0_6_ | CountTokens |       Hello, World! |     6,224.0 ns |  3.63 |   2.1820 |   0.0305 |      - |   13728 B |        4.15 |
|        TokenizerLibV1_3_2_ | CountTokens |       Hello, World! |       655.9 ns |  0.38 |   0.2356 |        - |      - |    1480 B |        0.45 |
|                  Tiktoken_ | CountTokens |       Hello, World! |       328.9 ns |  0.19 |   0.0420 |        - |      - |     264 B |        0.08 |
|                            |             |                     |                |       |          |          |        |           |             |
|          **SharpTokenV1_2_2_** | **CountTokens** | **King(...)edy. [275]** |    **15,497.6 ns** |  **1.00** |   **4.1199** |   **0.1526** |      **-** |   **26008 B** |        **1.00** |
|       TiktokenSharpV1_0_6_ | CountTokens | King(...)edy. [275] |    14,615.5 ns |  0.94 |   5.1117 |   0.1678 |      - |   32096 B |        1.23 |
|        TokenizerLibV1_3_2_ | CountTokens | King(...)edy. [275] |     8,513.5 ns |  0.55 |   3.0823 |   0.1373 |      - |   19344 B |        0.74 |
|                  Tiktoken_ | CountTokens | King(...)edy. [275] |     4,618.6 ns |  0.30 |   0.6409 |        - |      - |    4032 B |        0.16 |
|                            |             |                     |                |       |          |          |        |           |             |
|    **SharpTokenV1_2_2_Encode** |      **Encode** | **1. (...)57. [19866]** | **1,517,535.2 ns** |  **1.00** | **294.9219** | **121.0938** | **1.9531** | **1846190 B** |        **1.00** |
| TiktokenSharpV1_0_6_Encode |      Encode | 1. (...)57. [19866] | 1,086,903.6 ns |  0.72 | 250.0000 | 125.0000 |      - | 1571155 B |        0.85 |
|  TokenizerLibV1_3_2_Encode |      Encode | 1. (...)57. [19866] |   927,086.0 ns |  0.61 | 248.0469 | 124.0234 | 1.9531 | 1547676 B |        0.84 |
|            Tiktoken_Encode |      Encode | 1. (...)57. [19866] |   432,878.7 ns |  0.29 |  59.5703 |  29.7852 |      - |  375665 B |        0.20 |
|                            |             |                     |                |       |          |          |        |           |             |
|    **SharpTokenV1_2_2_Encode** |      **Encode** |       **Hello, World!** |     **1,717.5 ns** |  **1.00** |   **0.5264** |   **0.0019** |      **-** |    **3304 B** |        **1.00** |
| TiktokenSharpV1_0_6_Encode |      Encode |       Hello, World! |     6,233.2 ns |  3.63 |   2.1820 |   0.0381 |      - |   13728 B |        4.15 |
|  TokenizerLibV1_3_2_Encode |      Encode |       Hello, World! |       653.7 ns |  0.38 |   0.2356 |   0.0019 |      - |    1480 B |        0.45 |
|            Tiktoken_Encode |      Encode |       Hello, World! |       443.2 ns |  0.26 |   0.1135 |   0.0005 |      - |     712 B |        0.22 |
|                            |             |                     |                |       |          |          |        |           |             |
|    **SharpTokenV1_2_2_Encode** |      **Encode** | **King(...)edy. [275]** |    **15,284.3 ns** |  **1.00** |   **4.1199** |   **0.2136** |      **-** |   **26008 B** |        **1.00** |
| TiktokenSharpV1_0_6_Encode |      Encode | King(...)edy. [275] |    14,766.6 ns |  0.97 |   5.1117 |   0.2594 |      - |   32096 B |        1.23 |
|  TokenizerLibV1_3_2_Encode |      Encode | King(...)edy. [275] |     8,450.9 ns |  0.55 |   3.0823 |   0.1831 |      - |   19344 B |        0.74 |
|            Tiktoken_Encode |      Encode | King(...)edy. [275] |     4,917.3 ns |  0.32 |   0.8011 |   0.0153 |      - |    5056 B |        0.19 |

<!--BENCHMARKS_END-->

### Possible optimizations
- Modes - Fast(without special token regex)/Strict
- SIMD?
- Parallelism?
- string as dictionary key?

## Support

Priority place for bugs: https://github.com/tryAGI/LangChain/issues  
Priority place for ideas and general questions: https://github.com/tryAGI/LangChain/discussions  
Discord: https://discord.gg/Ca2xhfBf3v  