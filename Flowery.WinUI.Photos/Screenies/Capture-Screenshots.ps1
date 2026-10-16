<#
.SYNOPSIS
    Captures the Flowery Photos feature screenshots into this folder.
.DESCRIPTION
    Starts the built app with an isolated state folder, drives it through UI Automation and mouse input,
    and saves one PNG per feature (visible window area, default 1920x1080).

    Theme, language, size and view options are copied from your own Flowery Photos settings. Your folders,
    favorites, albums and metadata cache are never read: the library uses the bundled sample photos only,
    with a few favorites and two albums seeded for the screenshots.

    Do not touch mouse or keyboard while the script runs. The screen must be larger than the window.
.PARAMETER Configuration
    Build configuration of the app to run (Debug or Release). Build it first.
.PARAMETER Width
    Visible window width in pixels.
.PARAMETER Height
    Visible window height in pixels.
.PARAMETER SettingsFrom
    State folder to copy theme, language, size and view options from.
.PARAMETER Language
    Overrides the copied language (for example en or de).
.EXAMPLE
    pwsh -File .\Capture-Screenshots.ps1 -Language en
#>
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',
    [int]$Width = 1920,
    [int]$Height = 1080,
    [string]$SettingsFrom = (Join-Path $env:LOCALAPPDATA 'Flowery.WinUI.Photos'),
    [string]$Language
)

$ErrorActionPreference = 'Stop'
if (-not $IsWindows -or $PSVersionTable.PSVersion.Major -lt 7) {
    throw 'Run this script in PowerShell 7 on Windows.'
}

$outputDir = $PSScriptRoot
$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$appDir = Join-Path $repoRoot "bin\$Configuration\Flowery.WinUI.Photos\net10.0-windows10.0.19041\win-x64"
$exe = Join-Path $appDir 'Flowery.WinUI.Photos.exe'
$samples = Join-Path $appDir 'Assets\Samples'
if (-not (Test-Path -LiteralPath $exe)) { throw "App not found, build it first: $exe" }
if (-not (Test-Path -LiteralPath $samples)) { throw "Sample photos not found: $samples" }

$stateDir = Join-Path ([IO.Path]::GetTempPath()) 'FloweryPhotosScreenshots'

Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes, System.Drawing, System.Windows.Forms
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class ShotNative
{
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint flags, int dx, int dy, int data, UIntPtr extra);
    [DllImport("user32.dll")] public static extern void keybd_event(byte vk, byte scan, uint flags, UIntPtr extra);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
    [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr hWnd, IntPtr after, int x, int y, int cx, int cy, uint flags);
    [DllImport("dwmapi.dll")] public static extern int DwmGetWindowAttribute(IntPtr hWnd, int attribute, out RECT rect, int size);
}
'@

$script:hwnd = [IntPtr]::Zero
$A = [System.Windows.Automation.AutomationElement]
$TreeScope = [System.Windows.Automation.TreeScope]

#region Helpers

function Stop-App {
    Get-Process Flowery.WinUI.Photos -ErrorAction SilentlyContinue | Stop-Process -Force
    Start-Sleep -Milliseconds 800
}

function Get-VisibleBounds {
    $rect = New-Object ShotNative+RECT
    [void][ShotNative]::DwmGetWindowAttribute($script:hwnd, 9, [ref]$rect, 16) # DWMWA_EXTENDED_FRAME_BOUNDS
    return $rect
}

