function craterclaw {
    [CmdletBinding()]
    param(
        [Parameter(Position = 0)]
        [string]$Subcommand = '',
        [string]$Project = '',
        [string]$Config = '',
        [switch]$ApiOnly,
        [switch]$WebOnly,
        [switch]$Console,
        [switch]$Check
    )

    $root = $env:CRATERCLAW_ROOT
    if (-not $root) {
        Write-Error "CRATERCLAW_ROOT is not set. Run '.\tools\Install-CraterClaw.ps1' to set it up."
        return
    }

    switch ($Subcommand.ToLowerInvariant()) {
        'run'    { Invoke-CcRun    -Root $root -ApiOnly:$ApiOnly -WebOnly:$WebOnly -UseConsole:$Console -Config $Config }
        'build'  { Invoke-CcBuild  -Root $root }
        'test'   { Invoke-CcTest   -Root $root -Project $Project }
        'format' { Invoke-CcFormat -Root $root -Project $Project -Check:$Check }
        default  { Write-CcUsage }
    }
}

function Invoke-CcRun {
    param([string]$Root, [string]$Config = '', [switch]$ApiOnly, [switch]$WebOnly, [switch]$UseConsole)

    $configArg = ''
    if ($Config) {
        $resolvedConfig = (Resolve-Path -LiteralPath $Config).Path
        $configArg = " -- --config `"$resolvedConfig`""
    }

    if ($UseConsole) {
        $consolePath = Join-Path $Root 'CraterClaw.Console'
        if ($Config) {
            & dotnet run --project $consolePath -- --config $resolvedConfig
        } else {
            & dotnet run --project $consolePath
        }
        return
    }

    $psExe = if ($PSVersionTable.PSEdition -eq 'Core') { 'pwsh' } else { 'powershell' }

    if (-not $WebOnly) {
        $apiPath = Join-Path $Root 'CraterClaw.Api'
        Start-Process $psExe -ArgumentList '-NoExit', '-Command', "dotnet run --project `"$apiPath`"$configArg"
    }

    if (-not $ApiOnly) {
        $webPath = Join-Path $Root 'CraterClaw.Web'
        Start-Process $psExe -ArgumentList '-NoExit', '-Command', "Set-Location `"$webPath`"; npm run dev"
    }
}

function Invoke-CcBuild {
    param([string]$Root)
    & dotnet build (Join-Path $Root 'CraterClaw.slnx')
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

function Invoke-CcTest {
    param([string]$Root, [string]$Project)

    $failed = $false

    switch ($Project.ToLowerInvariant()) {
        'core' {
            & dotnet test (Join-Path $Root 'CraterClaw.Core.Tests')
            if ($LASTEXITCODE -ne 0) { $failed = $true }
        }
        'api' {
            & dotnet test (Join-Path $Root 'CraterClaw.Api.Tests')
            if ($LASTEXITCODE -ne 0) { $failed = $true }
        }
        'web' {
            Push-Location (Join-Path $Root 'CraterClaw.Web')
            & npm test -- --run
            if ($LASTEXITCODE -ne 0) { $failed = $true }
            Pop-Location
        }
        default {
            & dotnet test (Join-Path $Root 'CraterClaw.slnx')
            if ($LASTEXITCODE -ne 0) { $failed = $true }
            Push-Location (Join-Path $Root 'CraterClaw.Web')
            & npm test -- --run
            if ($LASTEXITCODE -ne 0) { $failed = $true }
            Pop-Location
        }
    }

    if ($failed) { exit 1 }
}

function Invoke-CcFormat {
    param([string]$Root, [string]$Project, [switch]$Check)

    $failed = $false
    $dotnetFormatArgs = if ($Check) { @('--verify-no-changes') } else { @() }
    $npmScript = if ($Check) { 'lint' } else { 'lint:fix' }

    switch ($Project.ToLowerInvariant()) {
        'core' {
            & dotnet format (Join-Path $Root 'CraterClaw.Core' 'CraterClaw.Core.csproj') @dotnetFormatArgs
            if ($LASTEXITCODE -ne 0) { $failed = $true }
        }
        'api' {
            & dotnet format (Join-Path $Root 'CraterClaw.Api' 'CraterClaw.Api.csproj') @dotnetFormatArgs
            if ($LASTEXITCODE -ne 0) { $failed = $true }
        }
        'web' {
            Push-Location (Join-Path $Root 'CraterClaw.Web')
            & npm run $npmScript
            if ($LASTEXITCODE -ne 0) { $failed = $true }
            Pop-Location
        }
        default {
            & dotnet format (Join-Path $Root 'CraterClaw.slnx') @dotnetFormatArgs
            if ($LASTEXITCODE -ne 0) { $failed = $true }
            Push-Location (Join-Path $Root 'CraterClaw.Web')
            & npm run $npmScript
            if ($LASTEXITCODE -ne 0) { $failed = $true }
            Pop-Location
        }
    }

    if ($failed) { exit 1 }
}

function Write-CcUsage {
    Write-Host 'Usage: craterclaw <subcommand> [options]'
    Write-Host ''
    Write-Host 'Subcommands:'
    Write-Host '  run              Start the API and Vue dev server in separate windows'
    Write-Host '    -ApiOnly       Start only the API'
    Write-Host '    -WebOnly       Start only the Vue dev server'
    Write-Host '    -Console       Start the console harness in the current terminal'
    Write-Host '    -Config        Path to an alternate craterclaw.json (relative or absolute)'
    Write-Host '  build            Build the .NET solution'
    Write-Host '  test             Run all tests'
    Write-Host '    -Project       Run tests for one project: core, api, web'
    Write-Host '  format           Format all source (dotnet format + npm run lint:fix)'
    Write-Host '    -Project       Format one project: core, api, web'
    Write-Host '    -Check         Verify formatting without making changes'
}

Export-ModuleMember -Function craterclaw
