# scripts/merge-binary-to-main.ps1
param(
    [Parameter(Mandatory=$true)]
    [string]$BinaryPath,

    [Parameter(Mandatory=$true)]
    [string]$BinaryName
)

$mainRepo = "C:\Users\cato\cs-f"
$targetPath = "$mainRepo\Binaries\$BinaryName"

# Copy binary to main repo
Copy-Item -Path $BinaryPath -Destination $targetPath -Force

Write-Host "Binary copied to: $targetPath"
Write-Host "Now commit on main branch:"
Write-Host "  git add Binaries/$BinaryName"
Write-Host "  git commit -m 'feat: add $BinaryName formatter binary'"
