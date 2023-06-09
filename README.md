# Tiktoken

[![Nuget package](https://img.shields.io/nuget/vpre/Tiktoken)](https://www.nuget.org/packages/Tiktoken/)
[![dotnet](https://github.com/tryAGI/Tiktoken/actions/workflows/dotnet.yml/badge.svg?branch=main)](https://github.com/tryAGI/Tiktoken/actions/workflows/dotnet.yml)
[![License: MIT](https://img.shields.io/github/license/tryAGI/Tiktoken)](https://github.com/tryAGI/Tiktoken/blob/main/LICENSE.txt)
[![Discord](https://img.shields.io/discord/1115206893015662663?label=Discord&logo=discord&logoColor=white&color=d82679)](https://discord.gg/Ca2xhfBf3v)

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

## Benchmarks
You can view the reports for each version [here](benchmarks)

<!--BENCHMARKS_START-->
``` ini

BenchmarkDotNet=v0.13.5, OS=macOS Ventura 13.3.1 (a) (22E772610a) [Darwin 22.4.0]
Apple M1 Pro, 1 CPU, 10 logical and 10 physical cores
.NET SDK=7.0.203
  [Host]     : .NET 7.0.5 (7.0.523.17405), Arm64 RyuJIT AdvSIMD
  Job-TGQYUW : .NET 7.0.5 (7.0.523.17405), Arm64 RyuJIT AdvSIMD DEBUG

BuildConfiguration=Debug  

```
|              Method |                Data |         Mean |      Error |     StdDev | Ratio |     Gen0 |     Gen1 |   Gen2 |  Allocated | Alloc Ratio |
|-------------------- |-------------------- |-------------:|-----------:|-----------:|------:|---------:|---------:|-------:|-----------:|------------:|
|   **SharpTokenV1_0_28** | **1. (...)57. [19866]** | **5,316.826 μs** | **55.8088 μs** | **52.2036 μs** |  **1.00** | **601.5625** | **296.8750** |      **-** | **3716.57 KB** |        **1.00** |
| TiktokenSharpV1_0_5 | 1. (...)57. [19866] | 1,677.066 μs | 11.4936 μs | 10.1888 μs |  0.32 | 253.9063 | 128.9063 | 3.9063 | 1534.33 KB |        0.41 |
|            Tiktoken | 1. (...)57. [19866] |   894.654 μs |  5.6507 μs |  5.2857 μs |  0.17 |  84.9609 |  29.2969 |      - |  525.11 KB |        0.14 |
|                     |                     |              |            |            |       |          |          |        |            |             |
|   **SharpTokenV1_0_28** |       **Hello, World!** |     **3.269 μs** |  **0.0157 μs** |  **0.0131 μs** |  **1.00** |   **0.6752** |   **0.0038** |      **-** |    **4.14 KB** |        **1.00** |
| TiktokenSharpV1_0_5 |       Hello, World! |     6.651 μs |  0.0364 μs |  0.0322 μs |  2.03 |   2.1820 |   0.0458 |      - |   13.41 KB |        3.24 |
|            Tiktoken |       Hello, World! |     1.230 μs |  0.0040 μs |  0.0034 μs |  0.38 |   0.3109 |   0.0019 |      - |    1.91 KB |        0.46 |
|                     |                     |              |            |            |       |          |          |        |            |             |
|   **SharpTokenV1_0_28** | **King(...)edy. [275]** |    **62.255 μs** |  **0.1989 μs** |  **0.1861 μs** |  **1.00** |   **8.5449** |   **0.4883** |      **-** |   **52.89 KB** |        **1.00** |
| TiktokenSharpV1_0_5 | King(...)edy. [275] |    21.834 μs |  0.0833 μs |  0.0739 μs |  0.35 |   5.0964 |   0.3052 |      - |   31.34 KB |        0.59 |
|            Tiktoken | King(...)edy. [275] |    11.433 μs |  0.0265 μs |  0.0235 μs |  0.18 |   1.3580 |   0.0305 |      - |    8.34 KB |        0.16 |

<!--BENCHMARKS_END-->
