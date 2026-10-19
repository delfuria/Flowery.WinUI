#requires -Version 7.0
param(
    [ValidateSet('Debug', 'Release')][string]$Configuration = 'Release',
    [string]$SdkVersion,
    [string]$DotNetPath
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'build_core.ps1')
Initialize-BuildEnvironment -HeadName 'NuGet audit' -LogFileName 'nuget-audit' -Configuration $Configuration -SdkVersion $SdkVersion -DotNetPath $DotNetPath

Push-Location (Get-RepoRoot)
try {
    $gitArguments = @('ls-files', '--', '*.csproj')
    $projects = @(& git @gitArguments)
    if ($LASTEXITCODE -ne 0 -or $projects.Count -eq 0) {
        throw 'Could not enumerate the tracked C# projects.'
    }

    foreach ($project in $projects) {
        if (-not (Test-Path -LiteralPath $project -PathType Leaf)) {
            throw ('Audit project not found: {0}' -f $project)
        }
        $auditArguments = @(
            $project, '-t:Restore', '-nologo', '-nodeReuse:false', '-verbosity:normal', '-m:1',
            ('-p:Configuration={0}' -f $Configuration), '-p:Platform=x64',
            '-p:RestoreForceEvaluate=true', '-p:NuGetAudit=true', '-p:NuGetAuditMode=all', '-p:NuGetAuditLevel=low',
            '-warnaserror:NU1900,NU1901,NU1902,NU1903,NU1904,NU1905'
        )
        if (-not (Invoke-MSBuildOperation -Title $project -Arguments $auditArguments)) {
            Write-BuildFailure $project
            exit 1
        }
    }
}
finally { Pop-Location }

Write-BuildSummary
Write-Host ('Audited {0} C# projects, including transitive NuGet dependencies.' -f $projects.Count)
