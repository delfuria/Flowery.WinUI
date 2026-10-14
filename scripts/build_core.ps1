#requires -Version 7.0
# Shared native Windows build functions.

$script:BuildResults = @()
$script:BuildStartTime = $null
$script:LogFile = $null
$script:VerboseOutput = $false
$script:MSBuildPath = $null
$script:SdkEnvironment = @{}

function Get-RepoRoot {
    return (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
}

function Resolve-BuildSdk {
    param([string]$SdkVersion, [string]$DotNetPath)

    $dotnetHost = if ($DotNetPath) {
        (Resolve-Path -LiteralPath $DotNetPath -ErrorAction Stop).Path
    } else {
        (Get-Command 'dotnet.exe' -CommandType Application -ErrorAction Stop).Source
    }
    if (-not (Test-Path -LiteralPath $dotnetHost -PathType Leaf)) {
        throw ('dotnet.exe not found: {0}' -f $dotnetHost)
    }

    Push-Location (Get-RepoRoot)
    try {
        if ([string]::IsNullOrWhiteSpace($SdkVersion)) {
            $versionOutput = & $dotnetHost '--version'
            if ($LASTEXITCODE -ne 0) { throw 'The dotnet host could not select an SDK from global.json.' }
            $SdkVersion = ($versionOutput | Select-Object -Last 1).Trim()
        }
        $sdkList = & $dotnetHost '--list-sdks'
        if ($LASTEXITCODE -ne 0) { throw 'The dotnet host could not list installed SDKs.' }
    }
    finally { Pop-Location }

    $installed = foreach ($line in $sdkList) {
        if ($line -match '^(?<Version>\S+)\s+\[(?<Root>.+)\]$' -and $Matches.Version -ceq $SdkVersion) {
            [pscustomobject]@{ Version = $Matches.Version; Root = $Matches.Root }
        }
    }
    $installed = @($installed)
    if ($installed.Count -ne 1) {
        throw ('SDK {0} is not uniquely installed for {1}. No other SDK will be substituted.' -f $SdkVersion, $dotnetHost)
    }
    $sdkDirectory = Join-Path $installed[0].Root $SdkVersion
    $sdksDirectory = Join-Path $sdkDirectory 'Sdks'
    $sdkProps = Join-Path $sdksDirectory 'Microsoft.NET.Sdk\Sdk\Sdk.props'
    $runtimeConfigPath = Join-Path $sdkDirectory 'dotnet.runtimeconfig.json'
    foreach ($path in @($sdkProps, $runtimeConfigPath)) {
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw ('SDK file not found: {0}' -f $path) }
    }
    $runtimeConfig = Get-Content -LiteralPath $runtimeConfigPath -Raw -Encoding utf8 | ConvertFrom-Json
    return [pscustomobject]@{
        Version = $SdkVersion
        HostPath = $dotnetHost
        HostDirectory = Split-Path -Path $dotnetHost -Parent
        SdkDirectory = $sdkDirectory
        SdksDirectory = $sdksDirectory
        Runtime = $runtimeConfig.runtimeOptions.framework
    }
}

function Invoke-NativeMSBuild {
    param([Parameter(Mandatory)][string[]]$Arguments)

    $processInfo = [System.Diagnostics.ProcessStartInfo]::new()
    $processInfo.FileName = $script:MSBuildPath
    $processInfo.WorkingDirectory = Get-RepoRoot
    $processInfo.UseShellExecute = $false
    $processInfo.CreateNoWindow = $true
    $processInfo.RedirectStandardOutput = $true
    $processInfo.RedirectStandardError = $true
    $processInfo.StandardOutputEncoding = [System.Text.Encoding]::UTF8
    $processInfo.StandardErrorEncoding = [System.Text.Encoding]::UTF8
    $null = $processInfo.Environment.Remove('MSBuildSDKsPath')
    foreach ($entry in $script:SdkEnvironment.GetEnumerator()) {
        $processInfo.Environment[$entry.Key] = $entry.Value
    }
    foreach ($argument in $Arguments) { $processInfo.ArgumentList.Add($argument) }

    $process = [System.Diagnostics.Process]::Start($processInfo)
    try {
        $stdoutTask = $process.StandardOutput.ReadToEndAsync()
        $stderrTask = $process.StandardError.ReadToEndAsync()
        $process.WaitForExit()
        return [pscustomobject]@{
            Stdout = $stdoutTask.GetAwaiter().GetResult()
            Stderr = $stderrTask.GetAwaiter().GetResult()
            ExitCode = $process.ExitCode
        }
    }
    finally { $process.Dispose() }
}

