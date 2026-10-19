# Graphics & Visual Effects

This document describes the graphics rendering capabilities, visual effects, and animation systems in Flowery.WinUI.

---

## Table of Contents

1. [Overview](#overview)
2. [SkiaSharp Infrastructure](#skiasharp-infrastructure)
3. [Glass Blur Effects](#glass-blur-effects) (DaisyGlass & DaisyCard)
4. [Carousel Slide Effects](#carousel-slide-effects)
5. [Carousel Slide Transitions](#carousel-slide-transitions)
6. [Native Windows Support](#native-windows-support)
7. [Performance Considerations](#performance-considerations)

---

## Overview

Flowery.WinUI provides native animation and image-processing capabilities:

| Layer | Technology | Purpose |
| ----- | ---------- | ------- |
| **UI Animations** | WinUI Composition API, Storyboards | Standard control animations, transitions |
| **Skia Processing** | CPU SkiaSharp raster surfaces | Image blur, saturation, glass effects |
| **Native Geometry** | Win2D and Windows Composition | Masks and vector geometry |

The repository targets Windows with WinUI 3 and Windows App SDK.

---

## SkiaSharp Infrastructure

### Architecture

The `Flowery.Skia` namespace provides core image processing capabilities:

```txt
Flowery.WinUI/Skia/
├── SkiaImageProcessor.cs      # Core blur, saturation, glass effect processing
└── SkiaBackdropCapture.cs     # WinUI ↔ Skia bridge for backdrop capture
```

### SkiaImageProcessor

Static utility class for CPU image processing:

| Method | Description |
| ------ | ----------- |
| `ApplyBlur(bytes, sigmaX, sigmaY)` | Gaussian blur on image bytes |
| `ApplyBlurAndSaturation(bytes, sigma, saturation)` | Combined blur + saturation adjustment |
| `ApplyGlassEffect(bitmap, blur, saturation, tint, opacity, radius)` | Full glass effect with tint overlay and rounded corners |
| `FromPixelData(pixels, width, height)` | Convert BGRA pixel data to SKBitmap |
| `ToPixelData(bitmap)` | Copy an SKBitmap's raw pixel bytes to a byte array |

### SkiaBackdropCapture

Bridges WinUI's `RenderTargetBitmap` with Skia processing:

```csharp
// Capture area behind element and apply glass effect
var imageSource = await SkiaBackdropCapture.CaptureAndApplyGlassEffectAsync(
    element,
    blurRadius: 40,
    saturation: 0.8,
    tintColor: Colors.White,
    tintOpacity: 0.3,
    cornerRadius: 16);
```

**How it works:**

1. Finds the background source and serializes capture with other glass panels.
2. Temporarily hides glass overlays in that scene, including sibling panels.
3. Captures native WinUI content with `RenderTargetBitmap` at the XamlRoot rasterization scale.
4. Restores overlay opacity values and bindings.
5. Processes a padded region with CPU SkiaSharp blur, saturation, and tint.
6. Crops and rounds the result to the panel bounds, then returns a WinUI `ImageSource`.

---

## Glass Blur Effects

Both `DaisyGlass` and `DaisyCard` (with `IsGlass="True"` or `CardStyle="Glass"`) support three blur rendering modes:

### Blur Modes

| Mode | Description |
| ---- | ----------- |
| `Simulated` | Translucent fills and highlights, without real capture or blur |
| `BitmapCapture` | Native bitmap snapshot processed by CPU SkiaSharp |
| `SkiaSharp` | The same native snapshot and CPU SkiaSharp processing path |

The captured image includes its tint once; a second tint fill does not cover it.
Size, blur, saturation, tint, corner-radius, and rasterization changes invalidate the snapshot.
Unload clears the image and invalidates pending work; reload schedules a new capture.
For background-content changes, use `RefreshBackdrop()`.

### Key Properties

Available on both `DaisyGlass` and `DaisyCard`:

| Property | Type | Default | Description |
| -------- | ---- | ------- | ----------- |
| `EnableBackdropBlur` | `bool` | `false` | Enable real blur capture |
| `BlurMode` | `GlassBlurMode` | `Simulated` | Rendering mode |
| `GlassBlur` | `double` | `40` | Blur strength (higher = more blur) |
| `GlassSaturation` | `double` | `1.0` | Saturation (0 = grayscale, 1 = normal) |
| `GlassTint` | `Color` | `White` | Tint overlay color |
| `GlassTintOpacity` | `double` | `0.5` | Tint opacity |
| `GlassOpacity` | `double` | `0.25`/`0.3` | Base glass opacity |
| `GlassBorderOpacity` | `double` | `0.2` | Border line opacity |
| `GlassReflectOpacity` | `double` | `0.1`/`0.15` | Top highlight opacity |

### DaisyGlass Examples

```xml
<!-- Simulated glass (lightweight) -->
<controls:DaisyGlass>
    <TextBlock Text="Simulated" Margin="16" />
</controls:DaisyGlass>

<!-- Bitmap capture blur -->
<controls:DaisyGlass EnableBackdropBlur="True"
                     BlurMode="BitmapCapture"
                     GlassBlur="30">
    <TextBlock Text="Captured Blur" Margin="16" />
</controls:DaisyGlass>

<!-- SkiaSharp blur with desaturation -->
<controls:DaisyGlass EnableBackdropBlur="True"
                     BlurMode="SkiaSharp"
                     GlassBlur="50"
                     GlassSaturation="0.5">
    <TextBlock Text="Desaturated Blur" Margin="16" />
</controls:DaisyGlass>
```

### DaisyCard Glass Examples

```xml
<!-- Simulated glass card (lightweight) -->
<controls:DaisyCard IsGlass="True">
    <TextBlock Text="Glass Card" Margin="16" />
</controls:DaisyCard>

<!-- Glass card with Skia blur -->
<controls:DaisyCard CardStyle="Glass"
                    EnableBackdropBlur="True"
                    BlurMode="SkiaSharp"
                    GlassBlur="40"
                    GlassSaturation="0.8">
    <StackPanel Margin="16">
        <TextBlock Text="Real Blur Effect" FontWeight="Bold" />
        <TextBlock Text="Content over blurred background" />
    </StackPanel>
</controls:DaisyCard>

<!-- Glass card with custom tint -->
<controls:DaisyCard IsGlass="True"
                    EnableBackdropBlur="True"
                    BlurMode="BitmapCapture"
                    GlassTint="LightBlue"
                    GlassTintOpacity="0.3">
    <TextBlock Text="Tinted Glass" Margin="16" />
</controls:DaisyCard>
```

### Programmatic Refresh

For dynamic backgrounds, manually trigger recapture:

```csharp
// DaisyGlass
myGlass.RefreshBackdrop();

// DaisyCard with glass enabled
myCard.RefreshBackdrop();
```

---

## Carousel Slide Effects

Slide effects animate the **currently visible** slide during display. They create cinematic motion like Ken Burns documentary-style pan/zoom.

### Available Effects

| Effect | Description |
| ------ | ----------- |
| `None` | Static display (default) |
| `PanAndZoom` | Randomized pan + zoom combination |
| `ZoomIn` | Slow zoom from 1.0x to 1.0x + intensity |
| `ZoomOut` | Slow zoom from 1.0x + intensity to 1.0x |
| `PanLeft` | Slow pan leftward |
| `PanRight` | Slow pan rightward |
| `PanUp` | Slow pan upward |
| `PanDown` | Slow pan downward |
| `Drift` | Random direction pan (varies per slide) |
| `Pulse` | Subtle breathing/pulsing zoom |
| `Breath` | Zoom in to peak, settle back |
| `Throw` | Dramatic parallax fly-through |

### Effect Parameters

| Property | Type | Default | Description |
| -------- | ---- | ------- | ----------- |
| `SlideEffect` | `FlowerySlideEffect` | `None` | Active effect |
| `ZoomIntensity` | `double` | `0.15` | Zoom scale (0.15 = 115%) |
| `PanDistance` | `double` | `50` | Pan distance in pixels |
| `PulseIntensity` | `double` | `0.08` | Pulse scale (0.08 = 108%) |
| `PanAndZoomLockZoom` | `bool` | `false` | Pan at a constant computed cover scale |
| `PanAndZoomPanSpeed` | `double` | `4.0` | Pan speed (px/sec) |
| `PanAndZoomCycleSeconds` | `double` | `6.0` | Effect cycle duration |

### Usage

```xml
<controls:DaisyCarousel SlideEffect="PanAndZoom"
                        ZoomIntensity="0.2"
                        PanDistance="60"
                        SlideInterval="5">
    <Image Source="photo1.jpg" />
    <Image Source="photo2.jpg" />
</controls:DaisyCarousel>
```

---

## Carousel Slide Transitions

Slide transitions animate the **change between** slides. They are categorized by implementation complexity.

### Tier 1: Native Transforms

Uses native WinUI 3 `CompositeTransform` and `PlaneProjection`.

| Family | Transitions |
| ------ | ----------- |
| **Fade** | `Fade`, `FadeThroughBlack`, `FadeThroughWhite` |
| **Slide** | `SlideLeft`, `SlideRight`, `SlideUp`, `SlideDown` |
| **Push** | `PushLeft`, `PushRight`, `PushUp`, `PushDown` |
| **Zoom** | `ZoomIn`, `ZoomOut`, `ZoomCross` |
| **Flip** | `FlipHorizontal`, `FlipVertical`, `CubeLeft`, `CubeRight` |
| **Cover/Reveal** | `CoverLeft`, `CoverRight`, `CoverUp`, `CoverDown`, `RevealLeft`, `RevealRight` |

### Tier 2: Clip-Based (Advanced)

Uses animated native `RectangleGeometry` clips.

| Family | Transitions |
| ------ | ----------- |
| **Wipe** | `WipeLeft`, `WipeRight`, `WipeUp`, `WipeDown` |
| **Blinds** | `BlindsHorizontal`, `BlindsVertical` |
| **Slices** | `SlicesHorizontal`, `SlicesVertical`, `Checkerboard`, `Spiral`, `MatrixRain`, `Wormhole` |

### Tier 3: Windows Composition

`Dissolve` and `Pixelate` use Windows Composition visual surfaces and mask grids.
The repository's `PORTING.md` records the native runtime verification scope.

| Transition | Description |
| ---------- | ----------- |
| `Dissolve` | Pixel noise dissolve pattern |
| `Pixelate` | Mosaic pixelation effect |

### Special Transitions

| Transition | Description |
| ---------- | ----------- |
| `None` | Instant snap (no animation) |
| `Random` | Random transition per navigation |

### Transition Parameters

| Property | Type | Default | Description |
| -------- | ---- | ------- | ----------- |
| `SlideTransition` | `FlowerySlideTransition` | `None` | Active transition |
| `TransitionDuration` | `double` | `0.4` | Duration in seconds |
| `TransitionSliceCount` | `int` | `8` | Slices for Blinds/Slices |
| `TransitionStaggerSlices` | `bool` | `true` | Stagger slice animations |
| `TransitionSliceStaggerMs` | `double` | `50` | Stagger delay (ms) |
| `TransitionCheckerboardSize` | `int` | `6` | Grid size for Checkerboard |
| `TransitionBlurRadius` | `double` | `20` | Blur for blur transitions |
| `TransitionPixelateSize` | `int` | `20` | Pixel block size |
| `TransitionDissolveDensity` | `double` | `0.5` | Noise density for Dissolve |
| `TransitionFlipAngle` | `double` | `90` | Rotation angle for flips |
| `TransitionPerspectiveDepth` | `double` | `1000` | 3D perspective depth |

### Transition Usage

```xml
<controls:DaisyCarousel SlideTransition="Fade"
                        TransitionDuration="0.6"
                        WrapAround="True">
    <Image Source="slide1.jpg" />
    <Image Source="slide2.jpg" />
</controls:DaisyCarousel>
```

### Slideshow Modes

| Mode | Description |
| ---- | ----------- |
| `Manual` | User navigates via buttons/swipe |
| `Slideshow` | Auto-advance sequentially (loops) |
| `Random` | Auto-advance in random order |
| `Kiosk` | Random order + random effects (screensaver) |

```xml
<controls:DaisyCarousel Mode="Kiosk"
                        SlideInterval="4"
                        SlideTransition="Random">
    <!-- Slides -->
</controls:DaisyCarousel>
```

---

## Helper Classes & APIs

### Slide Effect Helpers

Located in `Flowery.Helpers` namespace:

| Class | Purpose |
| ----- | ------- |
| `FlowerySlideEffects` | Attached properties for applying slide effects to any `UIElement` |
| `FlowerySlideEffectParams` | Record for configuring effect parameters |
| `FlowerySlideEffectParser` | Parses string names to `FlowerySlideEffect` enum |

**Attached Property Usage:**

```xml
xmlns:helpers="using:Flowery.Helpers"

<Image Source="photo.jpg"
       helpers:FlowerySlideEffects.Effect="PanAndZoom"
       helpers:FlowerySlideEffects.Duration="5"
       helpers:FlowerySlideEffects.ZoomIntensity="0.15"
       helpers:FlowerySlideEffects.AutoStart="True" />
```

**Programmatic Usage:**

```csharp
using Flowery.Helpers;

// Apply effect to any UIElement
FlowerySlideEffects.SetEffect(myImage, FlowerySlideEffect.PanAndZoom);
FlowerySlideEffects.SetDuration(myImage, 5.0);
FlowerySlideEffects.SetAutoStart(myImage, true);

// Start/stop manually
FlowerySlideEffect applied = FlowerySlideEffects.StartEffect(myImage);
FlowerySlideEffects.StopEffect(myImage);

// Preserve transform during stop (for smooth transitions)
FlowerySlideEffects.StopEffectPreservingTransform(myImage);
```

### Slide Transition Helpers

| Class | Purpose |
| ----- | ------- |
| `FlowerySlideTransitionHelpers` | Applies transitions between two slide elements |
| `FlowerySlideTransitionParams` | Record for configuring transition parameters |
| `FlowerySlideTransitionParser` | Parses string names to `FlowerySlideTransition` enum |
| `FloweryTransitionTier` | Enum categorizing transitions (Transform/Clip/Composition) |

**Programmatic Transition:**

```csharp
using Flowery.Helpers;

var transitionParams = new FlowerySlideTransitionParams
{
    Duration = TimeSpan.FromMilliseconds(500),
    SliceCount = 10,
    StaggerSlices = true
};

FlowerySlideTransition applied = FlowerySlideTransitionHelpers.ApplyTransition(
    container,
    oldSlideWrapper,
    newSlideWrapper,
    FlowerySlideTransition.Checkerboard,
    transitionParams,
    onComplete: () => { /* cleanup */ });
```

### Slideshow Controller

| Class | Purpose |
| ----- | ------- |
| `FlowerySlideshowController` | Manages auto-advance timing for slideshow/kiosk modes |
| `FlowerySlideshowMode` | Enum: `Manual`, `Slideshow`, `Random`, `Kiosk` |
| `FlowerySlideChangedEventArgs` | Event args with old/new index, applied effect, current element |

### Native Geometry and Masks

The optional Flowery.WinUI.Win2D project supplies Composition geometry and masking support.
The native solution uses Microsoft.Graphics.Win2D with Windows Composition.
The native shader-effect host uses WinUI 3 WebView2 and retains the original shader presets.
See [Native Carousel Rendering](FloweryCarouselGL.md) for its API and diagnostics.

### Skia Infrastructure

| Class | Purpose |
| ----- | ------- |
| `SkiaImageProcessor` | Core image blur, saturation, glass effect processing |
| `SkiaBackdropCapture` | Captures WinUI elements and processes with Skia |

**Direct Skia Processing:**

```csharp
using Flowery.Skia;

// Apply blur to image bytes
byte[]? blurred = SkiaImageProcessor.ApplyBlur(pngBytes, sigmaX: 5, sigmaY: 5);

// Apply glass effect to bitmap
byte[]? glass = SkiaImageProcessor.ApplyGlassEffect(
    skBitmap,
    blurSigma: 4.0f,
    saturation: 0.8f,
    tintColor: SKColors.White,
    tintOpacity: 0.3f,
    cornerRadius: 16f);
```

---

## Native Windows Support

Flowery.WinUI targets Windows with WinUI 3.
Composition animations, Storyboards, bitmap capture, and SkiaSharp image processing run in the native build.
The optional Flowery.WinUI.Win2D project supplies Win2D graphics.

### Shared Composition foundation

The existing rendering paths have different responsibilities:

| Existing path | Reuse decision |
| --- | --- |
| XAML controls, layout, and automation peers | Keep text, input, focus, hit testing, and accessible semantics in WinUI. |
| `DaisyLoading`, progress, status, and motion behaviors | Keep their native Composition animations and reuse the `DaisyMotion` policy. |
| `DaisyShadowContainer` outer shadows | Use the shared visual scope and retained vector geometry. |
| Optional `Flowery.WinUI.Win2D` geometry and carousel masks | Retain the optional integration; the core foundation does not reference Win2D. |
| Inset shadows and captured glass | Keep CPU SkiaSharp processing; displaying its bitmap through WinUI does not make that processing GPU work. |
| WebView2 shader examples | Keep the separate shader host; it is not the shared Composition implementation. |

The core library combines normal WinUI content with native Composition visuals.
Text, input, focus, and accessibility remain with the WinUI controls.
Composition draws decorative vectors and supports animation without a separate application render loop.

The internal `Flowery.Composition` helpers manage four responsibilities:

| Helper | Responsibility |
| --- | --- |
| `CompositionVisualScope` | Owns an attached visual tree and its resources. It preserves another owner's child visual during attachment and disposal. |
| `CompositionVectorVisual` | Keeps a native shape, geometry, and fill brush together for in-place updates. Native stroke, dash, transform, and clip properties remain available. |
| `CompositionElementTracker` | Reports changed element bounds in host-relative DIPs after layout, scrolling, or rasterization-scale changes. Unchanged bounds do not trigger another update. |
| `CompositionVisualTree` | Checks membership in the root content or an open popup. A cached `XamlRoot` reference alone does not establish that an element is still attached. |

`DaisyShadowContainer` uses this foundation for outer shadows.
Size, corner-radius, and appearance changes update the existing outer visual graph.
Changes in the number of visible outer shadows rebuild that graph.
Unload and template replacement release the owned resources.
The Showcase page combines this container with a normal editable `DaisyInput`.

Consumers supply theme and appearance changes through their dependency properties.
XAML layout supplies size changes; animated controls use the existing `DaisyMotion` policy.
The shared helpers do not subscribe to theme or motion settings themselves.
`DaisyShadowContainer` observes Windows contrast themes and releases its decorative outer and inset graphics while contrast mode is active.
It retains the shadow descriptions and restores the graphics when contrast mode ends.

Layout and wrapping still run through XAML on the UI thread.
Hardware composition does not calculate control relationships or replace those layout updates.
Inset shadows and captured glass effects retain their CPU SkiaSharp processing paths.
These helpers do not add a mandatory Win2D dependency or change the public shadow API.

The shader effects and gallery examples use a native WinUI 3 WebView2 host.
Tier 3 carousel transitions use Windows Composition surfaces and mask grids.
The repository's `PORTING.md` records validated configurations and verification scope.

---

## Performance Considerations

### DaisyGlass

- **Simulated mode** is always the fastest; use for high-frequency scenarios
- **Capture modes** refresh after relevant state changes; call `RefreshBackdrop()` after the background content changes
- **SkiaSharp** blur sigma scales with CPU cost; keep `GlassBlur` under 50 for smooth performance
- Backdrop capture temporarily hides glass overlays in the sampled scene; avoid unnecessary recaptures during animations

### Carousel Effects

- **Slide effects** run continuously; prefer lower `ZoomIntensity` and `PanDistance` for smooth 60fps
- **PanAndZoom** is the most versatile but most complex; test on target devices
- **Tier 3 transitions** use native Composition surfaces and mask grids; cell budgets limit the number of visual elements

### Memory

- SkiaSharp bitmaps are disposed after processing
- `RenderTargetBitmap` captures use the current rasterization scale; large backgrounds increase snapshot and processing memory
- Clear `ImageSource` on Unloaded to free resources

---

## Snapshot Capture Scope

The glass implementation uses native WinUI snapshots and CPU SkiaSharp processing.
It does not continuously sample the backdrop or use a Composition blur pipeline.
Use `RefreshBackdrop()` when background content changes independently of the panel's properties.
