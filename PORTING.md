# Flowery.WinUI migration record

## Source snapshot at clone time

- Source: the original local checkout. The exact path is recorded in the local Git snapshot metadata.
- Source branch: `main`.
- Source commit: `39ddf3f89c6db0ec23b93573a33455bd71135867`.
- The source repository was not shallow.
- The source had no staged, unstaged, or untracked files.
- No submodule or Git LFS configuration was present.
- The independent clone preserved all 22 source references, including branches, tags, and checkpoint references.
- Before migration, all 930 tracked files matched their source SHA-256 hashes.
- The clone has no Git alternates or shared object files.
- The `source` remote has a disabled push URL. `push.default` is `nothing`.
- At the user's request, the private GitHub repository `tobitege/Flowery.WinUI` was created and configured as `origin`.
- No new commits or pushes were made.

Core namespaces such as `Flowery.Controls` remain unchanged.
At the user's request, project-specific namespaces now use `Flowery.WinUI.Kanban` and `Flowery.Integrations.WinUI`.
Project names, assembly names, package names, and resource paths use `Flowery.WinUI`.
The source hash inventory is in the local Git metadata at `.git/port-source-snapshot/`.

## Baseline

The unchanged Windows target was built through `scripts/build_windows.ps1 -Configuration Debug -NoRun` in the target copy.
The build failed with `MSB3030` in the Gallery project.
Its manual resource-copy target expected `Themes/DaisyKanban.xbf` in the Kanban output directory, but that file did not exist.
The complete log is `logs/baseline-windows-debug.log`.

## Native dependencies

The migration retains the baseline stable versions of the Windows App SDK and Windows SDK build tools.
The active projects use `Microsoft.NET.Sdk` and a Windows-only target framework.
The native XAML compiler builds the resource dictionaries.
SkiaSharp remains an independent image-processing dependency.

| Dependency | Version |
| --- | --- |
| Microsoft.WindowsAppSDK | 1.7.250909003 |
| Microsoft.Windows.SDK.BuildTools | 10.0.28000.1839 |
| Microsoft.Graphics.Win2D | 1.3.2 |
| SkiaSharp | 3.119.2 |

The nine active projects target `net10.0-windows10.0.19041` and x64.
The solution retains core, Kanban, integrations, Win2D, Gallery.Core, Gallery, RuntimeTests, WindowsRunner, and Kiosk.
The five original unit-test methods run inside RuntimeTests on the native WinUI dispatcher.
The current source tree targets native Windows.
The shader-effect presets, gallery examples, and kiosk workflow now have native Windows host implementations.
The shader host uses WinUI 3 WebView2 and retains the existing shader source.
The catalogs contain 50 registered effects and 100 external transition shaders.
The Kiosk retains six slides, per-slide themes and keywords, progress animation, eight-second autoplay, and manual navigation.
The dated build and runtime results are recorded below.

Former framework, toolkit, development-server, resizetizer, and responsive-layout dependencies are removed from the native build.
Native Composition and SkiaSharp alpha masks provide the outer and inner shadows.
Native StackPanel and AdaptiveTrigger replace the used toolkit layouts.
The existing SVG patterns load through native SvgImageSource.

## Final native validation

Validation date: 2026-10-14.
All solution builds below include the nine native projects.
SDK selection changes the compiler and build toolchain; the target remains `net10.0-windows10.0.19041`.
The build scripts verify the exact SDK version and its imported SDK properties through native MSBuild.

| Toolchain and check | Result | Evidence |
| --- | --- | --- |
| SDK 11 RC2, Debug solution build | Passed, 2 warnings, 0 errors | [Build log](logs/all-build-debug-sdk11.0.100-rc.2.26504.105-20261014-205809-607.log) |
| SDK 11 RC2, Debug native tests | 168 passed, 0 failed | [Native XML](TestResults/Debug/20261014-205822-097-b29e8c60/native-runtime-360ef0bce46e4bfbbde5db9dc90892e7/runtime-tests-windows.xml) |
| SDK 11 RC2, Release solution build | Passed, 2 warnings, 0 errors | [Build log](logs/all-build-release-sdk11.0.100-rc.2.26504.105-20261014-205659-997.log) |
| SDK 11 RC2, Release native tests | 168 passed, 0 failed | [Native XML](TestResults/Release/20261014-205713-306-e07e94c8/native-runtime-59f324afe6224885a8b0fc2e6123d487/runtime-tests-windows.xml) |
| SDK 10.0.402, Release solution build | Passed, 0 warnings, 0 errors | [Build log](logs/all-build-release-sdk10.0.402-20261014-210008-766.log) |
| SDK 10.0.402, Release native tests | 168 passed, 0 failed | [Native XML](TestResults/Release/20261014-210147-337-03c77c8a/native-runtime-10da8cf6fd8441f49ce934c19b8cbd22/runtime-tests-windows.xml) |
| SDK 11 pack of SDK 11 Release artifacts | Four packages, 0 warnings and errors | [Pack log](logs/nuget-pack-release-sdk11.0.100-rc.2.26504.105-20261014-205758-286.log) |
| SDK 11 repack of normal SDK 10 Release artifacts | Four packages, 0 warnings and errors | [Pack log](logs/nuget-pack-release-sdk11.0.100-rc.2.26504.105-20261014-210229-250.log) |
| Package contents | Correct product IDs, repository URL, native framework and dependencies; all four packages contain PRI resources | `.git/name-audit/final-native-packages.json` |
| Original source protection | All 934 snapshot hashes match: 930 tracked files and four Git metadata files; tracked source status is clean | `.git/port-source-snapshot/` |

