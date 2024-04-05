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

var encoding = Tiktoken.Encoding.Get(Encodings.P50KBase);
var tokens = encoding.Encode("hello world"); // [31373, 995]
var text = encoding.Decode(tokens); // hello world
```

### Benchmarks
You can view the reports for each version [here](benchmarks)

<!--BENCHMARKS_START-->
```

BenchmarkDotNet v0.13.12, macOS Sonoma 14.4.1 (23E224) [Darwin 23.4.0]
Apple M1 Pro, 1 CPU, 10 logical and 10 physical cores
.NET SDK 8.0.203
  [Host]     : .NET 8.0.3 (8.0.324.11423), Arm64 RyuJIT AdvSIMD
  DefaultJob : .NET 8.0.3 (8.0.324.11423), Arm64 RyuJIT AdvSIMD


```
| Method                     | Categories  | Data                | Mean           | Median         | Ratio | Gen0     | Gen1     | Allocated | Alloc Ratio |
|--------------------------- |------------ |-------------------- |---------------:|---------------:|------:|---------:|---------:|----------:|------------:|
| **SharpTokenV2_0_1_**          | **CountTokens** | **1. (...)57. [19866]** |   **659,050.8 ns** |   **663,888.3 ns** |  **1.00** |   **2.9297** |   **0.9766** |   **20116 B** |        **1.00** |
| TiktokenSharpV1_0_9_       | CountTokens | 1. (...)57. [19866] |   951,380.1 ns |   939,690.6 ns |  1.45 | 250.0000 | 125.0000 | 1570772 B |       78.09 |
| TokenizerLibV1_3_3_        | CountTokens | 1. (...)57. [19866] | 1,049,794.0 ns | 1,032,725.9 ns |  1.61 | 246.0938 |  89.8438 | 1547675 B |       76.94 |
| Tiktoken_                  | CountTokens | 1. (...)57. [19866] |   325,631.7 ns |   324,920.4 ns |  0.49 |  49.3164 |        - |  309449 B |       15.38 |
|                            |             |                     |                |                |       |          |          |           |             |
| **SharpTokenV2_0_1_**          | **CountTokens** | **Hello, World!**       |       **431.0 ns** |       **430.5 ns** |  **1.00** |   **0.0405** |        **-** |     **256 B** |        **1.00** |
| TiktokenSharpV1_0_9_       | CountTokens | Hello, World!       |     5,826.4 ns |     5,826.7 ns | 13.52 |   2.1210 |   0.0305 |   13344 B |       52.12 |
| TokenizerLibV1_3_3_        | CountTokens | Hello, World!       |       774.3 ns |       771.0 ns |  1.80 |   0.2356 |        - |    1480 B |        5.78 |
| Tiktoken_                  | CountTokens | Hello, World!       |       214.2 ns |       212.9 ns |  0.50 |   0.0420 |        - |     264 B |        1.03 |
|                            |             |                     |                |                |       |          |          |           |             |
| **SharpTokenV2_0_1_**          | **CountTokens** | **King(...)edy. [275]** |     **6,643.3 ns** |     **6,645.0 ns** |  **1.00** |   **0.0763** |        **-** |     **520 B** |        **1.00** |
| TiktokenSharpV1_0_9_       | CountTokens | King(...)edy. [275] |    13,319.5 ns |    13,318.8 ns |  2.00 |   5.0507 |   0.1678 |   31712 B |       60.98 |
| TokenizerLibV1_3_3_        | CountTokens | King(...)edy. [275] |     7,342.0 ns |     7,349.4 ns |  1.10 |   3.0823 |   0.1373 |   19344 B |       37.20 |
| Tiktoken_                  | CountTokens | King(...)edy. [275] |     3,306.1 ns |     3,289.0 ns |  0.50 |   0.6447 |        - |    4064 B |        7.82 |
|                            |             |                     |                |                |       |          |          |           |             |
| **SharpTokenV2_0_1_Encode**    | **Encode**      | **1. (...)57. [19866]** |   **616,768.0 ns** |   **615,247.0 ns** |  **1.00** |   **2.9297** |        **-** |   **20115 B** |        **1.00** |
| TiktokenSharpV1_0_9_Encode | Encode      | 1. (...)57. [19866] |   929,080.6 ns |   926,978.2 ns |  1.51 | 250.0000 | 125.0000 | 1570770 B |       78.09 |
| TokenizerLibV1_3_3_Encode  | Encode      | 1. (...)57. [19866] |   793,069.4 ns |   791,800.6 ns |  1.29 | 246.0938 |  85.9375 | 1547673 B |       76.94 |
| Tiktoken_Encode            | Encode      | 1. (...)57. [19866] |   340,412.3 ns |   339,821.0 ns |  0.55 |  59.5703 |   2.4414 |  375601 B |       18.67 |
|                            |             |                     |                |                |       |          |          |           |             |
| **SharpTokenV2_0_1_Encode**    | **Encode**      | **Hello, World!**       |       **443.7 ns** |       **443.7 ns** |  **1.00** |   **0.0405** |        **-** |     **256 B** |        **1.00** |
| TiktokenSharpV1_0_9_Encode | Encode      | Hello, World!       |     5,783.7 ns |     5,778.7 ns | 13.04 |   2.1210 |   0.0305 |   13344 B |       52.12 |
| TokenizerLibV1_3_3_Encode  | Encode      | Hello, World!       |       491.2 ns |       491.0 ns |  1.11 |   0.2356 |        - |    1480 B |        5.78 |
| Tiktoken_Encode            | Encode      | Hello, World!       |       264.8 ns |       264.3 ns |  0.60 |   0.1030 |        - |     648 B |        2.53 |
|                            |             |                     |                |                |       |          |          |           |             |
| **SharpTokenV2_0_1_Encode**    | **Encode**      | **King(...)edy. [275]** |     **6,620.0 ns** |     **6,618.1 ns** |  **1.00** |   **0.0763** |        **-** |     **520 B** |        **1.00** |
| TiktokenSharpV1_0_9_Encode | Encode      | King(...)edy. [275] |    13,205.7 ns |    13,217.4 ns |  1.99 |   5.0507 |   0.1678 |   31712 B |       60.98 |
| TokenizerLibV1_3_3_Encode  | Encode      | King(...)edy. [275] |     7,312.6 ns |     7,307.4 ns |  1.10 |   3.0823 |   0.1373 |   19344 B |       37.20 |
| Tiktoken_Encode            | Encode      | King(...)edy. [275] |     3,599.7 ns |     3,596.8 ns |  0.54 |   0.7973 |        - |    5024 B |        9.66 |

<!--BENCHMARKS_END-->

## Support

Priority place for bugs: https://github.com/tryAGI/LangChain/issues  
Priority place for ideas and general questions: https://github.com/tryAGI/LangChain/discussions  
Discord: https://discord.gg/Ca2xhfBf3v  