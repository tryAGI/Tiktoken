# Harmony parity baseline

- Official Python package: `tiktoken==0.14.0`.
- Source commit: [`4e71bbe0c078468e00fefbf94b39849389f346e5`](https://github.com/openai/tiktoken/tree/4e71bbe0c078468e00fefbf94b39849389f346e5).
- Definitions: `tiktoken_ext/openai_public.py`, `tiktoken/model.py`.
- `data/o200k_base.tiktoken` SHA-256: `446a9538cb6c348e3516120d7c08b09f57c36495e2acfffe59a5bf8b0cfb1a2d`.
- Captured on 2026-10-16, macOS arm64, Python 3.14 / .NET 10.

`HarmonyParity.json` is generated exclusively by the official Python library. It
contains the complete special-token map, observed alias decode, ordinary and
mixed/special token sequences, model resolutions, and ordered-token SHA-256/counts
for long pieces. Its `seconds` values are single-run observations, not benchmark
thresholds. The generator validates package version and vocabulary hash and seeds
a temporary official cache from that verified local vocabulary; no vocabulary
network fetch is necessary.

Both `<|endofprompt|>` and `<|reserved_200018|>` encode to 200018. The pinned
upstream decodes it as `<|endofprompt|>` (also checked in eight fresh Python
processes). .NET preserves the first registered spelling for duplicate special IDs.
The test checks the captured canonical spelling rather than assuming each alias
round-trips to itself. Recheck this behavior when updating the upstream pin.

## Regeneration and checks

From the repository root:

```sh
python3 -m venv /tmp/tiktoken-parity
/tmp/tiktoken-parity/bin/pip install tiktoken==0.14.0
/tmp/tiktoken-parity/bin/python src/tests/Tiktoken.UnitTests/Resources/GenerateHarmonyParity.py
dotnet test src/tests/Tiktoken.UnitTests -c Release --filter FullyQualifiedName~HarmonyTests
dotnet build src/cli/Tiktoken.Cli -c Release
python3 src/tests/Tiktoken.UnitTests/Resources/VerifyHarmonyCli.py
```

The unit suite runs on .NET 8, 9 and 10 and compares ordered token IDs, literal
counts, complete special maps, explicit allowed/disallowed policies, cache on/off,
model selection, alias decoding, and #143 offsets in encode/explore/UTF-safe
explore. Long-piece checks have a 120-second test timeout and cover repeated
letters, mixed identifiers, punctuation, and one million `x` characters (125,000
tokens). They compare a SHA-256 of comma-separated decimal token IDs, not only
counts. A local .NET 10 observation for the million-`x` encode was 0.245 s versus
0.211 s upstream; these are smoke measurements and do not justify BPE changes.

The legacy libraries can be exercised on the .NET 10 test host without modifying
shared build output:

```sh
dotnet build src/libs/Tiktoken -c Release
python3 src/tests/Tiktoken.UnitTests/Resources/VerifyLegacyHarmony.py netstandard2.0
python3 src/tests/Tiktoken.UnitTests/Resources/VerifyLegacyHarmony.py netstandard2.1
```

The helper copies the test output to a private temporary directory and replaces
library assemblies with the selected built netstandard artifacts. The suite
bounds the legacy quadratic BPE lane to the 1,000-character repeated-letter,
12,000-character identifier and 4,096-character punctuation cases; it omits the
million-character case there. Modern target lanes exercise that full scenario.
Compiling net462 is covered by the library build; execution on the .NET Framework
runtime requires Windows and is not claimed by this helper.
