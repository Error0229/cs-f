# scripts/merge-binary-to-main.ps1
param(
    [Parameter(Mandatory=$true)]
    [string]$BinaryPath,

    [Parameter(Mandatory=$true)]
    [string]$BinaryName
)

$scriptRoot = Split-Path -Parent $PSCommandPath
$mainRepo = Split-Path -Parent $scriptRoot  # Go up one level from scripts/
$targetPath = "$mainRepo\Binaries\$BinaryName"

# Validate source binary exists
if (-not (Test-Path $BinaryPath)) {
    Write-Error "Source binary not found: $BinaryPath"
    exit 1
}

# Copy binary to main repo
Copy-Item -Path $BinaryPath -Destination $targetPath -Force
if ($LASTEXITCODE -ne 0 -or -not (Test-Path $targetPath)) {
    Write-Error "Failed to copy binary to: $targetPath"
    exit 1
}

Write-Host "Binary copied to: $targetPath"
Write-Host "Now commit on main branch:"
Write-Host "  git add Binaries/$BinaryName"
Write-Host "  git commit -m 'feat: add $BinaryName formatter binary'"
