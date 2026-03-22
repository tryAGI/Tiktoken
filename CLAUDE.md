# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

High-performance .NET port of OpenAI's [tiktoken](https://github.com/openai/tiktoken) tokenizer, optimized for token counting speed. Published as [Tiktoken](https://www.nuget.org/packages/Tiktoken/) on NuGet.

## Build Commands

```bash
# Build the solution
dotnet build Tiktoken.slnx

# Build for release
dotnet build Tiktoken.slnx -c Release

# Run unit tests
dotnet test src/tests/Tiktoken.UnitTests/Tiktoken.UnitTests.csproj

# Run all tests
dotnet test Tiktoken.slnx

# Run benchmarks (pick the project matching your concern)
dotnet run -c Release --project src/benchmarks/Tiktoken.Benchmarks.Construction/Tiktoken.Benchmarks.Construction.csproj
dotnet run -c Release --project src/benchmarks/Tiktoken.Benchmarks.Encode/Tiktoken.Benchmarks.Encode.csproj
dotnet run -c Release --project src/benchmarks/Tiktoken.Benchmarks.Decode/Tiktoken.Benchmarks.Decode.csproj
dotnet run -c Release --project src/benchmarks/Tiktoken.Benchmarks.CountTokens/Tiktoken.Benchmarks.CountTokens.csproj
dotnet run -c Release --project src/benchmarks/Tiktoken.Benchmarks.EncodingComparison/Tiktoken.Benchmarks.EncodingComparison.csproj
dotnet run -c Release --project src/benchmarks/Tiktoken.Benchmarks.Explore/Tiktoken.Benchmarks.Explore.csproj
dotnet run -c Release --project src/benchmarks/Tiktoken.Benchmarks.ColdPath/Tiktoken.Benchmarks.ColdPath.csproj

# Quick perf smoke test (checks cache speedup ratios, ~30s, machine-independent)
dotnet run -c Release --project src/benchmarks/Tiktoken.Benchmarks.SmokeTest/Tiktoken.Benchmarks.SmokeTest.csproj
```

## Architecture

### Project Layout

| Project | Purpose |
|---------|---------|
| `src/libs/Tiktoken/` | Main convenience library -- bundles Core + cl100k + o200k encodings |
| `src/libs/Tiktoken.Core/` | Core tokenizer engine (`Encoder`, `ModelToEncoder`, BPE logic) |
| `src/libs/Tiktoken.Encodings.Abstractions/` | Base types for encoding definitions |
| `src/libs/Tiktoken.Encodings.cl100k/` | `cl100k_base` encoding (GPT-3.5/GPT-4) |
| `src/libs/Tiktoken.Encodings.o200k/` | `o200k_base` encoding (GPT-4o) |
| `src/libs/Tiktoken.Encodings.p50k/` | `p50k_base` / `p50k_edit` encodings |
| `src/libs/Tiktoken.Encodings.r50k/` | `r50k_base` encoding |
| `src/libs/Tiktoken.Encodings.Tokenizer/` | Load HuggingFace `tokenizer.json` files (GPT-2, Llama 3, Qwen2, etc.) |
| `src/tests/Tiktoken.UnitTests/` | Unit tests (MSTest + AwesomeAssertions + Verify) |
| `src/benchmarks/Tiktoken.Benchmarks.*/` | BenchmarkDotNet benchmarks split by concern (Construction, Encode, Decode, CountTokens, ColdPath, EncodingComparison, Explore) |
| `src/benchmarks/Tiktoken.Benchmarks.Shared/` | Shared test strings (MSBuild shared project) |
| `benchmarks/` | Historical benchmark result reports (Markdown) |
| `data/` | Source-of-truth `.tiktoken` files from OpenAI + conversion/verification scripts |

### Supported Encodings

- `o200k_base` -- GPT-4o models
- `cl100k_base` -- GPT-3.5-turbo, GPT-4 models
- `r50k_base` -- older GPT-3 models
- `p50k_base` / `p50k_edit` -- Codex models

### Key API

```csharp
var encoder = ModelToEncoder.For("gpt-4o");
var tokens = encoder.Encode("hello world");       // [15339, 1917]
var text = encoder.Decode(tokens);                 // "hello world"
var count = encoder.CountTokens(text);             // 2
var parts = encoder.Explore(text);                 // ["hello", " world"]
```

### Build Configuration

- **Target frameworks:** `net4.6.2`, `netstandard2.0`, `netstandard2.1`, `net8.0`, `net9.0`, `net10.0`
- **Language:** C# with nullable reference types
- **Unsafe code:** Enabled in Core for performance
- **Encoding data:** Embedded as `.ttkb` binary resources in each `Tiktoken.Encodings.*` project (source `.tiktoken` text files in `data/`)
- **Versioning:** Semantic versioning from git tags via MinVer
- **Testing:** MSTest + AwesomeAssertions + Verify

### Encoding Data Pipeline

Source `.tiktoken` text files (from OpenAI) live in `data/`. Binary `.ttkb` files are mechanically derived and embedded in NuGet packages.

```bash
cd data && make    # Convert .tiktoken -> .ttkb, copy to encoding dirs, verify
```

See `data/README.md` for the binary format specification and provenance documentation.

### Custom Encoding API (`EncodingLoader`)

Key static methods in `Tiktoken.Encodings.EncodingLoader`:

| Method | Description |
|--------|-------------|
| `LoadEncodingFromFile(path)` | Load from file, auto-detects `.ttkb` vs `.tiktoken` by extension |
| `LoadEncodingFromFileAsync(path)` | Async variant with `CancellationToken` |
| `LoadEncodingFromBinaryData(byte[])` | Load from binary byte array |
| `LoadEncodingFromBinaryStream(stream)` | Load from binary stream |
| `LoadEncodingFromLines(lines, name)` | Load from text lines (extension method on `IReadOnlyList<string>`) |
| `WriteEncodingToBinaryStream(stream, dict)` | Write encoding dictionary to `.ttkb` binary format |

### CI/CD

- Uses shared workflows from `HavenDV/workflows` repo
- Dependabot updates NuGet packages
