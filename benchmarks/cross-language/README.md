# Cross-Language Tokenizer Benchmark

Reproducible benchmark comparing BPE tokenizer throughput across languages using **identical inputs** and the **o200k_base** encoding (GPT-4o).

## Inputs

All benchmarks use the same 6 inputs from `src/benchmarks/Tiktoken.Benchmarks.Shared/Strings.cs`:

| Input | UTF-8 Bytes | Description |
|-------|:-----------:|-------------|
| HelloWorld | 13 | ASCII greeting |
| Multilingual | 382 | 12 scripts + emoji |
| CjkHeavy | 1,676 | Chinese, Japanese, Korean, Devanagari, Thai, Arabic, Hebrew, Georgian |
| Code | 879 | Python binary search implementation |
| MultilingualLong | 4,312 | Extended pangrams in 11+ languages |
| Bitcoin | 19,884 | Bitcoin whitepaper (ASCII) |

The input texts are exported to `inputs/` as files for use by non-.NET benchmarks.

## Running

### .NET (tryAGI/Tiktoken)

```bash
# From repo root
dotnet run -c Release --project src/benchmarks/Tiktoken.Benchmarks.CountTokens/
dotnet run -c Release --project src/benchmarks/Tiktoken.Benchmarks.Encode/
```

### Python (OpenAI tiktoken)

```bash
pip install tiktoken
python bench_python.py
```

### Rust (tiktoken crate)

```bash
cd rust/
cargo bench
```

## Adding a new language

1. Read inputs from the `inputs/` directory (UTF-8 text files)
2. Use **o200k_base** encoding (GPT-4o)
3. Measure both `encode` (returns token IDs) and `count_tokens` (if available)
4. Report: input name, time (ns/us/ms), throughput (MiB/s), allocation (if measurable)
5. Run at least 1000 iterations, report median

## Results

Results will be collected in `results/` as markdown tables. See the main README's [Cross-language context](../../README.md#cross-language-context) section for the latest summary.
