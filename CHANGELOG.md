<!-- markdownlint-disable MD022 MD024 -->
# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Fixed

- Localized `DaisyColorPickerDialog` and `DaisyScreenColorPicker`. Their labels, buttons, tooltips and the screen picker hint were hard-coded in English; they now use new `ColorPicker_*` and `Common_Ok` keys in all 12 languages, and the screen picker follows language changes while it is shown.

## [1.3.0] - 2026-10-16

### Added

- Added `Flowery.WinUI.Photos`, a demo app for viewing photos and making basic edits with Flowery controls, Win2D, runtime themes, global sizing and 12 languages.

### Changed

- Aligned the font tokens with Flowery.NET. The primary ladder is now 10/12/14/18/20 (was 8/10/12/14/16), and the secondary, tertiary, section header, header, line height, badge, menu, kbd, divider and DateTimeline font tokens grow with it. `GetDefaultFontSize`, `GetDefaultLineHeight` and the `GetFontSizeForTier` fallbacks use the same values. Badge, kbd, DateTimeline, TextArea and floating input heights follow the Flowery.NET tokens so the larger text fits.
- Added `DaisyCheckbox{Size}FontSize`, `DaisyRadio{Size}FontSize` and `DaisyToggle{Size}FontSize` label tokens (Flowery.NET parity).
- Corrected the checkbox, radio and toggle size tables in `llms-static/DesignTokens.md` to the compact indicator tokens (for example a 12 px checkbox and radio and a 24×12 toggle with an 8 px knob at Medium) and documented the separate `DaisyRadio{Size}Size` tokens.

## [1.2.1] - 2026-10-15

### Fixed

- Fixed `FloweryComponentSidebar` categories that stayed expanded but empty after rapid search changes. Deferred item batches now continue with the newest search results.

## [1.2.0] - 2026-10-14

First native WinUI 3 release. Package versions match the Flowery.Uno 1.2.0 baseline.

- Repository: renamed project namespaces, documentation, resource examples, and repository links to Flowery.WinUI.
- Windows: migrated the nine solution projects to native WinUI 3 and Windows App SDK, including native resources, shadows, and graphics.
- Build: added exact SDK selection and local dotnet-host selection for early .NET 11 SDK RC2 testing. The application target remains .NET 10 for Windows; the CI matrix includes both SDK 10 and SDK 11 RC2.
- Build: all projects now use the shared version metadata from `Directory.Build.props`.
- Effects: preserved 50 carousel effects and 100 transition shaders through a native WinUI 3 WebView2 host.
- Animations: applied attached zoom intensity to PanAndZoom, kept locked zoom at a constant cover scale, and corrected deferred-start and unload/reload handling.
- Glass: corrected native snapshot cropping with blur padding, excluded sibling glass overlays from capture, applied tint once, and refreshed snapshots after appearance, size, rasterization, and load changes.
- Glass: corrected saturation channel weights and centered the Gallery comparison over a shared backdrop.
- Shadows: corrected native shadow demo cleanup and direct helper sizing, mask updates, and resource lifetime.
- Kiosk: preserved all six slides, slide themes, keywords, progress, eight-second autoplay, and manual navigation in the native Windows application.
- Kiosk: kept the Scaling header and progress fixed, made the demo fit the available width, and allowed titles and control rows to wrap. The selected percentage applies once, while controls retain their five size tiers. The loading caption remains readable on the Nord theme.
- Controls: made badge content fit each size tier and explicit dimensions without duplicate borders. Added the missing ExtraLarge badge tokens.
- Documentation: replaced obsolete backend and toolkit guidance with native WinUI 3 and Windows App SDK instructions. The generator reads native dependency property types, defaults, and descriptions.
- Copyright: changed current copyright and Gallery license headers to 2026.

## [Original source history]

The following entries describe the original source before the native WinUI migration.

### Unreleased before the native migration

