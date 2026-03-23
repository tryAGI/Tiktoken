# Install ttok — the fastest .NET token counter CLI
# Usage: irm https://raw.githubusercontent.com/tryAGI/Tiktoken/main/install.ps1 | iex
$ErrorActionPreference = 'Stop'

$repo = 'tryAGI/Tiktoken'
$installDir = if ($env:TTOK_INSTALL_DIR) { $env:TTOK_INSTALL_DIR } else { Join-Path $env:LOCALAPPDATA 'ttok' }

# Detect architecture
$arch = if ([Environment]::Is64BitOperatingSystem) {
    if ($env:PROCESSOR_ARCHITECTURE -eq 'ARM64' -or $env:PROCESSOR_IDENTIFIER -match 'ARM') { 'arm64' } else { 'x64' }
} else {
    Write-Error 'Error: 32-bit Windows is not supported.'
    return
}

$rid = "win-$arch"
$artifact = "ttok-$rid.zip"

# Build download URL
if ($env:TTOK_VERSION) {
    $tag = "v$env:TTOK_VERSION"
    $url = "https://github.com/$repo/releases/download/$tag/$artifact"
} else {
    $url = "https://github.com/$repo/releases/latest/download/$artifact"
}

Write-Host "Installing ttok ($rid)..."
Write-Host "  From: $url"
Write-Host "  To:   $installDir"

# Dry-run mode: validate detection only
if ($env:TTOK_DRY_RUN) {
    Write-Host "Dry run — detection successful."
    return
}

# Download and extract
$tmp = New-TemporaryFile | ForEach-Object { Remove-Item $_; New-Item -ItemType Directory -Path $_ }
$zipPath = Join-Path $tmp $artifact
Invoke-WebRequest -Uri $url -OutFile $zipPath -UseBasicParsing
Expand-Archive -Path $zipPath -DestinationPath $tmp -Force

# Install
New-Item -ItemType Directory -Path $installDir -Force | Out-Null
Copy-Item (Join-Path $tmp 'ttok.exe') (Join-Path $installDir 'ttok.exe') -Force

# Add to PATH if not already there
$userPath = [Environment]::GetEnvironmentVariable('PATH', 'User')
if ($userPath -notlike "*$installDir*") {
    [Environment]::SetEnvironmentVariable('PATH', "$userPath;$installDir", 'User')
    Write-Host "  Added $installDir to user PATH (restart terminal to take effect)"
}

# Clean up
Remove-Item $tmp -Recurse -Force

Write-Host "Done! Run 'ttok --help' to get started."
