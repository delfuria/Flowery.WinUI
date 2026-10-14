# Flowery.WinUI scripts

The active solution targets native Windows with WinUI 3 and the Windows App SDK.
All commands below run in PowerShell 7 from the repository root.

## Requirements

- Windows 10 version 2004 or later.
- The .NET SDK selected by `global.json`.
- Visual Studio with MSBuild and the Windows SDK.
- Microsoft Edge WebView2 Runtime for the shader-effect views.

The build uses Microsoft.WindowsAppSDK 1.7.250909003.
It requires the native Windows toolchain listed above.

## Select the .NET SDK

The projects target `net10.0-windows10.0.19041`.
The SDK supplies the compiler and build tools; selecting SDK 11 does not change the application's .NET 10 target runtime.

`global.json` sets a minimum SDK of `10.0.203`, allows major-version roll-forward, and permits prerelease SDKs.
Use `-SdkVersion` to select one exact installed SDK.
Use `-DotNetPath` when that SDK belongs to a separate native `dotnet.exe` installation.
These options apply to the build, run, test, package, and publish scripts.
An explicit SDK selection fails if that version is unavailable.

```powershell
./scripts/build_all.ps1 -Configuration Release -NoRun -SdkVersion 10.0.402

$dotnetPath = Join-Path $env:LOCALAPPDATA 'Microsoft\dotnet\dotnet.exe'
./scripts/build_all.ps1 -Configuration Release -NoRun -SdkVersion 11.0.100-rc.2.26504.105 -DotNetPath $dotnetPath
./scripts/test_windows.ps1 -Configuration Release -SkipBuild -Monitor 2 -WindowRegion LowerHalf
./scripts/test_kiosk.ps1 -Configuration Release -SkipBuild -Monitor 2 -WindowRegion LowerHalf
```

With `-SkipBuild`, the test scripts run the existing binaries from the matching configuration.
Build that configuration with the selected SDK first.
The SDK 11 RC2 path is an early toolchain test, not a claim of formal Windows App SDK support for a .NET 11 target.
Native Visual Studio MSBuild, the selected SDK, and the Windows XAML compiler remain part of the build.
Each build log records the selected SDK and resolved tool paths without changing the system environment.

## Build the solution

```powershell
./scripts/build_all.ps1 -Configuration Debug -NoRun
./scripts/build_all.ps1 -Configuration Release -NoRun
```

The solution includes the core library, Kanban, Win2D, integrations, Gallery, Kiosk, and native test projects.
Builds use the normal `bin/<Configuration>/<Project>/<TargetFramework>/win-x64/` output directories.

The scripts accept `-Rebuild` and `-VerboseOutput`.
Each build creates a new log in `logs/`.
The log name includes the build type, configuration, SDK version, and timestamp.

## Build or run the Gallery

```powershell
./scripts/build_windows.ps1 -Configuration Debug -NoRun
./scripts/run-windows.ps1 -Configuration Debug
./scripts/run-windows.ps1 -Configuration Debug -NoWait
```

The build script starts the Gallery unless you specify `-NoRun`.
The run script waits for the Gallery to close unless you specify `-NoWait`.

## Build or run the Kiosk

```powershell
./scripts/build_kiosk.ps1 -Configuration Debug -NoRun
./scripts/run-kiosk.ps1 -Configuration Debug -SkipBuild -Monitor 2 -WindowRegion LowerHalf
./scripts/run-kiosk.ps1 -Configuration Debug -SkipBuild -Manual -Monitor 2 -WindowRegion LowerHalf
```

The Kiosk has six slides: Authentication, Data Visualization, Communication, Enhanced Controls, Runtime Scaling, and Background Patterns.
Each slide has its own theme, title, and keywords.
The progress indicator runs over the eight-second slide interval.
Autoplay advances through the slides unless you specify `-Manual`, which passes `--manual` to the executable.

Right, Space, or N advances to the next slide.
Left or P returns to the previous slide.
The run script waits for the application unless you specify `-NoWait`.
Its default placement is the lower half of monitor 2.

After the matching solution build, run the native Kiosk smoke checks:

```powershell
./scripts/test_kiosk.ps1 -Configuration Debug -SkipBuild -Monitor 2 -WindowRegion LowerHalf
./scripts/test_kiosk.ps1 -Configuration Release -SkipBuild -Monitor 2 -WindowRegion LowerHalf
```

The script launches the native Kiosk with an isolated state directory and an absolute XML report path.
Reports are in `TestResults/Kiosk/<Configuration>/<run>/kiosk-smoke.xml`.
Logs are in `logs/kiosk-tests-<Configuration>-<run>.log`.
The script checks process exit, unique case names, report counts, and individual outcomes.

## Run native tests

```powershell
./scripts/test_windows.ps1 -Configuration Debug
./scripts/test_windows.ps1 -Configuration Release
```

If the matching solution build is complete, use `-SkipBuild`:

```powershell
./scripts/test_windows.ps1 -Configuration Debug -SkipBuild
```

For the lower half of display 2, use:

```powershell
./scripts/test_windows.ps1 -Configuration Debug -SkipBuild -Monitor 2 -WindowRegion LowerHalf
```

The Gallery resolves the Windows display device before it shows the window.
If the requested device is absent, the run fails without a fallback to another display.

The Windows runner starts the Gallery and executes tests on its WinUI dispatcher.
The suite includes the original control construction and global neumorphic tests.
It also includes the existing runtime tests and native resource tests.

The test script uses the matching Gallery and runner executables from the normal build output.
It sets `FLOWERY_GALLERY_EXE_PATH` and a separate `FLOWERY_TEST_RESULTS_ROOT` for the runner.
The script writes test reports under `TestResults/` and process logs under `logs/`.

## Create NuGet packages

```powershell
./scripts/build_nuget.ps1 -Configuration Release
```

This script builds the solution and creates packages in `nupkg/`.
It creates packages for Flowery.WinUI, Flowery.WinUI.Kanban, Flowery.WinUI.Win2D, and Flowery.Integrations.WinUI.
Use `-OutputDir` to select another package directory.
After a successful build of the same configuration, use `-SkipBuild` to package those binaries.

## Publish a Windows application

```powershell
./scripts/publish_windows.ps1 -Configuration Release
./scripts/publish_windows.ps1 -Configuration Release -Application Kiosk -OutputDir publish-kiosk
```

`-Application` accepts `Gallery` and `Kiosk`; the default is `Gallery`.
The default Gallery output is `publish/`.
The Kiosk example writes native Windows x64 output to `publish-kiosk/`.
The default output includes the .NET runtime and the Windows App SDK runtime.
The publish script uses the native XAML and PRI resource pipeline.

## Continuous integration

The CI workflow is configured to build and check Gallery and Kiosk in Debug and Release with SDK `10.0.402` and `11.0.100-rc.2.26504.105`.
The release workflow creates the native packages, Gallery output, and `Flowery.WinUI.Kiosk-Windows-x64.zip`.
Release publication remains a manual workflow action, with SDK `10.0.402` selected by default and SDK 11 RC2 available as an option.

The supported projects and scripts target native Windows only.
The kiosk workflow and shader-effect examples have native Windows host implementations with their functions preserved.
See [the migration record](../PORTING.md) for dated build results and runtime verification scope.
