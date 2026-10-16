# Flowery Photos

A simple demo app for viewing photos and making basic edits, built with
[Flowery.WinUI](../README.md) Daisy controls, Win2D and WinUI 3.

Flowery Photos shows what a complete, everyday desktop app looks like with Flowery.WinUI:
Daisy controls throughout, runtime themes, global sizing and 12 UI languages.
It is a showcase, not a replacement for a full photo manager or image editor.

## Features

### Home

- **Start page:** greeting with a library summary and quick actions (slideshow, add folder, import),
  counts for photos, favorites, albums and folders, and rows of recently taken photos, favorites,
  folders and albums.
- **Welcome page:** an empty library shows the first steps (add a folder, import photos or use the
  sample photos) and what the app can do.

### Library and gallery

- **Folders as library:** starts with your `Pictures` folder. Add more folders, include or exclude
  subfolders, and remove folders without deleting files. Changes on disk are picked up automatically.
- **Fast scanning:** dimensions, capture date, orientation and camera model are read directly from
  file headers (JPEG/EXIF, PNG, GIF, BMP, WebP) and cached. Other formats (TIFF, HEIC, AVIF, JXR)
  fall back to Windows Imaging.
- **Justified photo grid:** virtualized rows that keep each photo's aspect ratio, grouped by day,
  month or year. Optional square thumbnails and file names.
- **Date chip and year timeline:** the header shows the date of the photos in view; the timeline on
  the right jumps to any year or month.
- **Search, filter, sort:** search by file name, folder, camera, year or month name; filter by file
  type and orientation; sort by date taken, date modified, name or file size.
- **Thumbnail size:** slider, `Ctrl` + mouse wheel or `Ctrl` + `+`/`-`.
- **Multi-select:** check boxes, `Ctrl`/`Shift` click, select a whole date group or select all.
  Then favorite, add to an album, copy or delete in one step.
- **Context menu:** open, edit, favorite, add to album, copy, open with, show in File Explorer,
  set as desktop background, delete (Recycle Bin, with confirmation).
- **Import:** add a folder, import files (copied into `Pictures\Flowery Photos`), or drop photos and
  folders onto the window.

### Favorites, albums and folders

- **Favorites:** heart any photo (`F` in the viewer) and find it on the Favorites page.
- **Albums:** collect photos from anywhere without moving files; create, rename and delete albums
  and start a slideshow from an album.
- **Folders:** card overview of every folder that contains photos, with cover image and count.

### Viewer

- Zoom with the mouse wheel (or switch the wheel to next/previous) in round steps (10 % below
  100 %, then 25 %), double-click for 100 %, drag to pan, fit to window, zoom presets and slider.
- Previous/next with arrow keys or the overlay buttons; opening a single file from outside the
  library browses its folder.
- File info panel: folder, size, dimensions, dates, camera, lens, exposure and GPS location
  (opens the map).
- Slideshow with cross-fade and configurable interval.
- Immersive full screen (`F11`): hides the taskbar and the title bar; toolbar and status bar float
  above the photo and fade out until the pointer reaches the top or bottom edge.
- **View options:** rulers with pixel coordinates, gridlines, status bar on/off, auto-hide toolbar,
  and a navigator thumbnail for panning zoomed photos. The auto-hidden toolbar stays while the pointer
  is near it and hides at once on a click elsewhere; the title bar then offers the view options.

### Editing

All edits are non-destructive and GPU accelerated (Win2D) until you save.

- **Crop:** rotate, flip, straighten, free or fixed aspect ratios (original, 1:1, 4:3, 3:2, 16:9 and
  portrait variants), crop handles with rule-of-thirds grid.
- **Adjust:** one-click auto enhance, then brightness, exposure, contrast, highlights, shadows,
  saturation, warmth, tint, sharpness, clarity, vignette and fade.
- **Filters:** 14 looks with live previews of the current photo and adjustable intensity.
- **Markup:** pen and highlighter with adjustable size; strokes are saved into the image.
- **Colors (markup):** primary/secondary color (left/right mouse button, swap with `X`), presets,
  eyedropper, nine persistent custom colors, recently used colors. The *Advanced* section offers
  color adjustments, color harmonies, a palette extracted from the photo, palette import/export
  (Paint Shop Pro JASC-PAL, GIMP `.gpl`) and a full color editor.
- **Compare:** split view with a draggable before/after divider.
- **Undo/redo** for every step, **Revert** to the original (at the top of the edit panel).
- **Save:** save a copy (default), replace the original after confirmation, or copy the edited
  image to the clipboard. Unsaved edits are never lost silently.

### App

- Daisy themes, global size (scales controls and text) and 12 languages (English, German, French,
  Spanish, Italian, Chinese (Simplified), Japanese, Korean, Turkish, Arabic, Hebrew, Ukrainian),
  switchable at runtime.
- Custom title bar with search, navigation rail, toasts and dialogs.
- The window frame and the hover, focus and selection frames use the Windows accent color.
- Settings open as a modal dialog above the current view (also above the editor). Changes preview
  live; **OK** keeps them, **Cancel** (or `Esc`) restores the previous state and returns to where you were.
- Window position, size and all preferences are remembered.

## Keyboard shortcuts

| Keys | Action |
| --- | --- |
| `Ctrl` + `F` | Search |
| `Ctrl` + `,` | Settings |
| `Ctrl` + `A` | Select all photos |
| `Ctrl` + mouse wheel | Change thumbnail size |
| `←` / `→` | Previous / next photo |
| `Ctrl` + `E` | Open the editor |
| `Ctrl` + `Z` / `Ctrl` + `Y` | Undo / redo |
| `Ctrl` + `S` | Save a copy |
| `+` / `-`, `0`, `1` | Zoom in / out, fit, 100 % |
| `F` | Toggle favorite |
| `R` / `Shift` + `R` | Rotate right / left |
| `X` | Swap primary/secondary color (markup) |
| `Del` | Delete photo |
| `F5` | Slideshow |
| `F11` | Full screen |
| `Esc` | Close / go back |

## Screenshots

[`Screenies`](Screenies) contains feature screenshots (1920x1080). `Screenies/Capture-Screenshots.ps1`
recreates them: it starts the built app with an isolated state folder and the bundled sample photos,
copies only theme, language, size and view options from your settings, and drives the UI.

```powershell
pwsh -File Flowery.WinUI.Photos/Screenies/Capture-Screenshots.ps1 -Language en
```

## Build and run

Requirements are the same as for the repository (Windows 10/11, .NET 10 SDK, x64).

```powershell
dotnet build Flowery.WinUI.Photos/Flowery.WinUI.Photos.csproj -c Debug -p:Platform=x64
.\bin\Debug\Flowery.WinUI.Photos\net10.0-windows10.0.19041\win-x64\Flowery.WinUI.Photos.exe
```

Pass an image path as the first argument to open it directly in the viewer.

Settings and the metadata cache are stored in `%LOCALAPPDATA%\Flowery.WinUI.Photos`.
Set the environment variable `FLOWERY_STATE_ROOT` to an absolute path to use a different
location, for example for tests with sample data only.

## Limitations

- Saving re-encodes the image; EXIF metadata of the original is not carried over.
- Formats that cannot be written back (for example HEIC, AVIF, WebP) are saved as JPEG copies.
- Animated GIFs show their first frame.
- There is no repair/retouch tool, no RAW development and no cloud sync.
