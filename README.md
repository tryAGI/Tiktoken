# Tiktoken

[![Nuget package](https://img.shields.io/nuget/vpre/Tiktoken)](https://www.nuget.org/packages/Tiktoken/)
[![dotnet](https://github.com/tryAGI/Tiktoken/actions/workflows/dotnet.yml/badge.svg?branch=main)](https://github.com/tryAGI/Tiktoken/actions/workflows/dotnet.yml)
[![License: MIT](https://img.shields.io/github/license/tryAGI/Tiktoken)](https://github.com/tryAGI/Tiktoken/blob/main/LICENSE.txt)
[![Discord](https://img.shields.io/discord/1115206893015662663?label=Discord&logo=discord&logoColor=white&color=d82679)](https://discord.gg/Ca2xhfBf3v)

Due to the lack of a C# version of `cl100k_base` encoding (gpt-3.5-turbo), I have implemented a basic solution with encoding and decoding methods based on the official Rust implementation.

Currently, `cl100k_base` `p50k_base` has been implemented. Other encodings will be added in future submissions. If you encounter any issues or have questions, please feel free to submit them on the `lssues`."

### Usage
```csharp
using Tiktoken;

var encoding = Encoding.ForModel("gpt-3.5-turbo");
var tokens = encoding.Encode("hello world"); // [15339, 1917]
var text = encoding.Decode(tokens); // hello world

var encoding = Encoding.Get("cl100k_base");
var tokens = encoding.Encode("hello world"); // [15339, 1917]
var text = encoding.Decode(tokens); // hello world
```

**When using a new encoder for the first time, the required tiktoken files for the encoder will be downloaded from the internet. This may take some time.** Once the download is successful, subsequent uses will not require downloading again. You can set `TikToken.PBEFileDirectory` before using the encoder to modify the storage path of the downloaded tiktoken files, or you can pre-download the files to avoid network issues causing download failures.

**Why are the tiktoken files not integrated into the package?** On one hand, this would make the package size larger. On the other hand, I want to stay as consistent as possible with OpenAI's official Python code.

Below are the file download links:
[p50k_base.tiktoken](https://openaipublic.blob.core.windows.net/encodings/p50k_base.tiktoken)
[cl100k_base.tiktoken](https://openaipublic.blob.core.windows.net/encodings/cl100k_base.tiktoken)

## Benchmarks
You can view the reports for each version [here](benchmarks)

<!--BENCHMARKS_START-->
``` ini

BenchmarkDotNet=v0.13.5, OS=macOS Ventura 13.3.1 (a) (22E772610a) [Darwin 22.4.0]
Apple M1 Pro, 1 CPU, 10 logical and 10 physical cores
.NET SDK=7.0.203
  [Host]     : .NET 7.0.5 (7.0.523.17405), Arm64 RyuJIT AdvSIMD
  Job-YQCEPS : .NET 7.0.5 (7.0.523.17405), Arm64 RyuJIT AdvSIMD DEBUG

BuildConfiguration=Debug  

```
|              Method |                Data |      Mean |     Error |    StdDev | Ratio | RatioSD |   Gen0 |   Gen1 | Allocated | Alloc Ratio |
|-------------------- |-------------------- |----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
|   **SharpTokenV1_0_28** |       **Hello, World!** |  **3.335 μs** | **0.0589 μs** | **0.0522 μs** |  **1.00** |    **0.00** | **0.6752** | **0.0038** |   **4.14 KB** |        **1.00** |
| TiktokenSharpV1_0_5 |       Hello, World! |  6.733 μs | 0.0506 μs | 0.0449 μs |  2.02 |    0.04 | 2.1820 | 0.0458 |  13.41 KB |        3.24 |
|            Tiktoken |       Hello, World! |  6.742 μs | 0.0410 μs | 0.0364 μs |  2.02 |    0.03 | 2.1820 | 0.0458 |  13.41 KB |        3.24 |
|                     |                     |           |           |           |       |         |        |        |           |             |
|   **SharpTokenV1_0_28** | **King(...)edy. [275]** | **63.037 μs** | **0.4089 μs** | **0.3624 μs** |  **1.00** |    **0.00** | **8.5449** | **0.4883** |  **52.89 KB** |        **1.00** |
| TiktokenSharpV1_0_5 | King(...)edy. [275] | 21.937 μs | 0.0518 μs | 0.0404 μs |  0.35 |    0.00 | 5.0964 | 0.3052 |  31.34 KB |        0.59 |
|            Tiktoken | King(...)edy. [275] | 23.910 μs | 0.1035 μs | 0.0808 μs |  0.38 |    0.00 | 5.7678 | 0.3357 |  35.38 KB |        0.67 |

<!--BENCHMARKS_END-->
