"""Regenerate with Python 3 and tiktoken==0.14.0; no .NET implementation is used."""
import hashlib
import importlib.metadata
import json
import os
import tempfile
from pathlib import Path
import time
import tiktoken

VERSION = "0.14.0"
COMMIT = "4e71bbe0c078468e00fefbf94b39849389f346e5"
VOCAB_HASH = "446a9538cb6c348e3516120d7c08b09f57c36495e2acfffe59a5bf8b0cfb1a2d"
assert importlib.metadata.version("tiktoken") == VERSION
root = Path(__file__).resolve().parents[4]
vocabulary = (root / "data/o200k_base.tiktoken").read_bytes()
assert hashlib.sha256(vocabulary).hexdigest() == VOCAB_HASH
# Use the verified local vocabulary instead of downloading it during regeneration.
cache = tempfile.TemporaryDirectory(prefix="tiktoken-parity-vocabulary-")
os.environ["TIKTOKEN_CACHE_DIR"] = cache.name
url = "https://openaipublic.blob.core.windows.net/encodings/o200k_base.tiktoken"
(Path(cache.name) / hashlib.sha1(url.encode()).hexdigest()).write_bytes(vocabulary)
h = tiktoken.get_encoding("o200k_harmony")
b = tiktoken.get_encoding("o200k_base")
ordinary = ["", "hello world", "Привет, мир!", "你好，世界！", "mixed_Identifier42.HTTP/2", "!? ...\n\t  \r\n", "🙂🚀👩‍💻 café"]
segments = ["<|start|>leading", "trailing<|end|>", "A<|endoftext|>B<|endoftext|>C", "alpha <|start|> bravo charlie delta", "<|start|><|message|><|end|>", "A<|start|><|start|>different", "Привет<|channel|>中文🙂<|message|>end", "<|start|>user<|message|>Hello<|end|>", "<|endofprompt|>", "<|reserved_200018|>"]
cases = []
for text in ordinary + segments:
    literal = h.encode(text, disallowed_special=())
    assert literal == b.encode(text, disallowed_special=())
    cases.append(dict(text=text, literal=literal, allowed=h.encode(text, allowed_special="all")))
long_cases = []
for piece, repeats in [("x", 1000), ("abcXYZ", 2000), ("!", 4096), ("x", 1_000_000)]:
    text = piece * repeats
    start = time.perf_counter()
    ids = h.encode(text)
    elapsed = time.perf_counter() - start
    digest = hashlib.sha256(",".join(map(str, ids)).encode()).hexdigest()
    long_cases.append(dict(piece=piece, repeats=repeats, count=len(ids), sha256=digest, seconds=elapsed))
    print(piece, repeats, len(ids), elapsed, flush=True)
models = ["gpt-5", "gpt-5-mini", "gpt-5.1", "gpt-5.2-2026-01-01", "gpt-oss-20b", "gpt-oss-120b", "gpt-4.1-mini", "gpt-4o-mini", "gpt-4", "text-embedding-3-large"]
fixture = dict(version=VERSION, commit=COMMIT, vocabularySha256=VOCAB_HASH,
               specialTokens=h._special_tokens, aliasDecode=h.decode([200018]), cases=cases,
               models={m: tiktoken.encoding_name_for_model(m) for m in models}, longCases=long_cases)
Path(__file__).with_name("HarmonyParity.json").write_text(json.dumps(fixture, ensure_ascii=False, indent=2) + "\n")
