#requires -Version 7.0
param(
    [Parameter(Mandatory)][string]$InstallerPath,
    [Parameter(Mandatory)][string]$AppDirectory,
    [string]$OutputDirectory,
    [string]$ExpectedVersion,
    [switch]$LaunchGui
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repoRoot = Split-Path -Parent $PSScriptRoot
$installer = (Resolve-Path -LiteralPath $InstallerPath).Path
$appSource = (Resolve-Path -LiteralPath $AppDirectory).Path
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) { $OutputDirectory = Join-Path $repoRoot 'TestResults/PhotosInstaller' }
$root = [IO.Path]::GetFullPath($OutputDirectory)
$allowedRoots = @((Join-Path $repoRoot 'TestResults'))
if ($env:RUNNER_TEMP) { $allowedRoots += Join-Path $env:RUNNER_TEMP 'Flowery/Tests' }
if (-not ($allowedRoots | Where-Object { $root.StartsWith([IO.Path]::GetFullPath($_) + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase) -or $root -eq [IO.Path]::GetFullPath($_) })) {
    throw 'Installer tests must stay in TestResults or the runner test directory.'
}
if ([string]::IsNullOrWhiteSpace($ExpectedVersion)) {
    [xml]$props = Get-Content -LiteralPath (Join-Path $repoRoot 'Directory.Build.props') -Raw
    $ExpectedVersion = [string]$props.Project.PropertyGroup.Version.InnerText
}
$appExe = 'Flowery.WinUI.Photos.exe'
$registryPath = 'Software\Microsoft\Windows\CurrentVersion\Uninstall\Flowery.WinUI.Photos'
$registry = [Microsoft.Win32.RegistryKey]::OpenBaseKey([Microsoft.Win32.RegistryHive]::CurrentUser, [Microsoft.Win32.RegistryView]::Registry64)
$existing = $registry.OpenSubKey($registryPath)
if ($existing) { $existing.Dispose(); $registry.Dispose(); throw 'An installed Flowery Photos already exists. Use a clean test account.' }
$shortcut = Join-Path ([Environment]::GetFolderPath('Programs')) 'Flowery Photos/Flowery Photos.lnk'
if (Test-Path -LiteralPath $shortcut) { $registry.Dispose(); throw 'An existing Flowery Photos shortcut must not be replaced by this test.' }
$run = Join-Path $root ([Guid]::NewGuid().ToString('N'))
$install = Join-Path $run 'Application Files'
$state = Join-Path $run 'State'
New-Item -ItemType Directory -Path $state -Force | Out-Null
$sentinel = Join-Path $state 'settings-preserved.txt'
Set-Content -LiteralPath $sentinel -Value 'Keep these user settings.'
$results = [Collections.Generic.List[string]]::new()
$installed = $false
$initialStateRoot = $env:FLOWERY_STATE_ROOT

function Install([int]$Language = 0) {
    $arguments = '/S '
    if ($Language -gt 0) { $arguments += '/LANGUAGE={0} ' -f $Language }
    # NSIS requires /D last, without quotes, including when its value contains spaces.
    $arguments += '/D=' + $install
    $process = Start-Process -FilePath $installer -ArgumentList $arguments -PassThru -WindowStyle Hidden
    if (-not $process.WaitForExit(180000)) { throw 'Installer did not finish within 180 seconds.' }
    if ($process.ExitCode -ne 0) { throw ('Installer failed with exit code {0}.' -f $process.ExitCode) }
    $script:installed = $true
}

function Invoke-Expecting([string]$FilePath, [string]$Arguments, [int]$ExitCode, [string]$Message) {
    $process = Start-Process -FilePath $FilePath -ArgumentList $Arguments -PassThru -WindowStyle Hidden
    try {
        if (-not $process.WaitForExit(180000) -or $process.ExitCode -ne $ExitCode) { throw $Message }
    }
    finally { $process.Dispose() }
}

function Assert-Payload {
    foreach ($file in (Get-ChildItem -LiteralPath $appSource -Recurse -File | Where-Object Extension -ne '.pdb')) {
        $relative = [IO.Path]::GetRelativePath($appSource, $file.FullName)
        $target = Join-Path $install $relative
        if (-not (Test-Path -LiteralPath $target) -or (Get-FileHash -LiteralPath $target).Hash -ne (Get-FileHash -LiteralPath $file.FullName).Hash) {
            throw ('Installed payload mismatch: {0}' -f $target)
        }
    }
    if (Get-ChildItem -LiteralPath $install -Filter '*.pdb' -Recurse -File) { throw 'Debug symbols were installed.' }
}

