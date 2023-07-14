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
``` ini

BenchmarkDotNet=v0.13.5, OS=macOS Ventura 13.4 (22F66) [Darwin 22.5.0]
Apple M1 Pro, 1 CPU, 10 logical and 10 physical cores
.NET SDK=7.0.304
  [Host]     : .NET 7.0.7 (7.0.723.27404), Arm64 RyuJIT AdvSIMD
  Job-EVPJQP : .NET 7.0.7 (7.0.723.27404), Arm64 RyuJIT AdvSIMD DEBUG

BuildConfiguration=Debug  

```
|                     Method |  Categories |                Data |           Mean | Ratio |     Gen0 |     Gen1 |   Gen2 | Allocated | Alloc Ratio |
|--------------------------- |------------ |-------------------- |---------------:|------:|---------:|---------:|-------:|----------:|------------:|
|         **SharpTokenV1_0_28_** | **CountTokens** | **1. (...)57. [19866]** | **5,416,309.6 ns** |  **1.00** | **601.5625** | **289.0625** |      **-** | **3805771 B** |        **1.00** |
|       TiktokenSharpV1_0_5_ | CountTokens | 1. (...)57. [19866] | 1,532,135.7 ns |  0.28 | 250.0000 | 125.0000 |      - | 1571155 B |        0.41 |
|        TokenizerLibV1_3_2_ | CountTokens | 1. (...)57. [19866] |   856,737.2 ns |  0.16 | 246.0938 |  87.8906 |      - | 1547674 B |        0.41 |
|                  Tiktoken_ | CountTokens | 1. (...)57. [19866] |   413,599.2 ns |  0.08 |  49.3164 |        - |      - |  309449 B |        0.08 |
|                            |             |                     |                |       |          |          |        |           |             |
|         **SharpTokenV1_0_28_** | **CountTokens** |       **Hello, World!** |     **3,322.0 ns** |  **1.00** |   **0.6752** |        **-** |      **-** |    **4240 B** |        **1.00** |
|       TiktokenSharpV1_0_5_ | CountTokens |       Hello, World! |     6,690.3 ns |  2.02 |   2.1820 |   0.0381 |      - |   13728 B |        3.24 |
|        TokenizerLibV1_3_2_ | CountTokens |       Hello, World! |       642.2 ns |  0.19 |   0.2356 |        - |      - |    1480 B |        0.35 |
|                  Tiktoken_ | CountTokens |       Hello, World! |       326.3 ns |  0.10 |   0.0420 |        - |      - |     264 B |        0.06 |
|                            |             |                     |                |       |          |          |        |           |             |
|         **SharpTokenV1_0_28_** | **CountTokens** | **King(...)edy. [275]** |    **71,849.3 ns** |  **1.00** |   **8.5449** |   **0.3662** |      **-** |   **54160 B** |        **1.00** |
|       TiktokenSharpV1_0_5_ | CountTokens | King(...)edy. [275] |    21,938.0 ns |  0.31 |   5.0964 |   0.2136 |      - |   32096 B |        0.59 |
|        TokenizerLibV1_3_2_ | CountTokens | King(...)edy. [275] |     8,467.3 ns |  0.12 |   3.0823 |   0.1373 |      - |   19344 B |        0.36 |
|                  Tiktoken_ | CountTokens | King(...)edy. [275] |     4,616.9 ns |  0.06 |   0.6409 |        - |      - |    4032 B |        0.07 |
|                            |             |                     |                |       |          |          |        |           |             |
|   **SharpTokenV1_0_28_Encode** |      **Encode** | **1. (...)57. [19866]** | **5,639,328.3 ns** |  **1.00** | **601.5625** | **296.8750** |      **-** | **3805771 B** |        **1.00** |
| TiktokenSharpV1_0_5_Encode |      Encode | 1. (...)57. [19866] | 1,658,228.7 ns |  0.29 | 251.9531 | 126.9531 | 1.9531 | 1571157 B |        0.41 |
|  TokenizerLibV1_3_2_Encode |      Encode | 1. (...)57. [19866] |   926,416.4 ns |  0.16 | 248.0469 | 124.0234 | 1.9531 | 1547678 B |        0.41 |
|            Tiktoken_Encode |      Encode | 1. (...)57. [19866] |   430,360.1 ns |  0.08 |  59.5703 |  29.7852 |      - |  375665 B |        0.10 |
|                            |             |                     |                |       |          |          |        |           |             |
|   **SharpTokenV1_0_28_Encode** |      **Encode** |       **Hello, World!** |     **3,417.4 ns** |  **1.00** |   **0.6752** |   **0.0038** |      **-** |    **4240 B** |        **1.00** |
| TiktokenSharpV1_0_5_Encode |      Encode |       Hello, World! |     6,804.5 ns |  1.99 |   2.1820 |   0.0458 |      - |   13728 B |        3.24 |
|  TokenizerLibV1_3_2_Encode |      Encode |       Hello, World! |       640.1 ns |  0.19 |   0.2356 |   0.0010 |      - |    1480 B |        0.35 |
|            Tiktoken_Encode |      Encode |       Hello, World! |       479.1 ns |  0.14 |   0.1135 |        - |      - |     712 B |        0.17 |
|                            |             |                     |                |       |          |          |        |           |             |
|   **SharpTokenV1_0_28_Encode** |      **Encode** | **King(...)edy. [275]** |    **62,696.5 ns** |  **1.00** |   **8.5449** |   **0.4883** |      **-** |   **54160 B** |        **1.00** |
| TiktokenSharpV1_0_5_Encode |      Encode | King(...)edy. [275] |    27,168.7 ns |  0.43 |   5.0964 |   0.3052 |      - |   32096 B |        0.59 |
|  TokenizerLibV1_3_2_Encode |      Encode | King(...)edy. [275] |    12,317.4 ns |  0.20 |   3.0823 |   0.1831 |      - |   19344 B |        0.36 |
|            Tiktoken_Encode |      Encode | King(...)edy. [275] |     4,933.3 ns |  0.08 |   0.8011 |   0.0153 |      - |    5056 B |        0.09 |

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