SDK 11 RC2 is `11.0.100-rc.2.26504.105`.
The earlier SDK 11 builds above reported two `CsWinRT1032` warnings in the ThemeListCustomization tests, where collection expressions targeted `IReadOnlyCollection<string>`.
Those diagnostics concerned trimming and AOT compatibility in WinRT scenarios.
The two test inputs now use concrete `string[]` arrays with the same values and assertions.
Neither follow-up build reports `CsWinRT1032` in those tests.

| Follow-up check after the ThemeListCustomization correction | Result | Evidence |
| --- | --- | --- |
| SDK 11 RC2, Debug solution build | Passed, 0 warnings, 0 errors | [Build log](logs/all-build-debug-sdk11.0.100-rc.2.26504.105-20261014-211210-762.log) |
| SDK 11 RC2, Release solution build | Passed, 12 warnings in other source files, 0 errors | [Build log](logs/all-build-release-sdk11.0.100-rc.2.26504.105-20261014-211252-929.log) |
| SDK 11 RC2, Release native tests | 168 passed, 0 failed; all six ThemeListCustomization tests passed | [Native XML](TestResults/Release/20261014-211502-147-9a6c875c/native-runtime-5070cf39db5d4437b5ae15ecc10cbcc6/runtime-tests-windows.xml) |

That Release build reported `CsWinRT1032` at twelve further locations in OpenWeatherMapService, DaisyWeatherCard, GallerySettings, and CustomControls.
All twelve collection expressions now target concrete `ForecastDay[]`, `string[]`, or `KeyValuePair<string, string?>[]` arrays.
The existing enumerable APIs, values, order, and eager snapshots remain unchanged.
Both subsequent solution builds compiled the affected source files without warnings.

| Final check after the Weather and Gallery correction | Result | Evidence |
| --- | --- | --- |
| SDK 11 RC2, Debug solution build | Passed, 0 warnings, 0 errors | [Build log](logs/all-build-debug-sdk11.0.100-rc.2.26504.105-20261014-212048-529.log) |
| SDK 11 RC2, Release solution build | Passed, 0 warnings, 0 errors | [Build log](logs/all-build-release-sdk11.0.100-rc.2.26504.105-20261014-212239-560.log) |
| SDK 11 RC2, Release native tests | 168 passed, 0 failed | [Native XML](TestResults/Release/20261014-212353-948-fae5ff62/native-runtime-79da8ed60c7c4d3ea1b2c7838eb0e147/runtime-tests-windows.xml) |

The recorded runs establish early SDK 11 toolchain compatibility for this .NET 10 Windows target.
They do not establish formal Windows App SDK support for a .NET 11 target.

All three final native test runs used monitor 2 in its lower half.
The placement logs identify `\\.\DISPLAY2`, `Portrait=True`, and `IsPrimary=False`.
The work area was `-1440,0,1440,2520`; the actual window bounds were `-1440,1260,1152,1008`.
The Gallery checks these bounds before it activates the window.

The suite includes native resources, localization, image processing, outer and inner shadows, direct-shadow lifetime, unload/reload handling, and animation control.
It also covers Win2D paths and native Composition transitions.
GL checks render all 50 registered effects, compile all 100 transition shaders, and read representative rendered frames.

An earlier separate Release Kiosk run passed all 33 smoke checks for slides, scaling, progress, and the natural autoplay sequence.
Its report is [Kiosk XML](TestResults/Kiosk/Release/20261014-192854-754-e1faaff4/kiosk-smoke.xml).
That run predates the final glass and shadow batch; it is separate from the three final 168-case runs above.

All assistant-started Flowery.WinUI application processes exited.

The Windows runner stores reports outside MSTest's temporary deployment directory.
This preserves XML attachments after MSTest performs its own deployment cleanup.

## Preserved limits

- SlackUserProvider and TeamsUserProvider contain unimplemented methods in the original source. The migration preserves those methods unchanged.
- External authentication and service calls were not exercised.
- The normal WinUI Gallery verifies project-reference consumption. An additional application that consumes the generated NuGet packages was not built.
- Historical artwork remains available in Git history.
- The checks establish the listed behavior. They do not establish visual equivalence for every control or display scale.

The GitHub repository is private and has no pushed source branch.
All migration changes remain local and uncommitted.