function Assert-Registration {
    $key = $registry.OpenSubKey($registryPath)
    if (-not $key) { throw 'Per-user uninstall registration missing.' }
    try {
        if ($key.GetValue('InstallLocation') -ne $install -or $key.GetValue('DisplayVersion') -ne $ExpectedVersion -or $key.GetValue('DisplayIcon') -ne (Join-Path $install $appExe)) {
            throw 'Unexpected per-user installation metadata.'
        }
    }
    finally { $key.Dispose() }
    if (-not (Test-Path -LiteralPath $shortcut)) { throw 'Start menu shortcut missing.' }
    $shell = New-Object -ComObject WScript.Shell
    $link = $shell.CreateShortcut($shortcut)
    if ($link.TargetPath -ne (Join-Path $install $appExe)) { throw 'Start menu shortcut points to the wrong executable.' }
    if ($link.IconLocation -ne ('{0},0' -f (Join-Path $install $appExe))) { throw 'Start menu shortcut does not use the application icon.' }
}

function Get-InstalledHashes {
    $hashes = @{}
    foreach ($file in Get-ChildItem -LiteralPath $install -Recurse -File) {
        $hashes[[IO.Path]::GetRelativePath($install, $file.FullName)] = (Get-FileHash -LiteralPath $file.FullName).Hash
    }
    return $hashes
}

function Assert-InstalledHashes([hashtable]$Before) {
    $after = Get-InstalledHashes
    if ($after.Count -ne $Before.Count) { throw 'Failed update changed the installed file set.' }
    foreach ($entry in $Before.GetEnumerator()) {
        if ($after[$entry.Key] -ne $entry.Value) { throw ('Failed update changed: {0}' -f $entry.Key) }
    }
    Assert-Registration
}

function Assert-FailedUpdatePreservedFiles([hashtable]$Before) {
    Invoke-Expecting $installer ('/S /D=' + $install) 1 'Blocked update did not fail with exit code 1.'
    Assert-InstalledHashes $Before
}

function Uninstall {
    $exe = Join-Path $install 'Uninstall.exe'
    $process = Start-Process -FilePath $exe -ArgumentList '/S' -PassThru -WindowStyle Hidden
    if (-not $process.WaitForExit(180000)) { throw 'Uninstaller did not finish within 180 seconds.' }
    $watch = [Diagnostics.Stopwatch]::StartNew()
    do {
        $key = $registry.OpenSubKey($registryPath)
        $registered = $null -ne $key
        if ($key) { $key.Dispose() }
        if (-not $registered -and -not (Test-Path -LiteralPath $exe)) { break }
        Start-Sleep -Milliseconds 200
    } while ($watch.Elapsed.TotalSeconds -lt 30)
    if ($registered -or (Test-Path -LiteralPath $exe)) { throw 'Uninstall did not finish removing its registration and executable.' }
    $script:installed = $false
}

