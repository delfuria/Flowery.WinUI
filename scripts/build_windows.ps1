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
$project = Join-Path (Get-RepoRoot) 'Flowery.WinUI.Gallery\Flowery.WinUI.Gallery.csproj'
Initialize-BuildEnvironment -HeadName 'Gallery' -LogFileName 'windows-build' -Configuration $Configuration -VerboseOutput:$VerboseOutput -SdkVersion $SdkVersion -DotNetPath $DotNetPath
if (-not (Invoke-ProjectBuild -Title 'Flowery.WinUI.Gallery' -Project $project -Configuration $Configuration -Rebuild:$Rebuild)) {
    Write-BuildFailure 'Flowery.WinUI.Gallery'
    exit 1
}
Write-BuildSummary
if (-not $NoRun) {
    Start-GalleryApp -Configuration $Configuration
}
