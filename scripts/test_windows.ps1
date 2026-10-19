<#
.SYNOPSIS
    Runs the native WinUI runtime and unit tests through the Windows runner.
.DESCRIPTION
    Uses the normal solution output. Test state and reports use a unique directory.
#>
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',
    [switch]$SkipBuild,
    [string]$TestFilter,
    [ValidateRange(0, 16)]
    [int]$Monitor = 0,
    [ValidateSet('Full', 'LowerHalf')]
    [string]$WindowRegion = 'LowerHalf',
    [string]$SdkVersion,
    [string]$DotNetPath
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$framework = 'net10.0-windows10.0.19041'
$runtime = 'win-x64'

if (-not $IsWindows -or $PSVersionTable.PSEdition -ne 'Core' -or $PSVersionTable.PSVersion.Major -lt 7) {
    throw 'Run this script in native PowerShell 7 on Windows.'
}

if (-not $SkipBuild) {
    $buildScript = Join-Path $PSScriptRoot 'build_all.ps1'
    & $buildScript -Configuration $Configuration -NoRun -SdkVersion $SdkVersion -DotNetPath $DotNetPath
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}

$galleryPath = Join-Path $repoRoot "bin\$Configuration\Flowery.WinUI.Gallery\$framework\$runtime\Flowery.WinUI.Gallery.exe"
$runnerPath = Join-Path $repoRoot "bin\$Configuration\Flowery.WinUI.RuntimeTests.WindowsRunner\$framework\$runtime\Flowery.WinUI.RuntimeTests.WindowsRunner.exe"
foreach ($path in @($galleryPath, $runnerPath)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw ('Required executable is missing. Build the solution first: {0}' -f $path)
    }
}

$runId = '{0}-{1}' -f (Get-Date -Format 'yyyyMMdd-HHmmss-fff'), ([Guid]::NewGuid().ToString('N').Substring(0, 8))
$resultsDirectory = Join-Path $repoRoot "TestResults\$Configuration\$runId"
$logDirectory = Join-Path $repoRoot 'logs'
New-Item -ItemType Directory -Path $resultsDirectory -Force | Out-Null
New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null
$logPath = Join-Path $logDirectory "tests-$Configuration-$runId.log"
$runnerArguments = @('--report-trx', '--results-directory', $resultsDirectory)
$previousGalleryPath = [Environment]::GetEnvironmentVariable('FLOWERY_GALLERY_EXE_PATH', 'Process')
$previousResultsRoot = [Environment]::GetEnvironmentVariable('FLOWERY_TEST_RESULTS_ROOT', 'Process')
$previousTestFilter = [Environment]::GetEnvironmentVariable('FLOWERY_RUNTIME_TEST_FILTER', 'Process')
$previousMonitorDevice = [Environment]::GetEnvironmentVariable('FLOWERY_MONITOR_DEVICE', 'Process')
$previousMonitorRegion = [Environment]::GetEnvironmentVariable('FLOWERY_MONITOR_REGION', 'Process')

try {
    [Environment]::SetEnvironmentVariable('FLOWERY_GALLERY_EXE_PATH', $galleryPath, 'Process')
    [Environment]::SetEnvironmentVariable('FLOWERY_TEST_RESULTS_ROOT', $resultsDirectory, 'Process')
    [Environment]::SetEnvironmentVariable('FLOWERY_RUNTIME_TEST_FILTER', $TestFilter, 'Process')
    if ($Monitor -gt 0) {
        $monitorDevice = '\\.\DISPLAY{0}' -f $Monitor
        $monitorRegion = if ($WindowRegion -eq 'LowerHalf') { 'lower-half' } else { 'full' }
        [Environment]::SetEnvironmentVariable('FLOWERY_MONITOR_DEVICE', $monitorDevice, 'Process')
        [Environment]::SetEnvironmentVariable('FLOWERY_MONITOR_REGION', $monitorRegion, 'Process')
        Write-Host ('Window placement: {0}, {1}' -f $monitorDevice, $WindowRegion)
    }
    Write-Host ('Gallery: {0}' -f $galleryPath)
    Write-Host ('Results: {0}' -f $resultsDirectory)
    & $runnerPath @runnerArguments 2>&1 | Tee-Object -FilePath $logPath
    $testExitCode = $LASTEXITCODE
}
finally {
    [Environment]::SetEnvironmentVariable('FLOWERY_GALLERY_EXE_PATH', $previousGalleryPath, 'Process')
    [Environment]::SetEnvironmentVariable('FLOWERY_TEST_RESULTS_ROOT', $previousResultsRoot, 'Process')
    [Environment]::SetEnvironmentVariable('FLOWERY_RUNTIME_TEST_FILTER', $previousTestFilter, 'Process')
    [Environment]::SetEnvironmentVariable('FLOWERY_MONITOR_DEVICE', $previousMonitorDevice, 'Process')
    [Environment]::SetEnvironmentVariable('FLOWERY_MONITOR_REGION', $previousMonitorRegion, 'Process')
}

Write-Host ('Log: {0}' -f $logPath)
exit $testExitCode