function Initialize-BuildEnvironment {
    param(
        [Parameter(Mandatory)][string]$HeadName,
        [Parameter(Mandatory)][string]$LogFileName,
        [ValidateSet('Debug', 'Release')][string]$Configuration = 'Debug',
        [switch]$VerboseOutput,
        [switch]$Cli,
        [string]$SdkVersion,
        [string]$DotNetPath
    )

    if (-not $IsWindows) {
        throw 'Flowery.WinUI builds require native Windows and PowerShell 7.'
    }

    $script:BuildStartTime = Get-Date
    $script:BuildResults = @()
    $script:VerboseOutput = $VerboseOutput
    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
    if (-not (Test-Path -LiteralPath $vswhere)) {
        throw ('Visual Studio locator not found: {0}' -f $vswhere)
    }

    $locatorArgs = @('-latest', '-requires', 'Microsoft.Component.MSBuild', '-property', 'installationPath')
    $vsPath = & $vswhere @locatorArgs
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($vsPath)) {
        throw 'Visual Studio with MSBuild is required.'
    }

    $script:MSBuildPath = Join-Path $vsPath 'MSBuild\Current\Bin\MSBuild.exe'
    if (-not (Test-Path -LiteralPath $script:MSBuildPath)) {
        throw ('MSBuild not found: {0}' -f $script:MSBuildPath)
    }

    $sdk = Resolve-BuildSdk -SdkVersion $SdkVersion -DotNetPath $DotNetPath
    $script:SdkEnvironment = @{
        DOTNET_MSBUILD_SDK_RESOLVER_SDKS_DIR = $sdk.SdksDirectory
        DOTNET_MSBUILD_SDK_RESOLVER_SDKS_VER = $sdk.Version
        DOTNET_MSBUILD_SDK_RESOLVER_CLI_DIR = $sdk.HostDirectory
        DOTNET_HOST_PATH = $sdk.HostPath
    }

    $logsDir = Join-Path (Get-RepoRoot) 'logs'
    if (-not (Test-Path -LiteralPath $logsDir)) {
        New-Item -ItemType Directory -Path $logsDir | Out-Null
    }
    $logStem = [System.IO.Path]::GetFileNameWithoutExtension($LogFileName)
    $stamp = Get-Date -Format 'yyyyMMdd-HHmmss-fff'
    $script:LogFile = Join-Path $logsDir ('{0}-{1}-sdk{2}-{3}.log' -f $logStem, $Configuration.ToLowerInvariant(), $sdk.Version, $stamp)
    New-Item -ItemType File -Path $script:LogFile -ErrorAction Stop | Out-Null

    $project = Join-Path (Get-RepoRoot) 'Flowery.WinUI\Flowery.WinUI.csproj'
    $metadataArgs = @($project, '-nologo', '-nodeReuse:false',
        '-getProperty:NETCoreSdkVersion,NETCoreSdkBundledVersionsProps,MicrosoftNETBuildTasksAssembly,MSBuildSDKsPath,MSBuildRuntimeType,MSBuildVersion,SdkResolverMSBuildTaskHostRuntimeVersion')
    $metadataResult = Invoke-NativeMSBuild -Arguments $metadataArgs
    $toolchainText = ('dotnet host: {0}{1}SDK directory: {2}{1}SDK host runtime: {3} {4}{1}MSBuild executable: {5}{1}{6}{1}{7}' -f
        $sdk.HostPath, [Environment]::NewLine, $sdk.SdkDirectory, $sdk.Runtime.name, $sdk.Runtime.version,
        $script:MSBuildPath, $metadataResult.Stdout, $metadataResult.Stderr)
    Add-Content -LiteralPath $script:LogFile -Value $toolchainText -Encoding utf8
    Write-Host $toolchainText
    if ($metadataResult.ExitCode -ne 0) { throw ('MSBuild SDK evaluation failed. Log: {0}' -f $script:LogFile) }
    $metadata = $metadataResult.Stdout | ConvertFrom-Json
    $expectedSdkProps = Join-Path $sdk.SdkDirectory 'Microsoft.NETCoreSdk.BundledVersions.props'
    $selectedSdkProps = [System.IO.Path]::GetFullPath([string]$metadata.Properties.NETCoreSdkBundledVersionsProps)
    if ($metadata.Properties.NETCoreSdkVersion -cne $sdk.Version -or
        -not [string]::Equals($selectedSdkProps, $expectedSdkProps, [StringComparison]::OrdinalIgnoreCase)) {
        throw ('MSBuild did not select the requested SDK {0}. Log: {1}' -f $sdk.Version, $script:LogFile)
    }
    Write-Host ('Building Flowery.WinUI {0} ({1})...' -f $HeadName, $Configuration) -ForegroundColor Cyan
}

