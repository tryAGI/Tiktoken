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

BenchmarkDotNet v0.13.7, macOS Ventura 13.4.1 (c) (22F770820d) [Darwin 22.5.0]
Apple M1 Pro, 1 CPU, 10 logical and 10 physical cores
.NET SDK 7.0.304
  [Host]     : .NET 7.0.7 (7.0.723.27404), Arm64 RyuJIT AdvSIMD
  DefaultJob : .NET 7.0.7 (7.0.723.27404), Arm64 RyuJIT AdvSIMD


```
|                     Method |  Categories |                Data |           Mean | Ratio |     Gen0 |     Gen1 | Allocated | Alloc Ratio |
|--------------------------- |------------ |-------------------- |---------------:|------:|---------:|---------:|----------:|------------:|
|          **SharpTokenV1_2_6_** | **CountTokens** | **1. (...)57. [19866]** | **1,472,977.1 ns** |  **1.00** | **292.9688** | **146.4844** | **1846187 B** |        **1.00** |
|       TiktokenSharpV1_0_6_ | CountTokens | 1. (...)57. [19866] | 1,009,957.3 ns |  0.70 | 250.0000 | 125.0000 | 1571155 B |        0.85 |
|        TokenizerLibV1_3_2_ | CountTokens | 1. (...)57. [19866] |   964,021.9 ns |  0.68 | 246.0938 |  87.8906 | 1547674 B |        0.84 |
|                  Tiktoken_ | CountTokens | 1. (...)57. [19866] |   425,937.3 ns |  0.30 |  49.3164 |        - |  309449 B |        0.17 |
|                            |             |                     |                |       |          |          |           |             |
|          **SharpTokenV1_2_6_** | **CountTokens** |       **Hello, World!** |     **1,803.4 ns** |  **1.00** |   **0.5264** |        **-** |    **3304 B** |        **1.00** |
|       TiktokenSharpV1_0_6_ | CountTokens |       Hello, World! |     9,094.3 ns |  5.04 |   2.1820 |   0.0305 |   13728 B |        4.15 |
|        TokenizerLibV1_3_2_ | CountTokens |       Hello, World! |       967.6 ns |  0.54 |   0.2346 |        - |    1480 B |        0.45 |
|                  Tiktoken_ | CountTokens |       Hello, World! |       252.1 ns |  0.14 |   0.0420 |        - |     264 B |        0.08 |
|                            |             |                     |                |       |          |          |           |             |
|          **SharpTokenV1_2_6_** | **CountTokens** | **King(...)edy. [275]** |    **15,398.3 ns** |  **1.00** |   **4.1199** |   **0.1526** |   **26008 B** |        **1.00** |
|       TiktokenSharpV1_0_6_ | CountTokens | King(...)edy. [275] |    20,492.8 ns |  1.32 |   5.0964 |   0.1526 |   32096 B |        1.23 |
|        TokenizerLibV1_3_2_ | CountTokens | King(...)edy. [275] |    13,199.9 ns |  0.86 |   3.0823 |   0.1373 |   19344 B |        0.74 |
|                  Tiktoken_ | CountTokens | King(...)edy. [275] |     3,860.3 ns |  0.25 |   0.6409 |        - |    4032 B |        0.16 |
|                            |             |                     |                |       |          |          |           |             |
|    **SharpTokenV1_2_6_Encode** |      **Encode** | **1. (...)57. [19866]** | **1,438,502.2 ns** |  **1.00** | **292.9688** | **146.4844** | **1846187 B** |        **1.00** |
| TiktokenSharpV1_0_6_Encode |      Encode | 1. (...)57. [19866] |   995,770.4 ns |  0.67 | 250.0000 | 125.0000 | 1571155 B |        0.85 |
|  TokenizerLibV1_3_2_Encode |      Encode | 1. (...)57. [19866] |   850,449.9 ns |  0.58 | 246.0938 |  83.9844 | 1547673 B |        0.84 |
|            Tiktoken_Encode |      Encode | 1. (...)57. [19866] |   377,925.3 ns |  0.26 |  59.5703 |   2.4414 |  375665 B |        0.20 |
|                            |             |                     |                |       |          |          |           |             |
|    **SharpTokenV1_2_6_Encode** |      **Encode** |       **Hello, World!** |     **2,226.1 ns** |  **1.00** |   **0.5264** |        **-** |    **3304 B** |        **1.00** |
| TiktokenSharpV1_0_6_Encode |      Encode |       Hello, World! |     6,259.3 ns |  2.81 |   2.1820 |   0.0305 |   13728 B |        4.15 |
|  TokenizerLibV1_3_2_Encode |      Encode |       Hello, World! |       724.4 ns |  0.33 |   0.2356 |        - |    1480 B |        0.45 |
|            Tiktoken_Encode |      Encode |       Hello, World! |       335.0 ns |  0.15 |   0.1135 |        - |     712 B |        0.22 |
|                            |             |                     |                |       |          |          |           |             |
|    **SharpTokenV1_2_6_Encode** |      **Encode** | **King(...)edy. [275]** |    **20,605.4 ns** |  **1.00** |   **4.1199** |   **0.1526** |   **26008 B** |        **1.00** |
| TiktokenSharpV1_0_6_Encode |      Encode | King(...)edy. [275] |    14,828.8 ns |  0.72 |   5.1117 |   0.1678 |   32096 B |        1.23 |
|  TokenizerLibV1_3_2_Encode |      Encode | King(...)edy. [275] |     8,924.2 ns |  0.43 |   3.0823 |   0.1373 |   19344 B |        0.74 |
|            Tiktoken_Encode |      Encode | King(...)edy. [275] |     4,820.3 ns |  0.23 |   0.8011 |        - |    5056 B |        0.19 |

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