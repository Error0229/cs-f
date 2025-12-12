# scripts/create-formatter-worktree.ps1
param(
    [Parameter(Mandatory=$true)]
    [string]$FormatterName,

    [Parameter(Mandatory=$true)]
    [string]$Language
)

$branchName = "formatter/$Language-$FormatterName"
$worktreePath = "../cs-f-$Language-$FormatterName"

# Create branch and worktree
git worktree add -b $branchName $worktreePath
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to create git worktree"
    exit 1
}

if (-not (Test-Path $worktreePath)) {
    Write-Error "Worktree path does not exist: $worktreePath"
    exit 1
}

Set-Location $worktreePath

# Create standard directory structure
New-Item -ItemType Directory -Path "build" -Force
New-Item -ItemType Directory -Path "src" -Force
New-Item -ItemType Directory -Path ".github/workflows" -Force

Write-Host "Worktree created at: $worktreePath"
Write-Host "Branch: $branchName"