function Start-App {
    $env:FLOWERY_STATE_ROOT = $stateDir
    $process = Start-Process -FilePath $exe -PassThru
    for ($i = 0; $i -lt 60 -and $process.MainWindowHandle -eq [IntPtr]::Zero; $i++) {
        Start-Sleep -Milliseconds 250
        $process.Refresh()
    }
    if ($process.MainWindowHandle -eq [IntPtr]::Zero) { throw 'The app window did not appear.' }
    $script:hwnd = $process.MainWindowHandle
    Start-Sleep -Seconds 3

    # Size the window so that its visible area (without the invisible resize borders) is exactly Width x Height.
    $outer = New-Object ShotNative+RECT
    [void][ShotNative]::GetWindowRect($script:hwnd, [ref]$outer)
    $visible = Get-VisibleBounds
    $extraW = ($outer.Right - $outer.Left) - ($visible.Right - $visible.Left)
    $extraH = ($outer.Bottom - $outer.Top) - ($visible.Bottom - $visible.Top)
    [void][ShotNative]::SetWindowPos($script:hwnd, [IntPtr]::Zero, 120, 80, $Width + $extraW, $Height + $extraH, 0x0014) # NOZORDER | NOACTIVATE
    Start-Sleep -Seconds 2
    Hide-Cursor
}

function Get-Window { $A::FromHandle($script:hwnd) }

function Hide-Cursor {
    $screen = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
    [void][ShotNative]::SetCursorPos($screen.Right - 2, $screen.Bottom - 2)
}

function Move-To([int]$X, [int]$Y) {
    $b = Get-VisibleBounds
    [void][ShotNative]::SetCursorPos($b.Left + $X, $b.Top + $Y)
}

function Invoke-Click([int]$X, [int]$Y, [double]$Wait = 1.0) {
    Move-To $X $Y
    Start-Sleep -Milliseconds 120
    [ShotNative]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero)
    [ShotNative]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero)
    Hide-Cursor
    Start-Sleep -Milliseconds ([int]($Wait * 1000))
}

function Invoke-CtrlClicks([int[][]]$Points) {
    [ShotNative]::keybd_event(0x11, 0, 0, [UIntPtr]::Zero)
    foreach ($p in $Points) {
        Move-To $p[0] $p[1]
        Start-Sleep -Milliseconds 120
        [ShotNative]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero)
        [ShotNative]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero)
        Start-Sleep -Milliseconds 200
    }
    [ShotNative]::keybd_event(0x11, 0, 2, [UIntPtr]::Zero)
    Hide-Cursor
    Start-Sleep -Milliseconds 800
}

function Invoke-Drag([int[][]]$Points) {
    $b = Get-VisibleBounds
    $screen = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
    Move-To $Points[0][0] $Points[0][1]
    Start-Sleep -Milliseconds 150
    [ShotNative]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero)
    for ($i = 1; $i -lt $Points.Count; $i++) {
        for ($s = 1; $s -le 12; $s++) {
            $x = $Points[$i - 1][0] + ($Points[$i][0] - $Points[$i - 1][0]) * $s / 12
            $y = $Points[$i - 1][1] + ($Points[$i][1] - $Points[$i - 1][1]) * $s / 12
            # Absolute moves produce real pointer-move input (SetCursorPos alone does not draw strokes).
            $ax = [int](($b.Left + $x) * 65535 / ($screen.Width - 1))
            $ay = [int](($b.Top + $y) * 65535 / ($screen.Height - 1))
            [ShotNative]::mouse_event(0x8001, $ax, $ay, 0, [UIntPtr]::Zero)
            Start-Sleep -Milliseconds 12
        }
    }
    [ShotNative]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero)
    Hide-Cursor
    Start-Sleep -Milliseconds 500
}

function Invoke-Wheel([int]$X, [int]$Y, [int]$Notches) {
    Move-To $X $Y
    for ($i = 0; $i -lt [Math]::Abs($Notches); $i++) {
        [ShotNative]::mouse_event(0x0800, 0, 0, [Math]::Sign($Notches) * 120, [UIntPtr]::Zero)
        Start-Sleep -Milliseconds 250
    }
    Hide-Cursor
}

