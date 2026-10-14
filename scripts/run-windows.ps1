#requires -Version 7.0
param(
    [ValidateSet('Debug', 'Release')][string]$Configuration = 'Debug',
    [switch]$Rebuild,
    [switch]$NoWait,
    [string]$SdkVersion,
    [string]$DotNetPath
)

$ErrorActionPreference = 'Stop'
$buildScript = Join-Path $PSScriptRoot 'build_windows.ps1'
& $buildScript -Configuration $Configuration -Rebuild:$Rebuild -NoRun -SdkVersion $SdkVersion -DotNetPath $DotNetPath
if ($LASTEXITCODE -and $LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
. (Join-Path $PSScriptRoot 'build_core.ps1')
Start-GalleryApp -Configuration $Configuration -Wait:(-not $NoWait)
