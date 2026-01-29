# ============================================================================
# Sync Private Lab to Public Repository (PowerShell)
# ============================================================================
# This script copies files from the private repo to the public repo
# while preserving the public repo's git configuration.
#
# Usage: .\scripts\sync-to-public.ps1
# ============================================================================

$ErrorActionPreference = "Stop"

# Configuration - Update these paths for your environment
$SourceDir = "C:\src\chat_api_lab_private"
$DestDir = "C:\src\chat_api_lab_public"

# Exclusions
$Exclusions = @(
    '.git',
    '.claude',
    'appsettings.Development.json',
    'appsettings.Local.json',
    '*.local.json',
    '.env',
    '.env.*',
    'secrets.json',
    'bin',
    'obj',
    'out',
    'publish',
    '.vs',
    '.vscode',
    '.idea',
    '*.user',
    '*.suo',
    '.DS_Store',
    'Thumbs.db',
    'deploy.zip',
    '*.zip',
    '.local'
)

Write-Host "========================================" -ForegroundColor Green
Write-Host "Syncing Private Lab to Public Repository" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Source: $SourceDir"
Write-Host "Destination: $DestDir"
Write-Host ""

# Verify source exists
if (-not (Test-Path $SourceDir)) {
    Write-Host "Error: Source directory does not exist: $SourceDir" -ForegroundColor Red
    exit 1
}

# Verify destination exists
if (-not (Test-Path $DestDir)) {
    Write-Host "Error: Destination directory does not exist: $DestDir" -ForegroundColor Red
    exit 1
}

# Verify destination has .git (is a git repo)
if (-not (Test-Path (Join-Path $DestDir ".git"))) {
    Write-Host "Warning: Destination does not appear to be a git repository" -ForegroundColor Yellow
    $confirm = Read-Host "Continue anyway? (y/N)"
    if ($confirm -ne 'y' -and $confirm -ne 'Y') {
        exit 1
    }
}

Write-Host "Preserving .git directory..." -ForegroundColor Yellow

# Function to check if path should be excluded
function Test-Excluded {
    param([string]$Path, [string]$BasePath)
    
    $relativePath = $Path.Substring($BasePath.Length).TrimStart('\', '/')
    
    foreach ($exclusion in $Exclusions) {
        # Check exact match
        if ($relativePath -eq $exclusion) { return $true }
        
        # Check if any path component matches
        $parts = $relativePath -split '[\\/]'
        foreach ($part in $parts) {
            if ($part -eq $exclusion) { return $true }
            if ($exclusion.Contains('*') -and $part -like $exclusion) { return $true }
        }
        
        # Check wildcard match on filename
        $fileName = Split-Path $relativePath -Leaf
        if ($exclusion.Contains('*') -and $fileName -like $exclusion) { return $true }
    }
    
    return $false
}

Write-Host "Copying files..." -ForegroundColor Yellow

# Get all files from source
$sourceFiles = Get-ChildItem -Path $SourceDir -Recurse -Force -ErrorAction SilentlyContinue

$copiedCount = 0
$skippedCount = 0

foreach ($item in $sourceFiles) {
    $relativePath = $item.FullName.Substring($SourceDir.Length).TrimStart('\', '/')
    $destPath = Join-Path $DestDir $relativePath
    
    # Check if excluded
    if (Test-Excluded -Path $item.FullName -BasePath $SourceDir) {
        $skippedCount++
        continue
    }
    
    if ($item.PSIsContainer) {
        # Create directory if it doesn't exist
        if (-not (Test-Path $destPath)) {
            New-Item -ItemType Directory -Path $destPath -Force | Out-Null
        }
    } else {
        # Copy file
        $destDir = Split-Path $destPath -Parent
        if (-not (Test-Path $destDir)) {
            New-Item -ItemType Directory -Path $destDir -Force | Out-Null
        }
        Copy-Item -Path $item.FullName -Destination $destPath -Force
        $copiedCount++
    }
}

# Clean up files in destination that don't exist in source (except .git)
Write-Host "Cleaning up removed files..." -ForegroundColor Yellow
$destFiles = Get-ChildItem -Path $DestDir -Recurse -Force -ErrorAction SilentlyContinue

$removedCount = 0
foreach ($item in $destFiles) {
    $relativePath = $item.FullName.Substring($DestDir.Length).TrimStart('\', '/')
    $sourcePath = Join-Path $SourceDir $relativePath
    
    # Skip .git folder
    if ($relativePath -like '.git*') { continue }
    
    # If file doesn't exist in source and isn't excluded, remove it
    if (-not (Test-Path $sourcePath)) {
        if (-not $item.PSIsContainer) {
            Remove-Item -Path $item.FullName -Force
            $removedCount++
        }
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "Sync Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Summary:"
Write-Host "  Files copied:  $copiedCount"
Write-Host "  Files skipped: $skippedCount"
Write-Host "  Files removed: $removedCount"
Write-Host ""
Write-Host "Next steps:"
Write-Host "  1. cd $DestDir"
Write-Host "  2. git status                    # Review changes"
Write-Host "  3. git diff                      # Review file changes"
Write-Host "  4. git add -A                    # Stage all changes"
Write-Host "  5. git commit -m 'Sync from private repo'"
Write-Host "  6. git push                      # Push to public repo"
Write-Host ""
Write-Host "Remember to review changes before committing!" -ForegroundColor Yellow
