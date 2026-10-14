#requires -Version 7.0
param(
    [ValidateSet('Debug', 'Release')][string]$Configuration = 'Release',
    [ValidateSet('win-x64')][string]$RuntimeIdentifier = 'win-x64',
    [ValidateSet('Gallery', 'Kiosk')][string]$Application = 'Gallery',
    [bool]$SelfContained = $true,
    [string]$OutputDir = 'publish',
    [switch]$VerboseOutput,
    [string]$SdkVersion,
    [string]$DotNetPath
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'build_core.ps1')
$repoRoot = Get-RepoRoot
$projectName = 'Flowery.WinUI.{0}' -f $Application
$project = Join-Path $repoRoot ('{0}\{0}.csproj' -f $projectName)
if (-not [System.IO.Path]::IsPathRooted($OutputDir)) {
    $OutputDir = Join-Path $repoRoot $OutputDir
}
Initialize-BuildEnvironment -HeadName 'publish' -LogFileName 'windows-publish' -Configuration $Configuration -VerboseOutput:$VerboseOutput -SdkVersion $SdkVersion -DotNetPath $DotNetPath

$publishDirectory = [IO.Path]::TrimEndingDirectorySeparator([IO.Path]::GetFullPath($OutputDir)) + [IO.Path]::DirectorySeparatorChar
$publishArgs = @($project, '-t:Publish', '-restore', '-nologo', '-nodeReuse:false', '-m:1',
    ('-p:Configuration={0}' -f $Configuration), ('-p:RuntimeIdentifier={0}' -f $RuntimeIdentifier),
    ('-p:SelfContained={0}' -f $SelfContained.ToString().ToLowerInvariant()), '-p:Platform=x64',
    '-p:WindowsPackageType=None', ('-p:PublishDir={0}' -f $publishDirectory))
if (-not (Invoke-MSBuildOperation -Title ('Publish {0}' -f $projectName) -Arguments $publishArgs)) {
    Write-BuildFailure ('{0} publish' -f $projectName)
    exit 1
}
Write-BuildSummary
Write-Host ('Published {0}: {1}' -f $projectName, $OutputDir)
Write-Host ('Log: {0}' -f $script:LogFile)
