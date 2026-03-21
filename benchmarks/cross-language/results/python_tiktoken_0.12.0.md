# Python tiktoken 0.12.0 — Apple M4 Max

Encoding: o200k_base | Warmup: 100 | Iterations: 5000

| Input | Median | P95 | Throughput |
|-------|--------|-----|:----------:|
| hello_world (13 B) | 1.9 us | 2.0 us | 7 MiB/s |
| multilingual (382 B) | 24.7 us | 29.5 us | 15 MiB/s |
| cjk_heavy (1,676 B) | 80.5 us | 91.5 us | 20 MiB/s |
| code (879 B) | 91.2 us | 103.4 us | 9 MiB/s |
| multilingual_long (4,313 B) | 238.7 us | 317.4 us | 17 MiB/s |
| bitcoin (19,866 B) | 1.6 ms | 1.8 ms | 12 MiB/s |

Note: Python tiktoken uses a Rust core via C extension. The low throughput
compared to the raw Rust core is due to Python ↔ Rust FFI overhead and
Python's GIL.