function Invoke-ProjectBuild {
    param(
        [Parameter(Mandatory)][string]$Title,
        [Parameter(Mandatory)][string]$Project,
        [ValidateSet('Debug', 'Release')][string]$Configuration = 'Debug',
        [switch]$Rebuild,
        [string]$Framework
    )

    if (-not $script:MSBuildPath -or -not $script:LogFile) {
        throw 'Call Initialize-BuildEnvironment before building.'
    }
    if (-not (Test-Path -LiteralPath $Project)) {
        throw ('Build input not found: {0}' -f $Project)
    }

    $target = if ($Rebuild) { 'Rebuild' } else { 'Build' }
    $buildArgs = @($Project, ('-t:{0}' -f $target), '-restore', '-nologo', '-nodeReuse:false', '-verbosity:normal', '-m:1',
        ('-p:Configuration={0}' -f $Configuration), '-p:Platform=x64')
    if ($Framework) {
        $buildArgs += ('-p:TargetFramework={0}' -f $Framework)
    }

    return Invoke-MSBuildOperation -Title $Title -Arguments $buildArgs
}

function Invoke-MSBuildOperation {
    param([Parameter(Mandatory)][string]$Title, [Parameter(Mandatory)][string[]]$Arguments)

    Write-Host ('Running {0}...' -f $Title) -ForegroundColor Cyan
    $start = Get-Date
    $result = Invoke-NativeMSBuild -Arguments $Arguments
    $stdout = $result.Stdout
    $stderr = $result.Stderr
    $exitCode = $result.ExitCode

    $output = ('=== {0} ==={1}{2}{1}{3}{1}' -f $Title, [Environment]::NewLine, $stdout, $stderr)
    Add-Content -LiteralPath $script:LogFile -Value $output -Encoding utf8
    if ($script:VerboseOutput) {
        Write-Host $output
    }
    $warningPattern = '\bwarning\s+[A-Za-z]+\d+:'
    $warnings = @($stdout -split '\r?\n' |
        Where-Object { $_ -match $warningPattern } |
        ForEach-Object { $_.Trim() } |
        Select-Object -Unique).Count
    $script:BuildResults += [pscustomobject]@{
        Project = $Title
        ExitCode = $exitCode
        Duration = (Get-Date) - $start
        Warnings = $warnings
    }
    return $exitCode -eq 0
}

function Write-BuildFailure {
    param([string]$ProjectName)

    Write-Host ('Build failed: {0}' -f $ProjectName) -ForegroundColor Red
    if (-not $script:VerboseOutput) {
        $pattern = '(error|warning) [A-Z]+\d+:'
        Select-String -LiteralPath $script:LogFile -Pattern $pattern |
            ForEach-Object { Write-Host $_.Line }
    }
    Write-Host ('Log: {0}' -f $script:LogFile)
}

function Write-BuildSummary {
    foreach ($result in $script:BuildResults) {
        $status = if ($result.ExitCode -eq 0) { 'OK' } else { 'FAILED' }
        Write-Host ('[{0}] {1}: {2:hh\:mm\:ss}, {3} warnings' -f $status, $result.Project, $result.Duration, $result.Warnings)
    }
    Write-Host ('Log: {0}' -f $script:LogFile)
}

function Start-GalleryApp {
    param(
        [string]$ExePattern = 'Flowery.WinUI.Gallery.exe',
        [ValidateSet('Debug', 'Release')][string]$Configuration = 'Debug',
        [string]$Framework = 'net10.0-windows10.0.19041',
        [switch]$Wait
    )

    $relative = 'bin\{0}\Flowery.WinUI.Gallery\{1}\win-x64\{2}' -f $Configuration, $Framework, $ExePattern
    $exePath = Join-Path (Get-RepoRoot) $relative
    if (-not (Test-Path -LiteralPath $exePath)) {
        throw ('Gallery executable not found: {0}' -f $exePath)
    }
    $processArgs = @{
        FilePath = $exePath
        WorkingDirectory = Split-Path -Path $exePath -Parent
        PassThru = $true
    }
    if ($Wait) {
        $processArgs.Wait = $true
    }
    $process = Start-Process @processArgs
    Write-Host ('Started Gallery process {0}: {1}' -f $process.Id, $exePath)
}
