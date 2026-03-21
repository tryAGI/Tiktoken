//! Cross-language benchmark: Rust tokenizers.
//!
//! Compares:
//! - tiktoken crate v3 (pure Rust, arena-based)
//! - bpe-openai crate (GitHub's Aho-Corasick based, O(n) worst case)
//!
//! Uses o200k_base encoding to match the .NET benchmarks.
//! Reads inputs from ../inputs/ directory (run export_inputs.py first).
//!
//! Run: cargo bench

use criterion::{criterion_group, criterion_main, BenchmarkId, Criterion, Throughput};
use std::fs;
use std::path::PathBuf;

struct Input {
    name: &'static str,
    bytes: u64,
    text: String,
}

fn load_inputs() -> Vec<Input> {
    let inputs_dir: PathBuf = [env!("CARGO_MANIFEST_DIR"), "..", "inputs"].iter().collect();

    let specs: Vec<(&str, &str, u64)> = vec![
        ("hello_world", "hello_world.txt", 13),
        ("multilingual", "multilingual.txt", 382),
        ("cjk_heavy", "cjk_heavy.txt", 1_676),
        ("code", "code.txt", 879),
        ("multilingual_long", "multilingual_long.txt", 4_313),
        ("bitcoin", "bitcoin.txt", 19_866),
    ];

    specs
        .into_iter()
        .map(|(name, file, bytes)| {
            let path = inputs_dir.join(file);
            let text = fs::read_to_string(&path)
                .unwrap_or_else(|e| panic!("Failed to read {}: {}", path.display(), e));
            let actual_bytes = text.len() as u64;
            if actual_bytes != bytes {
                eprintln!(
                    "WARNING: {} expected {} bytes, got {} bytes",
                    name, bytes, actual_bytes
                );
            }
            Input { name, bytes, text }
        })
        .collect()
}

fn bench_tiktoken(c: &mut Criterion) {
    let inputs = load_inputs();
    let enc = tiktoken::EncodingFactory::o200k_base().unwrap();

    let mut group = c.benchmark_group("tiktoken_v3");

    for input in &inputs {
        group.throughput(Throughput::Bytes(input.bytes));
        group.bench_with_input(
            BenchmarkId::new(input.name, input.bytes),
            &input.text,
            |b, text| {
                b.iter(|| enc.encode(text));
            },
        );
    }

    group.finish();
}

fn bench_bpe_openai(c: &mut Criterion) {
    let inputs = load_inputs();
    let tokenizer = bpe_openai::o200k_base();

    let mut group = c.benchmark_group("github_bpe");

    for input in &inputs {
        group.throughput(Throughput::Bytes(input.bytes));
        group.bench_with_input(
            BenchmarkId::new(input.name, input.bytes),
            &input.text,
            |b, text| {
                b.iter(|| tokenizer.count(text));
            },
        );
    }

    group.finish();
}

criterion_group!(benches, bench_tiktoken, bench_bpe_openai);
criterion_main!(benches);
