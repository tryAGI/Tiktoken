#!/bin/sh
# Install ttok — the fastest .NET token counter CLI
# Usage: curl -fsSL https://raw.githubusercontent.com/tryAGI/Tiktoken/main/install.sh | sh
set -e

REPO="tryAGI/Tiktoken"
INSTALL_DIR="${TTOK_INSTALL_DIR:-/usr/local/bin}"
BINARY_NAME="ttok"

# Detect OS
OS="$(uname -s)"
case "$OS" in
  Linux)  os="linux" ;;
  Darwin) os="osx" ;;
  *)      echo "Error: Unsupported OS: $OS" >&2; exit 1 ;;
esac

# Detect architecture
ARCH="$(uname -m)"
case "$ARCH" in
  x86_64|amd64) arch="x64" ;;
  aarch64|arm64) arch="arm64" ;;
  *)             echo "Error: Unsupported architecture: $ARCH" >&2; exit 1 ;;
esac

# Detect musl (Alpine, etc.)
libc=""
if [ "$os" = "linux" ]; then
  if ldd --version 2>&1 | grep -qi musl || [ -f /etc/alpine-release ]; then
    libc="-musl"
  fi
fi

RID="${os}${libc}-${arch}"
ARTIFACT="ttok-${RID}.tar.gz"

# Get latest release URL
if [ -n "$TTOK_VERSION" ]; then
  TAG="v${TTOK_VERSION}"
  URL="https://github.com/${REPO}/releases/download/${TAG}/${ARTIFACT}"
else
  URL="https://github.com/${REPO}/releases/latest/download/${ARTIFACT}"
fi

echo "Installing ttok (${RID})..."
echo "  From: ${URL}"
echo "  To:   ${INSTALL_DIR}/${BINARY_NAME}"

# Dry-run mode: validate detection only
if [ "$1" = "--dry-run" ] || [ -n "$TTOK_DRY_RUN" ]; then
  echo "Dry run — detection successful."
  exit 0
fi

# Download and extract
TMPDIR="$(mktemp -d)"
trap 'rm -rf "$TMPDIR"' EXIT

if command -v curl >/dev/null 2>&1; then
  curl -fsSL "$URL" -o "$TMPDIR/$ARTIFACT"
elif command -v wget >/dev/null 2>&1; then
  wget -qO "$TMPDIR/$ARTIFACT" "$URL"
else
  echo "Error: curl or wget is required" >&2
  exit 1
fi

tar xzf "$TMPDIR/$ARTIFACT" -C "$TMPDIR"
chmod +x "$TMPDIR/$BINARY_NAME"

# Install (try without sudo first)
if [ -w "$INSTALL_DIR" ]; then
  mv "$TMPDIR/$BINARY_NAME" "$INSTALL_DIR/$BINARY_NAME"
else
  echo "  (requires sudo for ${INSTALL_DIR})"
  sudo mv "$TMPDIR/$BINARY_NAME" "$INSTALL_DIR/$BINARY_NAME"
fi

echo "Done! Run 'ttok --help' to get started."
