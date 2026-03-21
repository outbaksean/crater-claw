$repoRoot = Split-Path $PSScriptRoot -Parent
$moduleSrc = Join-Path $PSScriptRoot 'CraterClaw.psm1'

# Determine module directory based on PowerShell edition
$docsFolder = [System.Environment]::GetFolderPath('MyDocuments')
$psFolder = if ($PSVersionTable.PSEdition -eq 'Core') { 'PowerShell' } else { 'WindowsPowerShell' }
$moduleDir = Join-Path (Join-Path (Join-Path $docsFolder $psFolder) 'Modules') 'CraterClaw'

# Create module directory
if (-not (Test-Path $moduleDir)) {
    New-Item -ItemType Directory -Path $moduleDir -Force | Out-Null
    Write-Host "Created module directory: $moduleDir"
} else {
    Write-Host "Module directory exists: $moduleDir"
}

# Copy module file
Copy-Item -Path $moduleSrc -Destination (Join-Path $moduleDir 'CraterClaw.psm1') -Force
Write-Host "Installed module to: $moduleDir"

# Set CRATERCLAW_ROOT as a persistent user environment variable
[System.Environment]::SetEnvironmentVariable('CRATERCLAW_ROOT', $repoRoot, 'User')
$env:CRATERCLAW_ROOT = $repoRoot
Write-Host "Set CRATERCLAW_ROOT = $repoRoot"

# Add Import-Module to profile if not already present
$profilePath = $PROFILE.CurrentUserAllHosts
$profileDir = Split-Path $profilePath
if (-not (Test-Path $profileDir)) {
    New-Item -ItemType Directory -Path $profileDir -Force | Out-Null
}
if (-not (Test-Path $profilePath)) {
    New-Item -ItemType File -Path $profilePath -Force | Out-Null
}

$importLine = 'Import-Module CraterClaw'
$profileContent = Get-Content $profilePath -Raw -ErrorAction SilentlyContinue
if (-not ($profileContent -and $profileContent.Contains($importLine))) {
    Add-Content -Path $profilePath -Value "`n$importLine"
    Write-Host "Added '$importLine' to: $profilePath"
} else {
    Write-Host "Profile already imports CraterClaw"
}

Write-Host ""
Write-Host "Done. Run 'Import-Module CraterClaw' or open a new session to use the craterclaw command."
