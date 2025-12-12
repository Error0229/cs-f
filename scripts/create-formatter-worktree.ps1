# scripts/create-formatter-worktree.ps1
param(
    [Parameter(Mandatory=$true)]
    [string]$FormatterName,

    [Parameter(Mandatory=$true)]
    [string]$Language
)

$branchName = "formatter/$Language-$FormatterName"
$worktreePath = "../cs-f-$Language-$FormatterName"

# Create orphan branch and worktree
git worktree add -b $branchName $worktreePath
Set-Location $worktreePath

# Create standard directory structure
New-Item -ItemType Directory -Path "build" -Force
New-Item -ItemType Directory -Path "src" -Force
New-Item -ItemType Directory -Path ".github/workflows" -Force

Write-Host "Worktree created at: $worktreePath"
Write-Host "Branch: $branchName"
