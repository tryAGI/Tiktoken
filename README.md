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
  Job-POLNET : .NET 7.0.5 (7.0.523.17405), Arm64 RyuJIT AdvSIMD DEBUG

BuildConfiguration=Debug  

```
|              Method |                Data |      Mean |     Error |    StdDev | Ratio | RatioSD |   Gen0 |   Gen1 | Allocated | Alloc Ratio |
|-------------------- |-------------------- |----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
|   **SharpTokenV1_0_28** |       **Hello, World!** |  **3.347 μs** | **0.0649 μs** | **0.0607 μs** |  **1.00** |    **0.00** | **0.6752** | **0.0038** |   **4.14 KB** |        **1.00** |
| TiktokenSharpV1_0_5 |       Hello, World! |  6.721 μs | 0.0887 μs | 0.0741 μs |  2.00 |    0.05 | 2.1820 | 0.0458 |  13.41 KB |        3.24 |
|            Tiktoken |       Hello, World! |  6.406 μs | 0.0224 μs | 0.0187 μs |  1.91 |    0.03 | 2.1820 | 0.0458 |  13.41 KB |        3.24 |
|                     |                     |           |           |           |       |         |        |        |           |             |
|   **SharpTokenV1_0_28** | **King(...)edy. [275]** | **63.220 μs** | **0.3281 μs** | **0.3069 μs** |  **1.00** |    **0.00** | **8.5449** | **0.4883** |  **52.89 KB** |        **1.00** |
| TiktokenSharpV1_0_5 | King(...)edy. [275] | 22.215 μs | 0.4191 μs | 0.3920 μs |  0.35 |    0.01 | 5.0964 | 0.3052 |  31.34 KB |        0.59 |
|            Tiktoken | King(...)edy. [275] | 20.263 μs | 0.1266 μs | 0.1122 μs |  0.32 |    0.00 | 5.7678 | 0.3357 |  35.38 KB |        0.67 |

<!--BENCHMARKS_END-->