- **Breaking:** Build: moved all Windows, Desktop (Skia), Browser and tooling target frameworks from net9.0 to net10.0 (Android was already net10.0); consumers must now target net10.0. Build/run scripts, CI build, runtime test runners and developer docs updated accordingly.
- Historical Linux target: the original desktop Gallery resumed startup after removal of the incompatible `Tmds.DBus.Protocol` 0.93.0 pin. Its original framework resolved 0.21.3 transitively.
- Tests: the Windows runtime tests no longer fail with path-too-long errors when the repository sits in a deep folder (e.g. a VM shared folder). Both runtime test runners now build the Gallery under `<temp>/frt/<8-char id>/` and log that folder as `Runtime test build root: ...`; the folder is deleted after each run. This is needed because the Windows App SDK XAML compiler (a net472 tool) ignores `LongPathsEnabled`. The results directory also drops a redundant project-name segment.
- Tests: runtime tests now use an in-memory state storage instead of the app's real one. Saved user settings no longer leak into tests (the Kanban drop-index test failed whenever the last saved Kanban view was Home), and test runs no longer add "Untitled Board" files to the Gallery's storage.
- **Visual:** Theming: contrast fixes ported from Flowery.NET 3.2.3 (8132bca) and 3.3.0 (62800f1). New palette keys `DaisyPrimaryForegroundBrush`, `DaisySecondaryForegroundBrush`, `DaisyAccentForegroundBrush` and `DaisyButtonPrimaryForegroundBrush` drive the text of outline, dash, soft and link buttons and of `DaisyIconText` on base surfaces; they equal the fill unless a theme overrides them. Black, Coffee, Cupcake, Dark, Lemonade, Luxury, Pastel, Retro and Wireframe use more readable shades there. Business uses white primary content and an opaque fill for disabled primary buttons. Smooth uses a lighter `Base300`, so borders and separators are visible. Review custom color overrides that depend on the previous values.
- **Breaking:** Visual: `DaisyCheckBox`, `DaisyRadio` and `DaisyToggle` use the compact indicators from Flowery.NET 3.3.0 (62800f1) in every size tier, for example a 12 px checkbox and radio and a 24×12 toggle at Medium (before 20 px and 36×20). `DaisyRadio` gets its own `DaisyRadio{Size}Size` tokens instead of reusing the checkbox sizes. The default `TogglePadding` is 1 instead of 4, so the knob sits at the track edge in every size. Label font sizes stay as they were, since the original implementation already used smaller sizes than the new Flowery.NET ones. Review fixed dimensions and style overrides that depend on the previous sizes.
- Windows: `DaisyTableCell` with string content showed empty cells on the WinUI head, because its default style had no template and WinUI never displayed the string. The style now has an explicit `ContentPresenter` template; cells with element content were not affected.
- Theming: `DaisyThemeManager.TryCreatePalette` and `GetPaletteFactory` expose the factory passed to `RegisterTheme`. `AvailableThemesChanged` fires when `RegisterTheme` adds a new theme name; built-in themes raise it only when `NotifyForInternalThemesChanged` is true (default false). Ported from Flowery.NET 3.2.0 (4c1cc89).
- Theming: `DaisyThemeDropdown` preview swatches now come from the registered palette factory. Themes registered with a custom factory previously showed the Dark palette, because the dropdown created palettes by name and unknown names fall back to Dark. Themes registered after a dropdown was loaded now appear in it.
- Theming: theme lists can be customized, ported from Flowery.NET 3.3.0 (62800f1). `DaisyThemeManager.ExcludedThemes`, `HideTheme`, `ShowTheme` and `IsThemeVisible` hide themes from both dropdowns. `PreferredThemes` and `AddTheme(originalName, displayName, preferred)` put themes into a preferred group at the top, separated by a line, with optional display aliases; `RemoveThemeOverride` undoes an addition. `DaisyThemeDropdown` and `DaisyProductThemeDropdown` gain local `AddTheme`/`RemoveThemeOverride`. Selecting an added product theme registers and applies it. `AvailableThemes` now lists preferred themes first.
- Security: `System.Security.Cryptography.Xml` (Gallery Browser head) bumped from 10.0.7 to 10.0.13, the first 10.0.x release without known vulnerabilities; this removes the `NU1903` build warnings.
- ColorPicker: added `DaisyColorPickerDialog`, ported from Flowery.NET 3.1.1. It combines the color wheel, lightness slider, screen color picker, palette and editor in a `FloweryDialogBase`, with a 50-step Undo/Redo history, and `ShowAsync(XamlRoot, Color)` returns the selected color or `null`. Every component, including hex input and the editor sliders, updates the preview and the returned color. The Gallery's Color Picker Dialog sample now uses it instead of a hand-built modal, and `DaisyColorPickerDialog.md` documents the XamlRoot-based dialog API.

