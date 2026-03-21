# Rust tokenizers — Apple M4 Max

Encoding: o200k_base | Framework: Criterion.rs 0.5 | 100 samples per benchmark

## tiktoken crate v3 — `encode()` (returns token IDs)

| Input | Time | Throughput |
|-------|------|:----------:|
| hello_world (13 B) | 141.4 ns | 87.7 MiB/s |
| multilingual (382 B) | 8.93 µs | 40.8 MiB/s |
| cjk_heavy (1,676 B) | 47.79 µs | 33.4 MiB/s |
| code (879 B) | 9.60 µs | 87.3 MiB/s |
| multilingual_long (4,313 B) | 119.1 µs | 34.5 MiB/s |
| bitcoin (19,866 B) | 287.8 µs | 65.8 MiB/s |

## bpe-openai crate v0.3 (GitHub) — `count()` (token count only)

| Input | Time | Throughput |
|-------|------|:----------:|
| hello_world (13 B) | 425.5 ns | 29.1 MiB/s |
| multilingual (382 B) | 5.74 µs | 63.5 MiB/s |
| cjk_heavy (1,676 B) | 24.14 µs | 66.2 MiB/s |
| code (879 B) | 20.65 µs | 40.6 MiB/s |
| multilingual_long (4,313 B) | 76.98 µs | 53.4 MiB/s |
| bitcoin (19,866 B) | 396.5 µs | 47.8 MiB/s |

## bpe-openai crate v0.3 (GitHub) — `encode()` (returns token IDs)

| Input | Time | Throughput |
|-------|------|:----------:|
| hello_world (13 B) | 378.4 ns | 32.8 MiB/s |
| multilingual (382 B) | 8.67 µs | 42.0 MiB/s |
| cjk_heavy (1,676 B) | 25.16 µs | 63.5 MiB/s |
| code (879 B) | 21.26 µs | 39.4 MiB/s |
| multilingual_long (4,313 B) | 79.47 µs | 51.8 MiB/s |
| bitcoin (19,866 B) | 442.4 µs | 42.8 MiB/s |

Note: `count()` vs `encode()` overhead for bpe-openai is small (~3-13%), confirming
the allocation cost is minor. The main difference between tiktoken v3 and bpe-openai
is algorithmic: tiktoken v3 wins on short/ASCII text, bpe-openai wins on
multilingual/CJK text.
