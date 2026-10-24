#requires -Version 7.0
param(
    [ValidateSet('Debug', 'Release')][string]$Configuration = 'Debug',
    [switch]$SkipBuild,
    [switch]$Manual,
    [switch]$NoWait,
    [ValidateRange(0, 16)][int]$Monitor = 0,
    [ValidateSet('Full', 'UpperHalf', 'LowerHalf')][string]$WindowRegion = 'LowerHalf',
    [string]$SdkVersion,
    [string]$DotNetPath
)

$ErrorActionPreference = 'Stop'
if (-not $IsWindows) { throw 'Run this script in native PowerShell 7 on Windows.' }
if (-not $SkipBuild) {
    $buildScript = Join-Path $PSScriptRoot 'build_kiosk.ps1'
    & $buildScript -Configuration $Configuration -NoRun -SdkVersion $SdkVersion -DotNetPath $DotNetPath
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
$repoRoot = Split-Path -Parent $PSScriptRoot
$relative = 'bin\{0}\Flowery.WinUI.Kiosk\net10.0-windows10.0.19041\win-x64\Flowery.WinUI.Kiosk.exe' -f $Configuration
$executable = Join-Path $repoRoot $relative
if (-not (Test-Path -LiteralPath $executable -PathType Leaf)) { throw ('Kiosk executable is missing: {0}' -f $executable) }
$startInfo = [Diagnostics.ProcessStartInfo]::new()
$startInfo.FileName = $executable
$startInfo.WorkingDirectory = Split-Path -Parent $executable
$startInfo.UseShellExecute = $false
$startInfo.CreateNoWindow = $true
if ($Manual) { $startInfo.ArgumentList.Add('--manual') }
if ($Monitor -gt 0) {
    $startInfo.Environment['FLOWERY_MONITOR_DEVICE'] = '\\.\DISPLAY{0}' -f $Monitor
    $startInfo.Environment['FLOWERY_MONITOR_REGION'] = switch ($WindowRegion) { 'UpperHalf' { 'upper-half' } 'LowerHalf' { 'lower-half' } default { 'full' } }
}
$process = [Diagnostics.Process]::Start($startInfo)
Write-Host ('Started Kiosk process {0}: {1}' -f $process.Id, $executable)
if (-not $NoWait) {
    try { $process.WaitForExit(); $exitCode = $process.ExitCode }
    finally { $process.Dispose() }
    exit $exitCode
}
$process.Dispose()
