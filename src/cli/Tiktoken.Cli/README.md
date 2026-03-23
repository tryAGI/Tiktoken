# ttok — Token Counter CLI

The fastest .NET CLI tool for counting, encoding, decoding, and exploring BPE tokens. Powered by [Tiktoken](https://www.nuget.org/packages/Tiktoken/) (618 MiB/s).

## Install

```bash
# Homebrew (macOS — always installs latest release)
brew install tryAGI/tap/ttok

# Shell script (macOS/Linux — auto-detects OS/arch)
curl -fsSL https://raw.githubusercontent.com/tryAGI/Tiktoken/main/install.sh | sh
# Recommended for Linux — Homebrew cask is macOS-only

# PowerShell (Windows — auto-detects arch)
irm https://raw.githubusercontent.com/tryAGI/Tiktoken/main/install.ps1 | iex

# .NET global tool (NativeAOT on .NET 10+)
dotnet tool install -g Tiktoken.Cli

# Or run without installing (.NET 10+)
dnx Tiktoken.Cli "Hello world"
```

### Standalone binary (no .NET required)

Download a pre-built native binary from [GitHub Releases](https://github.com/tryAGI/Tiktoken/releases):

| Platform | Download |
|----------|----------|
| Linux x64 | `ttok-linux-x64.tar.gz` |
| Linux x64 (Alpine/musl) | `ttok-linux-musl-x64.tar.gz` |
| Linux ARM64 | `ttok-linux-arm64.tar.gz` |
| Linux ARM64 (Alpine/musl) | `ttok-linux-musl-arm64.tar.gz` |
| macOS Apple Silicon | `ttok-osx-arm64.tar.gz` |
| macOS Intel | `ttok-osx-x64.tar.gz` |
| Windows x64 | `ttok-win-x64.zip` |
| Windows ARM64 | `ttok-win-arm64.zip` |

### Docker

```bash
# Use the Alpine/musl binary in a container
docker run --rm -v "$(pwd):/src" -w /src alpine sh -c '
  wget -qO- https://github.com/tryAGI/Tiktoken/releases/latest/download/ttok-linux-musl-x64.tar.gz | tar xz
  echo "Hello world" | ./ttok
'
```

## Usage

### Count tokens (default: gpt-4o / o200k_base)

```bash
echo "Hello world" | ttok
# 3

cat prompt.md | ttok --model gpt-4
# 1847
```

### Encode / Decode / Explore

```bash
# Show token IDs
echo "Hello world" | ttok --encode
# 13225 2375

# Decode token IDs back to text
echo "13225 2375" | ttok --decode
# Hello world

# Show token boundaries
echo "Hello world" | ttok --explore
# |Hello| world|

# Truncate to N tokens
cat long_doc.md | ttok --truncate 4000
```

### Count files

```bash
ttok prompt.md system.md
# prompt.md    1,847
# system.md      423
# ─────────────────────
# 2 files      2,270 tokens
```

### Scan directories (respects .gitignore)

```bash
ttok src/ --include "*.cs"

# JSON output for CI
ttok . --format json

# Sort by token count
ttok . --sort tokens --top 10

# Group by extension
ttok . --group-by ext

# Compare against model context window
ttok . --context-check

# Fail if over limit (CI gate, exit code 2)
ttok . --max-tokens 50000
```

## Options

| Option | Description |
|--------|-------------|
| `-m, --model` | Model name (default: `gpt-4o`) |
| `-e, --encoding` | Encoding name (`cl100k_base`, `o200k_base`, etc.) |
| `--encode` | Output token IDs |
| `--decode` | Decode token IDs to text |
| `--explore` | Show token boundaries |
| `-t, --truncate N` | Truncate to N tokens |
| `--include` | Include file patterns (e.g. `*.cs`) |
| `--exclude` | Exclude file patterns |
| `--max-file-size` | Skip files over size (default: `50mb`) |
| `-f, --format` | Output format: `table` or `json` |
| `--sort` | Sort: `tokens` or `name` |
| `--top N` | Show only top N files |
| `--group-by ext` | Group by file extension |
| `--context-check` | Show % of model context window |
| `--max-tokens N` | Exit code 2 if exceeded |
| `-q, --quiet` | Suppress summary footer |
| `--no-default-excludes` | Include `bin/`, `obj/`, `node_modules/`, etc. |
| `--no-gitignore` | Disable .gitignore processing |
| `--progress` | Show progress to stderr |
| `--stats` | Show scan statistics to stderr |
| `--version` | Show version information |

## Supported models

All OpenAI models are supported via prefix matching: `gpt-4o`, `gpt-4.1`, `gpt-4`, `gpt-3.5-turbo`, `o1`, `o3`, `o4-mini`, and more.

Direct encoding names: `o200k_base`, `cl100k_base`, `p50k_base`, `p50k_edit`, `r50k_base`.