# Invokes, expands or toggles the first element with this accessible name (or its nearest actionable parent).
function Invoke-Name([string]$Name, [double]$Wait = 1.0) {
    $win = Get-Window
    $walker = [System.Windows.Automation.TreeWalker]::ControlViewWalker
    $elements = $win.FindAll($TreeScope::Descendants, (New-Object System.Windows.Automation.PropertyCondition($A::NameProperty, $Name)))
    foreach ($element in $elements) {
        $current = $element
        for ($i = 0; $i -lt 4 -and $current; $i++) {
            $pattern = $null
            if ($current.TryGetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern, [ref]$pattern)) { $pattern.Invoke(); Start-Sleep -Milliseconds ([int]($Wait * 1000)); return }
            if ($current.TryGetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern, [ref]$pattern)) { $pattern.Expand(); Start-Sleep -Milliseconds ([int]($Wait * 1000)); return }
            if ($current.TryGetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern, [ref]$pattern)) { $pattern.Toggle(); Start-Sleep -Milliseconds ([int]($Wait * 1000)); return }
            $current = $walker.GetParent($current)
        }
    }
    throw "No actionable element named '$Name'."
}

# Toggles the toggle switches with these indexes (in UI Automation order).
function Invoke-Toggles([int[]]$Indexes) {
    $win = Get-Window
    $toggles = $win.FindAll($TreeScope::Descendants, (New-Object System.Windows.Automation.PropertyCondition($A::IsTogglePatternAvailableProperty, $true)))
    foreach ($index in $Indexes) {
        $toggles[$index].GetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern).Toggle()
        Start-Sleep -Milliseconds 400
    }
}

# Automation invokes move keyboard focus, which shows tooltips; a click on the empty title bar removes them.
function Clear-Tooltip { Invoke-Click 600 25 0.6 }

function Save-Shot([string]$Name) {
    [void][ShotNative]::SetForegroundWindow($script:hwnd)
    Start-Sleep -Milliseconds 400
    $b = Get-VisibleBounds
    $w = $b.Right - $b.Left
    $h = $b.Bottom - $b.Top
    $bitmap = New-Object System.Drawing.Bitmap $w, $h
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.CopyFromScreen($b.Left, $b.Top, 0, 0, $bitmap.Size)
    $path = Join-Path $outputDir "$Name.png"
    $bitmap.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $graphics.Dispose()
    $bitmap.Dispose()
    Write-Host ("{0}.png ({1}x{2})" -f $Name, $w, $h)
}

function Write-State([string]$Key, [string[]]$Lines) {
    Set-Content -LiteralPath (Join-Path $stateDir "$Key.state") -Value $Lines -Encoding utf8
}

#endregion

# Window-relative positions (visible window area, 1920x1080 layout).
$Rail = @{ Home = 81; Gallery = 133; Favorites = 185; Albums = 237; Folders = 289; Settings = 1043 }
$Tabs = @{ Crop = 1590; Adjust = 1660; Filter = 1730; Markup = 1800 }
$TabY = 175
$CompareX = 1647
$RevertX = 1870
$EditRowY = 113

#region State

Stop-App
if (Test-Path -LiteralPath $stateDir) { Remove-Item -LiteralPath $stateDir -Recurse -Force }
New-Item -ItemType Directory -Path $stateDir | Out-Null
foreach ($key in 'theme', 'language', 'size', 'photos.options') {
    $source = Join-Path $SettingsFrom "$key.state"
    if (Test-Path -LiteralPath $source) { Copy-Item -LiteralPath $source -Destination $stateDir }
}
if ($Language) { Write-State 'language' @($Language) }
# An intentionally empty library (marker line), so the first screenshot shows the welcome page.
Write-State 'photos.folders' @(' ')

Get-ChildItem -LiteralPath $outputDir -Filter '*.png' | Remove-Item -Force

#endregion

