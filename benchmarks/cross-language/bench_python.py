"""Cross-language benchmark: Python (OpenAI tiktoken).

Requires: pip install tiktoken

Uses o200k_base encoding to match the .NET benchmarks.
Reads inputs from inputs/ directory (run export_inputs.py first).
"""

import os
import time
import statistics

import tiktoken


INPUT_DIR = os.path.join(os.path.dirname(__file__), "inputs")

INPUTS = [
    ("hello_world", 13),
    ("multilingual", 382),
    ("cjk_heavy", 1_676),
    ("code", 879),
    ("multilingual_long", 4_313),
    ("bitcoin", 19_866),
]

WARMUP = 100
ITERATIONS = 5_000


def load_inputs():
    texts = {}
    for name, expected_bytes in INPUTS:
        path = os.path.join(INPUT_DIR, f"{name}.txt")
        with open(path, "r", encoding="utf-8") as f:
            text = f.read()
        actual_bytes = len(text.encode("utf-8"))
        if actual_bytes != expected_bytes:
            print(f"  WARNING: {name} expected {expected_bytes} B, got {actual_bytes} B")
        texts[name] = text
    return texts


def bench_encode(enc, text, iterations):
    """Benchmark encode (returns token list)."""
    # Warmup
    for _ in range(WARMUP):
        enc.encode(text)

    times = []
    for _ in range(iterations):
        start = time.perf_counter_ns()
        enc.encode(text)
        elapsed = time.perf_counter_ns() - start
        times.append(elapsed)

    return times


def format_time(ns):
    if ns < 1_000:
        return f"{ns:.0f} ns"
    elif ns < 1_000_000:
        return f"{ns / 1_000:.1f} us"
    else:
        return f"{ns / 1_000_000:.1f} ms"


def format_throughput(byte_size, ns):
    if ns == 0:
        return "∞"
    mib_per_sec = (byte_size / (1024 * 1024)) / (ns / 1e9)
    return f"{mib_per_sec:.0f} MiB/s"


def main():
    print("Cross-Language Tokenizer Benchmark: Python (OpenAI tiktoken)")
    print(f"Encoding: o200k_base | Warmup: {WARMUP} | Iterations: {ITERATIONS}")
    print(f"tiktoken version: {tiktoken.__version__}")
    print()

    texts = load_inputs()
    enc = tiktoken.get_encoding("o200k_base")

    print("| Input | Median | P95 | Throughput |")
    print("|-------|--------|-----|:----------:|")

    for name, byte_size in INPUTS:
        text = texts[name]
        times = bench_encode(enc, text, ITERATIONS)
        median = statistics.median(times)
        p95 = sorted(times)[int(len(times) * 0.95)]

        print(
            f"| {name} ({byte_size:,} B) "
            f"| {format_time(median)} "
            f"| {format_time(p95)} "
            f"| {format_throughput(byte_size, median)} |"
        )

    print()
    print("Note: Python tiktoken uses a Rust core via C extension.")
    print("Throughput = UTF-8 bytes / median time.")


if __name__ == "__main__":
    main()
