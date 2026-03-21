$docsFolder = [System.Environment]::GetFolderPath('MyDocuments')
$psFolder = if ($PSVersionTable.PSEdition -eq 'Core') { 'PowerShell' } else { 'WindowsPowerShell' }
$moduleDir = Join-Path (Join-Path (Join-Path $docsFolder $psFolder) 'Modules') 'CraterClaw'

# Remove module directory
if (Test-Path $moduleDir) {
    Remove-Item -Path $moduleDir -Recurse -Force
    Write-Host "Removed module directory: $moduleDir"
} else {
    Write-Host "Module directory not found: $moduleDir"
}

# Remove CRATERCLAW_ROOT environment variable
if ([System.Environment]::GetEnvironmentVariable('CRATERCLAW_ROOT', 'User')) {
    [System.Environment]::SetEnvironmentVariable('CRATERCLAW_ROOT', $null, 'User')
    Remove-Item Env:CRATERCLAW_ROOT -ErrorAction SilentlyContinue
    Write-Host "Removed CRATERCLAW_ROOT environment variable"
} else {
    Write-Host "CRATERCLAW_ROOT was not set"
}

# Remove Import-Module line from profile
$profilePath = $PROFILE.CurrentUserAllHosts
$importLine = 'Import-Module CraterClaw'
if (Test-Path $profilePath) {
    $profileContent = Get-Content $profilePath -Raw
    if ($profileContent -and $profileContent.Contains($importLine)) {
        $updated = ($profileContent -split "`n" | Where-Object { $_.Trim() -ne $importLine }) -join "`n"
        Set-Content -Path $profilePath -Value $updated.TrimEnd()
        Write-Host "Removed '$importLine' from: $profilePath"
    } else {
        Write-Host "Profile did not contain '$importLine'"
    }
} else {
    Write-Host "Profile not found: $profilePath"
}

Write-Host ""
Write-Host "Done. Open a new session for changes to take effect."
