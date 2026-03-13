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

# Run benchmarks
dotnet run -c Release --project src/benchmarks/Tiktoken.Benchmarks/Tiktoken.Benchmarks.csproj
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
| `src/tests/Tiktoken.UnitTests/` | Unit tests (MSTest + FluentAssertions + Verify) |
| `src/benchmarks/Tiktoken.Benchmarks/` | BenchmarkDotNet performance benchmarks |
| `benchmarks/` | Historical benchmark result reports (Markdown) |

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

- **Target frameworks:** `net4.6.2`, `netstandard2.0`, `netstandard2.1`, `net8.0`, `net9.0`
- **Language:** C# with nullable reference types
- **Unsafe code:** Enabled in Core for performance
- **Encoding data:** Embedded as `.tiktoken` resources in `Tiktoken.Core/Encodings/`
- **Versioning:** Semantic versioning from git tags via MinVer
- **Testing:** MSTest + FluentAssertions + Verify

### CI/CD

- Uses shared workflows from `HavenDV/workflows` repo
- Dependabot updates NuGet packages
