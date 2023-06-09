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
  Job-KSEHQD : .NET 7.0.5 (7.0.523.17405), Arm64 RyuJIT AdvSIMD DEBUG

BuildConfiguration=Debug  

```
|              Method |                Data |         Mean |       Error |      StdDev | Ratio |     Gen0 |     Gen1 |   Gen2 |  Allocated | Alloc Ratio |
|-------------------- |-------------------- |-------------:|------------:|------------:|------:|---------:|---------:|-------:|-----------:|------------:|
|   **SharpTokenV1_0_28** | **1. (...)57. [19866]** | **5,342.292 μs** | **104.5883 μs** | **124.5049 μs** |  **1.00** | **601.5625** | **296.8750** |      **-** | **3716.57 KB** |        **1.00** |
| TiktokenSharpV1_0_5 | 1. (...)57. [19866] | 1,674.700 μs |  11.4495 μs |  10.7099 μs |  0.31 | 253.9063 | 128.9063 | 3.9063 | 1534.33 KB |        0.41 |
|            Tiktoken | 1. (...)57. [19866] |   905.094 μs |   9.7406 μs |   9.1113 μs |  0.17 |  86.9141 |  28.3203 |      - |  535.65 KB |        0.14 |
|                     |                     |              |             |             |       |          |          |        |            |             |
|   **SharpTokenV1_0_28** |       **Hello, World!** |     **3.235 μs** |   **0.0069 μs** |   **0.0064 μs** |  **1.00** |   **0.6752** |   **0.0038** |      **-** |    **4.14 KB** |        **1.00** |
| TiktokenSharpV1_0_5 |       Hello, World! |     6.670 μs |   0.0213 μs |   0.0200 μs |  2.06 |   2.1820 |   0.0458 |      - |   13.41 KB |        3.24 |
|            Tiktoken |       Hello, World! |     6.603 μs |   0.0107 μs |   0.0083 μs |  2.04 |   2.0294 |   0.0458 |      - |   12.45 KB |        3.01 |
|                     |                     |              |             |             |       |          |          |        |            |             |
|   **SharpTokenV1_0_28** | **King(...)edy. [275]** |    **63.158 μs** |   **0.1723 μs** |   **0.1612 μs** |  **1.00** |   **8.5449** |   **0.4883** |      **-** |   **52.89 KB** |        **1.00** |
| TiktokenSharpV1_0_5 | King(...)edy. [275] |    22.863 μs |   0.1151 μs |   0.1076 μs |  0.36 |   5.0964 |   0.3052 |      - |   31.34 KB |        0.59 |
|            Tiktoken | King(...)edy. [275] |    19.184 μs |   0.0615 μs |   0.0545 μs |  0.30 |   3.0823 |   0.0916 |      - |   18.88 KB |        0.36 |

<!--BENCHMARKS_END-->
