#requires -Version 7.0
param(
    [ValidateSet('Debug', 'Release')][string]$Configuration = 'Debug',
    [switch]$NoRun,
    [switch]$Rebuild,
    [switch]$VerboseOutput,
    [string]$SdkVersion,
    [string]$DotNetPath
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'build_core.ps1')
$project = Join-Path (Get-RepoRoot) 'Flowery.WinUI.Kiosk\Flowery.WinUI.Kiosk.csproj'
Initialize-BuildEnvironment -HeadName 'Kiosk' -LogFileName 'kiosk-build' -Configuration $Configuration -VerboseOutput:$VerboseOutput -SdkVersion $SdkVersion -DotNetPath $DotNetPath
if (-not (Invoke-ProjectBuild -Title 'Flowery.WinUI.Kiosk' -Project $project -Configuration $Configuration -Rebuild:$Rebuild)) {
    Write-BuildFailure 'Flowery.WinUI.Kiosk'
    exit 1
}
Write-BuildSummary
if (-not $NoRun) {
    $runScript = Join-Path $PSScriptRoot 'run-kiosk.ps1'
    & $runScript -Configuration $Configuration -SkipBuild -NoWait -SdkVersion $SdkVersion -DotNetPath $DotNetPath
}
