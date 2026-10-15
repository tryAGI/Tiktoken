"""Run the Harmony suite against netstandard libraries on the .NET 10 test host.

Build the net10.0 unit tests and the requested library target first. Only a
private temporary copy is modified; source/build outputs remain untouched.
"""
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile

root = Path(__file__).resolve().parents[4]
framework = sys.argv[1] if len(sys.argv) > 1 else 'netstandard2.0'
assert framework in ('netstandard2.0', 'netstandard2.1')
with tempfile.TemporaryDirectory(prefix='tiktoken-harmony-legacy-') as directory:
    target = Path(directory) / 'tests'
    shutil.copytree(root / 'src/tests/Tiktoken.UnitTests/bin/Release/net10.0', target)
    for name in ['Tiktoken', 'Tiktoken.Core', 'Tiktoken.Encodings.Abstractions',
                 'Tiktoken.Encodings.o200k', 'Tiktoken.Encodings.cl100k',
                 'Tiktoken.Encodings.p50k', 'Tiktoken.Encodings.r50k', 'Tiktoken.Encodings.Tokenizer']:
        shutil.copyfile(root / f'src/libs/{name}/bin/Release/{framework}/{name}.dll', target / f'{name}.dll')
    subprocess.run(['dotnet', 'vstest', str(target / 'Tiktoken.UnitTests.dll'),
                    '--TestCaseFilter:FullyQualifiedName~HarmonyTests'], check=True, timeout=120)
print(f'Harmony suite passed against {framework} libraries.')