try {
    $env:FLOWERY_STATE_ROOT = $state
    Install
    Assert-Payload
    Assert-Registration
    $entries = [IO.File]::ReadAllLines((Join-Path $install 'files.lst'))
    if (@($entries | Sort-Object -Unique).Count -ne $entries.Count) { throw 'Payload files were recorded more than once.' }
    $results.Add('Setup installs the payload with a single manifest, per-user registration and a Start menu shortcut with the application icon.')

    $custom = Join-Path $install 'user-file.txt'
    Set-Content -LiteralPath $custom -Value 'This file does not belong to the installer.'
    $obsolete = Join-Path $install 'obsolete-owned-file.txt'
    Set-Content -LiteralPath $obsolete -Value 'An obsolete file from a previous version.'
    Add-Content -LiteralPath (Join-Path $install 'files.lst') -Value 'obsolete-owned-file.txt' -Encoding Unicode
    Install
    Assert-Payload
    Assert-Registration
    if (Test-Path -LiteralPath $obsolete) { throw 'Upgrade retained an obsolete owned file.' }
    if (-not (Test-Path -LiteralPath $custom)) { throw 'Upgrade removed an unowned file.' }
    $results.Add('Upgrade removes obsolete payload and preserves unowned files.')

    # A recorded relative path must not traverse a junction into unrelated data.
    $external = Join-Path $run 'ExternalFiles'
    $linked = Join-Path $install 'linked-data'
    $retiredLink = Join-Path $run 'retired-link'
    foreach ($path in @($external, $linked, $retiredLink)) {
        if (-not ([IO.Path]::GetFullPath($path)).StartsWith($run + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Link fixture escaped the test run.' }
        if (Test-Path -LiteralPath $path) { throw 'Link fixture already exists.' }
    }
    New-Item -ItemType Directory -Path (Join-Path $external 'nested') -Force | Out-Null
    $externalFile = Join-Path $external 'nested/preserve.txt'
    Set-Content -LiteralPath $externalFile -Value 'This data is outside the installation.'
    $externalHash = (Get-FileHash -LiteralPath $externalFile).Hash
    New-Item -ItemType Junction -Path $linked -Target $external | Out-Null
    $manifestPath = Join-Path $install 'files.lst'
    $manifestBytes = [IO.File]::ReadAllBytes($manifestPath)
    try {
        $entries = @('linked-data\nested\preserve.txt') + [IO.File]::ReadAllLines($manifestPath)
        [IO.File]::WriteAllLines($manifestPath, $entries, [Text.Encoding]::Unicode)
        $before = Get-InstalledHashes
        $commands = @(@($installer, ('/S /D=' + $install)), @((Join-Path $install 'Uninstall.exe'), ('/S _?=' + $install)))
        foreach ($command in $commands) {
            Invoke-Expecting $command[0] $command[1] 1 'Setup/removal accepted a manifest path through a junction.'
            if (-not (Test-Path -LiteralPath $externalFile) -or (Get-FileHash -LiteralPath $externalFile).Hash -ne $externalHash) { throw 'Setup/removal changed data outside the installation.' }
            if ((Get-Item -LiteralPath $linked).LinkType -ne 'Junction') { throw 'Setup/removal changed the junction.' }
            Assert-InstalledHashes $before
        }
    }
    finally {
        [IO.File]::WriteAllBytes($manifestPath, $manifestBytes)
        if (Test-Path -LiteralPath $linked) { Move-Item -LiteralPath $linked -Destination $retiredLink }
    }
    $results.Add('Setup and removal reject junction traversal, preserving external data, the link, and the installed payload.')

    # A late lock must restore the earlier renamed files, including their original bytes.
    $oldAsset = Join-Path $install 'Assets/app_icon.svg'
    Add-Content -LiteralPath $oldAsset -Value '<!-- previous installed version -->'
    $before = Get-InstalledHashes
    $locked = [IO.File]::Open((Join-Path $install $appExe), [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
    try { Assert-FailedUpdatePreservedFiles $before }
    finally { $locked.Dispose() }
    $results.Add('A locked application executable aborts the update and restores every installed file and manifest byte-for-byte.')

    # This unowned file blocks a late destination directory, after payload publication has started.
    $lateDirectory = Get-ChildItem -LiteralPath $install -Directory | Sort-Object Name | Select-Object -Last 1
    if (-not $lateDirectory -or $lateDirectory.Name -eq 'Assets') { throw 'No late payload directory available for the publication test.' }
    $savedDirectory = Join-Path $run 'saved-late-directory'
    Move-Item -LiteralPath $lateDirectory.FullName -Destination $savedDirectory
    Set-Content -LiteralPath $lateDirectory.FullName -Value 'Preserve the unowned destination blocker.'
    try {
        $before = Get-InstalledHashes
        Assert-FailedUpdatePreservedFiles $before
    }
    finally {
        Move-Item -LiteralPath $lateDirectory.FullName -Destination (Join-Path $run 'destination-blocker.txt')
        Move-Item -LiteralPath $savedDirectory -Destination $lateDirectory.FullName
        Copy-Item -LiteralPath (Join-Path $appSource 'Assets/app_icon.svg') -Destination $oldAsset -Force
    }
    Assert-Payload
    $results.Add('A late publication failure rolls back the new payload while preserving old files and unowned blockers.')

    # A running setup owns the shared mutex. Competing setup and uninstall processes must fail before changing any installed bytes.
    $setupMutex = [Threading.Mutex]::new($true, 'Global\Flowery.WinUI.Photos.Setup')
    try {
        $before = Get-InstalledHashes
        $commands = @(@($installer, ('/S /D=' + $install)), @((Join-Path $install 'Uninstall.exe'), ('/S _?=' + $install)))
        foreach ($command in $commands) {
            Invoke-Expecting $command[0] $command[1] 1618 'Concurrent setup/removal was not rejected with exit code 1618.'
            Assert-InstalledHashes $before
        }
    }
    finally {
        $setupMutex.ReleaseMutex()
        $setupMutex.Dispose()
    }
    $results.Add('Setup and uninstall are rejected while another setup owns the installation mutex, preserving every byte.')

    if ($LaunchGui) {
        $exe = Join-Path $install $appExe
        $process = Start-Process -FilePath $exe -WorkingDirectory (Split-Path $exe) -PassThru
        try {
            $watch = [Diagnostics.Stopwatch]::StartNew()
            do { Start-Sleep -Milliseconds 200; $process.Refresh() } while (-not $process.HasExited -and $process.MainWindowHandle -eq 0 -and $watch.Elapsed.TotalSeconds -lt 30)
            if ($process.HasExited -or $process.MainWindowHandle -eq 0 -or -not $process.Responding) { throw 'Installed application did not open a responsive window.' }
            $results.Add('Installed application opened a responsive main window.')
        }
        finally {
            $process.Refresh()
            if (-not $process.HasExited) {
                [void]$process.CloseMainWindow()
                if (-not $process.WaitForExit(10000)) { throw 'Installed test application did not close normally.' }
            }
            $process.Dispose()
        }
    }

    $manifestPath = Join-Path $install 'files.lst'
    $manifestBytes = [IO.File]::ReadAllBytes($manifestPath)
    try {
        [IO.File]::WriteAllText($manifestPath, "..\State\settings-preserved.txt`r`n", [Text.Encoding]::Unicode)
        Invoke-Expecting (Join-Path $install 'Uninstall.exe') ('/S _?=' + $install) 1 'Uninstaller accepted an unsafe manifest path.'
        if (-not (Test-Path -LiteralPath $sentinel) -or -not (Test-Path -LiteralPath (Join-Path $install $appExe))) { throw 'Unsafe manifest changed files.' }
    }
    finally { [IO.File]::WriteAllBytes($manifestPath, $manifestBytes) }
    $results.Add('Uninstaller rejects path traversal in a modified payload manifest.')

    $languages = @{
        1033 = 'en'; 1031 = 'de'; 1036 = 'fr'; 1034 = 'es'; 1040 = 'it'; 1041 = 'ja'
        1042 = 'ko'; 2052 = 'zh-CN'; 1025 = 'ar'; 1055 = 'tr'; 1058 = 'uk'; 1037 = 'he'
    }
    foreach ($language in $languages.GetEnumerator()) {
        $env:FLOWERY_STATE_ROOT = Join-Path $run ('Languages/' + $language.Value)
        Install $language.Key
        $preference = Join-Path $env:FLOWERY_STATE_ROOT 'language.state'
        if ((Get-Content -LiteralPath $preference -Raw).Trim() -ne $language.Value) { throw 'Installer did not initialize the selected language.' }
    }
    $preference = Join-Path $env:FLOWERY_STATE_ROOT 'language.state'
    $before = (Get-FileHash -LiteralPath $preference).Hash
    Install 1036
    if ((Get-FileHash -LiteralPath $preference).Hash -ne $before) { throw 'Installer overwrote an existing language preference.' }
    $results.Add('All 12 installer languages initialize new preferences; existing preferences remain unchanged.')

    Uninstall
    if ((Test-Path -LiteralPath $shortcut) -or (Test-Path -LiteralPath (Join-Path $install $appExe))) { throw 'Uninstall retained a shortcut or the application executable.' }
    if (-not (Test-Path -LiteralPath $custom) -or -not (Test-Path -LiteralPath $sentinel)) { throw 'Uninstall removed user files or settings.' }
    $results.Add('Uninstall removed the payload, shortcut and registration, preserving user files and settings.')
}
finally {
    $results | Set-Content -LiteralPath (Join-Path $run 'results.txt')
    if ($installed -and (Test-Path -LiteralPath (Join-Path $install 'Uninstall.exe'))) { Uninstall }
    $env:FLOWERY_STATE_ROOT = $initialStateRoot
    $registry.Dispose()
}
$results
Write-Output ('Installer smoke test passed: {0}' -f $run)
