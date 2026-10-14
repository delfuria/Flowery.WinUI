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
$solution = Join-Path (Get-RepoRoot) 'Flowery.WinUI.sln'
Initialize-BuildEnvironment -HeadName 'solution' -LogFileName 'all-build' -Configuration $Configuration -VerboseOutput:$VerboseOutput -SdkVersion $SdkVersion -DotNetPath $DotNetPath
if (-not (Invoke-ProjectBuild -Title 'Flowery.WinUI.sln' -Project $solution -Configuration $Configuration -Rebuild:$Rebuild)) {
    Write-BuildFailure 'Flowery.WinUI.sln'
    exit 1
}
Write-BuildSummary
