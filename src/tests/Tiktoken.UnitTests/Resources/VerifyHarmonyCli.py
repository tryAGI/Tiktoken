"""Run after: dotnet build src/cli/Tiktoken.Cli -c Release (Python standard library only)."""
import json
from pathlib import Path
import subprocess
import tempfile

root = Path(__file__).resolve().parents[4]
cli = root / 'src/cli/Tiktoken.Cli/bin/Release/net10.0/ttok.dll'
# Assembly name is configured by the project; accept its default too.
if not cli.exists():
    cli = cli.with_name('Tiktoken.Cli.dll')
fixture = json.loads(Path(__file__).with_name('HarmonyParity.json').read_text())
text = '<|start|>user<|message|>Hello<|end|>'
case = next(c for c in fixture['cases'] if c['text'] == text)

def run(*args, input=text):
    result = subprocess.run(['dotnet', str(cli), *args], input=input, text=True, capture_output=True, timeout=30)
    assert result.returncode == 0, result.stderr
    return result.stdout.strip()

assert '--allow-special' in run('--help')
for selector in [('-e', 'o200k_harmony'), ('-m', 'gpt-oss-20b'), ('-m', 'gpt-oss-120b')]:
    assert int(run(*selector)) == len(case['literal'])
    assert int(run(*selector, '--allow-special')) == len(case['allowed'])
    assert run(*selector, '--allow-special', '--encode') == ' '.join(map(str, case['allowed']))
    assert run(*selector, '--decode', input=' '.join(map(str, case['allowed']))) == text
    assert run(*selector, '--allow-special', '--truncate', '3') == '<|start|>user<|message|>'
with tempfile.TemporaryDirectory() as directory:
    path = Path(directory) / 'message.txt'
    path.write_text(text)
    assert run('-e', 'o200k_harmony', '--allow-special', '--encode', str(path)) == ' '.join(map(str, case['allowed']))
    for source in (str(path), directory):
        for special in (False, True):
            args = ['-e', 'o200k_harmony', '--format', 'json', source]
            if special:
                args.append('--allow-special')
            output = json.loads(run(*args))
            # Verify counts independently of table formatting.
            expected = len(case['allowed' if special else 'literal'])
            assert output["total"] == expected, output
            assert len(output["files"]) == 1 and output["files"][0]["tokens"] == expected, output
print('Harmony CLI checks passed: help, stdin, models, encode/decode, truncate, file and directory counts.')