try {
    # 01: home page without folders
    Start-App
    Save-Shot '01-home-no-folders'
    Stop-App

    # Sample library with favorites and albums
    $s = $samples
    Write-State 'photos.folders' @($s)
    Write-State 'photos.favorites' @("$s\wallpaper_yellow.jpg", "$s\tile_green.jpg", "$s\wallpaper_blue.jpg")
    Write-State 'photos.albums' @(
        (@('Favorite patterns', "$s\tile_blue.jpg", "$s\tile_green.jpg", "$s\tile_red.jpg", "$s\tile_yellow.jpg") -join "`t"),
        (@('Wallpapers', "$s\wallpaper_blue.jpg", "$s\wallpaper_green.jpg", "$s\wallpaper_red.jpg", "$s\wallpaper_yellow.jpg") -join "`t"))
    Start-App
    Start-Sleep -Seconds 3
    Save-Shot '02-home'

    # Gallery
    Invoke-Click 32 $Rail.Gallery 2
    Save-Shot '03-gallery'

    Invoke-Name 'View options' 1.2
    Save-Shot '04-gallery-view-options'
    Invoke-Click 1300 900 0.8

    Invoke-CtrlClicks @(@(450, 250), @(950, 250), @(1200, 250))
    Save-Shot '05-gallery-multi-select'
    Invoke-Name 'Clear selection' 0.5

    # Collections
    Invoke-Click 32 $Rail.Favorites 2
    Save-Shot '06-favorites'
    Invoke-Click 32 $Rail.Albums 2
    Save-Shot '07-albums'
    Invoke-Click 32 $Rail.Folders 2
    Save-Shot '08-folders'

    # Viewer
    Invoke-Click 32 $Rail.Gallery 2
    Invoke-Click 960 250 2.5
    Save-Shot '09-viewer'

    Invoke-Name 'File info' 1.2
    Clear-Tooltip
    Save-Shot '10-file-info'

    # Editor
    Invoke-Name 'Edit' 1.2
    Clear-Tooltip
    Invoke-Click $Tabs.Crop $TabY 1.2
    Invoke-Click 1696 494 1.2          # 16:9 aspect ratio
    Save-Shot '11-edit-crop'

    Invoke-Click $RevertX $EditRowY 1
    Invoke-Click $Tabs.Adjust $TabY 1.2
    Invoke-Click 1700 232 1.5          # Auto enhance
    Save-Shot '12-edit-adjust'

    Invoke-Click $RevertX $EditRowY 1
    Invoke-Click $Tabs.Filter $TabY 1.5
    Invoke-Click 1610 367 1.5          # "Cool" filter
    Save-Shot '13-edit-filters'

    Invoke-Click $CompareX $EditRowY 1.5
    Save-Shot '14-edit-before-after'
    Invoke-Click $CompareX $EditRowY 1

    Invoke-Click $RevertX $EditRowY 1
    Invoke-Click $Tabs.Markup $TabY 1.5
    Invoke-Click 1680 508 0.6          # pen size
    Invoke-Drag @(@(640, 520), @(700, 420), @(820, 380), @(950, 430), @(990, 560), @(930, 690), @(800, 730), @(680, 680), @(630, 580), @(650, 500))
    Invoke-Click 1724 357 0.6          # blue
    Invoke-Click 1815 263 0.6          # highlighter
    Invoke-Drag @(@(420, 880), @(600, 870), @(800, 885), @(1000, 872), @(1180, 880))
    Invoke-Name 'Advanced' 1
    Start-Sleep -Seconds 2             # navigation arrows fade out
    Save-Shot '15-edit-markup'

    # View options: rulers, grid and navigator
    Invoke-Click $RevertX $EditRowY 1.2
    Invoke-Name 'Edit' 1.2
    Clear-Tooltip
    Invoke-Name 'View' 1.2
    Invoke-Toggles @(0, 1, 3)
    Invoke-Click 800 560 0.8           # closes the flyout
    Invoke-Wheel 800 560 3
    Start-Sleep -Seconds 3
    Save-Shot '16-viewer-rulers-grid-navigator'

    # Settings dialog
    Invoke-Click 32 $Rail.Settings 1.5
    Save-Shot '17-settings'
    Invoke-Name 'Cancel' 0.8
}
finally {
    Stop-App
    Remove-Item Env:FLOWERY_STATE_ROOT -ErrorAction SilentlyContinue
}

Write-Host "Screenshots saved to $outputDir"