### [1.2.0] - 2026-07-09

- Windows: fixed the Gallery startup crash (0xC000027B) caused by library XBFs missing from the app output; the WinAppSDK PRI merge skips multi-targeted library projects, so the Windows head now copies theme/resource XBFs to the library layout paths after build.
- Kanban: fixed dropping cards onto swimlane cells (lane reassignment within the same column did nothing and drop positions were mapped incorrectly in lane-filtered cells).
- Kanban: card moves now update the board incrementally instead of rebuilding all lane rows and task lists, removing the visible flicker on drag and drop.
- Kanban: bounded lane-cell heights in swimlane layout so task lists virtualize and scroll instead of realizing every card.
- Kanban: made drop-index calculation order-independent under ListView container recycling and simplified the internal visual caches.
- Tests: runtime tests now run with deterministic theme and global size instead of inheriting user-persisted Gallery settings, and no longer overwrite them.
- Tests: fixed stale default-value assertions and added coverage for drop-index calculation and lane-aware moves; full Skia runtime suite passes again.

### [1.1.0] - 2026-07-04

- Added 25 DaisyLoading animations across business workflow and Win95 retro variants.
- Added the new DaisyLoading variants to the Gallery feedback examples.
- Improved small-size readability for Win95 recycle-bin document animations.
- Ported theme fixes for neutral outline buttons, focused radios, and selected Gallery sidebar items.

### [1.0.0] - 2026-05-10

- Bumped version to 1.0.0 as there's no reason not to. :)
- Fixed Daisy control public styling behavior for `DaisyRadio`, `DaisyCheckBox`, `DaisyToggle`, and `DaisyTagPicker`.
- Public `BorderBrush` and `BorderThickness` now apply to each whole control border while internal themed indicators and tag panels keep their own borders.
- Public text styling such as `Foreground`, `FontSize`, `FontFamily`, `FontStyle`, and `FontWeight` now flows to visible labels, headers, and tag text.
- Added runtime coverage for explicit public border and font styling on the updated Daisy controls.

### [0.1.3] - 2026-05-07

- Updated Flowery shared package version metadata to `0.1.3`.
- Updated the original framework SDK and its package pins to the 6.5 line across the shared projects.
- Updated central package versions for SkiaSharp, the original UI toolkit, MSTest, Octokit, Win2D, WebAssembly crypto support, Tmds.DBus.Protocol, and Microsoft Testing extensions.
- Kept `Microsoft.Graphics.Win2D` on `1.3.2` because `1.4.0` introduced a mixed Windows App SDK 1.7/1.8 build graph with the original framework SDK.
- Updated Gallery desktop/browser package references required by the refreshed package graph.
- Updated release artifact actions for Node.js 24 and pinned Windows CI jobs to `windows-2025-vs2026`.
- Historical mobile packages: normalized library-layout paths so `dotnet pack` emitted matching library and dependency groups.

### [0.1.2] - 2026-02-12

- Refined size design tokens around a 4px grid and introduced mobile-specific sizing via `PlatformCompatibility.IsMobile`, keeping desktop/Windows ergonomics with the existing size presets.
- Increased mobile touch-target defaults for interactive controls and aligned global default size to `DaisySize.Medium`.
- Added line-height size tokens and applied token-driven text vertical alignment improvements in `DaisyButton`, `DaisyInput`, and `DaisyNumericUpDown` (including Android-specific centering adjustments).
- Updated `DaisyToggle` sizing behavior to enforce mobile-friendly minimum tap targets while preserving compact desktop rendering.
- Fixed browser build scripts to force browser-only framework evaluation and suppress EOL-check noise on preview SDKs (`build_browser.ps1`, `build_core.ps1`, `run-browser.ps1`).
- Historical mobile targets: migrated the original projects and their build/run scripts from .NET 9 to .NET 10.

### [0.1.1] - 2026-02-07

- Centralized versioning in `Directory.Build.props`.
- Improved Gallery startup responsiveness by prioritizing initial Home rendering and deferring restore navigation to heavy pages.
- Reduced Gallery startup overhead by deferring Kanban-specific initialization and resource loading until first Kanban usage.
- Library: mitigated repeated first-chance `NotImplementedException` churn for unsupported stroke APIs by caching runtime support checks.
- Common versions across projects updated.

### [0.1.0] - 2026-02-03

Initial port to the original cross-platform framework (alpha).
