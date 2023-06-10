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
  Job-XLSUNQ : .NET 7.0.5 (7.0.523.17405), Arm64 RyuJIT AdvSIMD DEBUG

BuildConfiguration=Debug  

```
|              Method |                Data |           Mean |        Error |       StdDev | Ratio |     Gen0 |     Gen1 |   Gen2 | Allocated | Alloc Ratio |
|-------------------- |-------------------- |---------------:|-------------:|-------------:|------:|---------:|---------:|-------:|----------:|------------:|
|   **SharpTokenV1_0_28** | **1. (...)57. [19866]** | **5,301,619.9 ns** | **93,313.71 ns** | **72,853.21 ns** |  **1.00** | **601.5625** | **296.8750** |      **-** | **3805771 B** |        **1.00** |
| TiktokenSharpV1_0_5 | 1. (...)57. [19866] | 1,674,605.4 ns | 14,542.22 ns | 13,602.80 ns |  0.32 | 253.9063 | 128.9063 | 3.9063 | 1571158 B |        0.41 |
|            Tiktoken | 1. (...)57. [19866] |   887,726.7 ns |  3,753.84 ns |  3,134.62 ns |  0.17 |  80.0781 |  32.2266 |      - |  506425 B |        0.13 |
|                     |                     |                |              |              |       |          |          |        |           |             |
|   **SharpTokenV1_0_28** |       **Hello, World!** |     **3,286.1 ns** |      **8.25 ns** |      **7.72 ns** |  **1.00** |   **0.6752** |   **0.0038** |      **-** |    **4240 B** |        **1.00** |
| TiktokenSharpV1_0_5 |       Hello, World! |     6,729.4 ns |     30.61 ns |     23.90 ns |  2.05 |   2.1820 |   0.0458 |      - |   13728 B |        3.24 |
|            Tiktoken |       Hello, World! |       547.5 ns |      5.29 ns |      4.42 ns |  0.17 |   0.1144 |        - |      - |     720 B |        0.17 |
|                     |                     |                |              |              |       |          |          |        |           |             |
|   **SharpTokenV1_0_28** | **King(...)edy. [275]** |    **62,614.2 ns** |    **500.66 ns** |    **443.82 ns** |  **1.00** |   **8.5449** |   **0.4883** |      **-** |   **54160 B** |        **1.00** |
| TiktokenSharpV1_0_5 | King(...)edy. [275] |    21,797.8 ns |     53.99 ns |     50.50 ns |  0.35 |   5.0964 |   0.3052 |      - |   32096 B |        0.59 |
|            Tiktoken | King(...)edy. [275] |    10,511.8 ns |     27.65 ns |     24.51 ns |  0.17 |   1.0986 |   0.0153 |      - |    6904 B |        0.13 |

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