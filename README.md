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

var encoding = Tiktoken.Encoding.Get("p50k_base");
var tokens = encoding.Encode("hello world"); // [31373, 995]
var text = encoding.Decode(tokens); // hello world
```

### Benchmarks
You can view the reports for each version [here](benchmarks)

<!--BENCHMARKS_START-->
``` ini

BenchmarkDotNet=v0.13.5, OS=macOS Ventura 13.3.1 (a) (22E772610a) [Darwin 22.4.0]
Apple M1 Pro, 1 CPU, 10 logical and 10 physical cores
.NET SDK=7.0.203
  [Host]     : .NET 7.0.5 (7.0.523.17405), Arm64 RyuJIT AdvSIMD
  Job-FQVSUN : .NET 7.0.5 (7.0.523.17405), Arm64 RyuJIT AdvSIMD DEBUG

BuildConfiguration=Debug  

```
|                     Method |  Categories |                Data |           Mean | Ratio |     Gen0 |     Gen1 |   Gen2 | Allocated | Alloc Ratio |
|--------------------------- |------------ |-------------------- |---------------:|------:|---------:|---------:|-------:|----------:|------------:|
|         **SharpTokenV1_0_28_** | **CountTokens** | **1. (...)57. [19866]** | **5,274,472.1 ns** |  **1.00** | **609.3750** | **273.4375** | **7.8125** | **3805787 B** |        **1.00** |
|       TiktokenSharpV1_0_5_ | CountTokens | 1. (...)57. [19866] | 1,523,632.2 ns |  0.29 | 250.0000 | 125.0000 |      - | 1571154 B |        0.41 |
|                  Tiktoken_ | CountTokens | 1. (...)57. [19866] |   734,470.2 ns |  0.14 |  61.5234 |        - |      - |  387017 B |        0.10 |
|                            |             |                     |                |       |          |          |        |           |             |
|         **SharpTokenV1_0_28_** | **CountTokens** |       **Hello, World!** |     **3,243.4 ns** |  **1.00** |   **0.6752** |        **-** |      **-** |    **4240 B** |        **1.00** |
|       TiktokenSharpV1_0_5_ | CountTokens |       Hello, World! |     6,620.5 ns |  2.04 |   2.1820 |   0.0381 |      - |   13728 B |        3.24 |
|                  Tiktoken_ | CountTokens |       Hello, World! |       435.8 ns |  0.13 |   0.0429 |        - |      - |     272 B |        0.06 |
|                            |             |                     |                |       |          |          |        |           |             |
|         **SharpTokenV1_0_28_** | **CountTokens** | **King(...)edy. [275]** |    **62,185.4 ns** |  **1.00** |   **8.5449** |   **0.3662** |      **-** |   **54160 B** |        **1.00** |
|       TiktokenSharpV1_0_5_ | CountTokens | King(...)edy. [275] |    21,633.9 ns |  0.35 |   5.0964 |   0.2136 |      - |   32096 B |        0.59 |
|                  Tiktoken_ | CountTokens | King(...)edy. [275] |     8,570.0 ns |  0.14 |   0.8087 |        - |      - |    5128 B |        0.09 |
|                            |             |                     |                |       |          |          |        |           |             |
|   **SharpTokenV1_0_28_Encode** |      **Encode** | **1. (...)57. [19866]** | **5,001,057.0 ns** |  **1.00** | **601.5625** | **296.8750** |      **-** | **3805769 B** |        **1.00** |
| TiktokenSharpV1_0_5_Encode |      Encode | 1. (...)57. [19866] | 1,657,914.8 ns |  0.33 | 253.9063 | 128.9063 | 3.9063 | 1571158 B |        0.41 |
|            Tiktoken_Encode |      Encode | 1. (...)57. [19866] |   769,015.9 ns |  0.15 |  75.1953 |  37.1094 |      - |  473625 B |        0.12 |
|                            |             |                     |                |       |          |          |        |           |             |
|   **SharpTokenV1_0_28_Encode** |      **Encode** |       **Hello, World!** |     **3,258.5 ns** |  **1.00** |   **0.6752** |   **0.0038** |      **-** |    **4240 B** |        **1.00** |
| TiktokenSharpV1_0_5_Encode |      Encode |       Hello, World! |     6,670.4 ns |  2.05 |   2.1820 |   0.0458 |      - |   13728 B |        3.24 |
|            Tiktoken_Encode |      Encode |       Hello, World! |       536.6 ns |  0.16 |   0.1144 |        - |      - |     720 B |        0.17 |
|                            |             |                     |                |       |          |          |        |           |             |
|   **SharpTokenV1_0_28_Encode** |      **Encode** | **King(...)edy. [275]** |    **62,531.4 ns** |  **1.00** |   **8.5449** |   **0.4883** |      **-** |   **54160 B** |        **1.00** |
| TiktokenSharpV1_0_5_Encode |      Encode | King(...)edy. [275] |    26,823.9 ns |  0.43 |   5.0964 |   0.3052 |      - |   32096 B |        0.59 |
|            Tiktoken_Encode |      Encode | King(...)edy. [275] |     9,253.4 ns |  0.15 |   1.0223 |   0.0153 |      - |    6448 B |        0.12 |

<!--BENCHMARKS_END-->

### Possible optimizations
- stackalloc in BytePairEncode
- BytePairEncode caching
- Modes - Fast(without special token regex)/Strict
- SIMD?
- Parallelism?
- string as dictionary key?

## Support

Priority place for bugs: https://github.com/tryAGI/LangChain/issues  
Priority place for ideas and general questions: https://github.com/tryAGI/LangChain/discussions  
Discord: https://discord.gg/Ca2xhfBf3v  