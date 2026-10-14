# Overview

Neumorphic (Soft UI) mode is a design style that uses highlights and shadows to create elements that appear to extrude from or be inset into the background surface.

## What is Neumorphism?

Neumorphism (new skeuomorphism) combines the realism of skeuomorphism with the simplicity of flat design. It creates soft, extruded UI elements that appear to push out from or press into the background surface, using carefully positioned light and dark shadows.

### The Three Essential Properties

To apply neumorphism style to any element, you must implement these three main properties:

1. **Background Match**: Element background must be the same as its container (seamless surface)
2. **Dark Shadow**: Applied to one corner (bottom-right for raised, top-left for inset)
3. **Light Shadow**: Applied to the opposite corner (top-left for raised, bottom-right for inset)

### Shadow Positioning (Light Source Model)

The effect assumes a **top-left light source** (like sunlight through a window):

- **Raised Mode**:
  - Light shadow on top-left (where light hits the raised edge)
  - Dark shadow on bottom-right (where light doesn't reach)
- **Inset Mode**: Inverted positioning
  - Dark shadow on top-left (inner edge in shadow)
  - Light shadow on bottom-right (inner edge catching light)

## Technical Implementation

Flowery.WinUI renders neumorphism through native WinUI 3 controls and Windows Composition.

| Mode | Implementation |
| --- | --- |
| Raised | DaisyShadowContainer with dark and light outer Composition shadows |
| Flat | DaisyShadowContainer with one reduced outer shadow |
| Inset | DaisyNeumorphicHelper injects two gradient overlays |
| None | The helper removes its visual layers |

DaisyNeumorphicHelper.Windows.cs provides a direct Composition shadow when the visual tree cannot host a shadow container.
That path uses a single dark shadow.
Unsupported overlay injection disables the helper's inset layers for that instance.
The direct shadow follows owner size changes and updates its rounded mask.
Disposal releases its visual, shadow, mask surface, geometry, brushes, and size subscription.

### Native Shadow Container

DaisyShadowContainer and DaisyShadow use the Flowery.Controls namespace.
Merge the library's Generic.xaml after XamlControlsResources.
These controls require no separate toolkit package.

```xml
xmlns:daisy="using:Flowery.Controls"

<daisy:DaisyShadowContainer CornerRadius="12">
    <daisy:DaisyShadowContainer.Shadows>
        <daisy:DaisyShadow Color="Black" Opacity="0.35"
                           OffsetX="4" OffsetY="4" BlurRadius="12" />
    </daisy:DaisyShadowContainer.Shadows>
    <Border Background="{ThemeResource DaisyBase200Brush}"
            CornerRadius="12" Padding="16">
        <TextBlock Text="Native shadow" />
    </Border>
</daisy:DaisyShadowContainer>
```

DaisyShadow describes color, opacity, X/Y offset, blur radius, spread, and IsInner.
Outer shadows use Windows Composition masks.
Explicit IsInner shadows use SkiaSharp alpha masks at the current XamlRoot rasterization scale.
Collection changes, property changes, size changes, and rasterization changes update the shadow.
Unload releases Composition resources, image sources, and subscriptions.

The helper's Inset mode uses gradient overlays.
It is distinct from an explicit DaisyShadow with IsInner set to True.

### Custom Control Integration

1. Derive the control from DaisyBaseContentControl.
2. Select the element that renders the visible surface.
3. Override GetNeumorphicHostElement to return that surface.
4. Cache template parts in OnApplyTemplate.
5. Call RequestNeumorphicRefresh after the template is ready.
6. Keep the surface attached while the helper owns its layers.

```csharp
protected override FrameworkElement? GetNeumorphicHostElement()
{
    return _surfaceBorder is { } surface ? surface : this;
}
```

Enable the effect through the control's existing properties:

```xml
<daisy:DaisyCard NeumorphicEnabled="True"
                NeumorphicMode="Raised"
                NeumorphicIntensity="0.8" />
```

### Elevation System (Size-Aware)

Elevation values are based on the control's `DaisySize` via `DaisyResourceLookup.GetDefaultElevation()`:

| DaisySize | Elevation |
| :--- | :--- |
| ExtraSmall | 10 |
| Small | 11 |
| Medium | 12 |
| Large | 14 |
| ExtraLarge | 16 |

The caller (e.g., `DaisyButton`) passes the appropriate elevation based on its Size property.

### DaisyNeumorphicHelper API

```csharp
public void Update(
    bool enabled,
    DaisyNeumorphicMode mode,
    double intensity,
    double elevation)
```

| Parameter | Description |
| :--- | :--- |
| `enabled` | Whether neumorphic effect is active |
| `mode` | `Raised`, `Inset`, `Flat`, or `None` |
| `intensity` | Multiplier for shadow strength (0.0 - 1.0) |
| `elevation` | Base shadow depth in pixels (from `DaisyResourceLookup.GetDefaultElevation()`) |

### Deferred Attachment (Timing Fix)

The neumorphic helper requires the control's visual tree to be fully laid out before it can find the correct injection point. When `Content` is set on a `ContentControl`, the `ContentPresenter` doesn't process it immediately—this happens on the next layout pass.

**The Problem**: If `RefreshNeumorphicEffect()` runs immediately after `OnLoaded()`:

1. The helper calls `VisualTreeHelper.GetChild()` to find the visual tree
2. The `ContentPresenter` has 0 children (not laid out yet)
3. The helper falls back to attaching directly to the `ContentPresenter`
4. When layout completes, the composition surface renders ON TOP of the actual content
5. Controls appear as blank boxes

**The Solution**: Neumorphic attachment is deferred until after the first `LayoutUpdated` event:

```csharp
private void OnBaseLoaded(object sender, RoutedEventArgs e)
{
    // ... subscriptions ...
    OnLoaded();  // Derived control builds its visual tree here

    // Defer until visual tree is laid out
    LayoutUpdated += OnFirstLayoutForNeumorphic;
}

private void OnFirstLayoutForNeumorphic(object? sender, object e)
{
    LayoutUpdated -= OnFirstLayoutForNeumorphic;
    if (_isLoaded) RefreshNeumorphicEffect();
}
```

This ensures dynamically-built controls (like `DaisyInput`, `DaisyTextArea`) work correctly with neumorphic mode.

### Stability Safeguards (Post-Hang Fixes)

The following safeguards were added after real-world hangs (scrolling + page switches) to prevent layout storms and runaway visual tree updates:

1. **Single-shot overlay injection**
   - The helper now attempts to inject the overlay container only once per instance.
   - If the owner is not a direct child of a supported parent (`Panel` or `ContentPresenter`), injection is marked as failed and will not be retried.
   - This avoids repeated `LayoutUpdated` loops when the visual tree cannot be safely re-parented.

2. **Graceful fallback when injection fails**
   - If overlay injection fails, the helper *disables inset overlays* for that instance and falls back to **direct elevation** for Raised/Flat.
   - This keeps the app responsive on complex templates and virtualized panels, at the cost of losing inset on that element.

3. **Layout monitoring is gated**
   - `LayoutUpdated` monitoring is stopped as soon as the overlay is injected *or* injection is known to be impossible.
   - Update calls are ignored while the element is unloaded or the injection has failed, preventing update storms during scroll.

4. **No forced enablement**
   - Controls must respect `IsNeumorphicEffectivelyEnabled()` and **never** force-enable the effect when global mode is `None`.
   - Example fix: `DaisySlideToConfirm` stopped enabling neumorphism unconditionally to avoid background update churn.

5. **Gallery diagnostics guard (runtime only)**
   - Central in-app diagnostic log aggregation was disabled in the Gallery app to avoid runaway memory usage when many controls update simultaneously.
   - This is a Gallery-only safety measure and does not affect library logging APIs.

### Field-Tested Integration Notes (Session Takeaways)

These notes describe the native Windows integration:

1. **Pick the real surface as the host element**
   - For composite controls, attach neumorphism to the element that actually renders the surface.
   - Example: `DaisyCard` uses `PART_SolidBackground` through `GetNeumorphicHostElement()`.

2. **ComboBox popup anchoring after host changes**
   - When the host is an inner element, anchor the drop-down to the trigger button (not the outer control) so the popup aligns with the input instead of its shadow.

3. **Use native shadow types**
   - Use `DaisyNeumorphicHelper`, `DaisyShadowContainer`, and `DaisyShadow` in the native projects.

4. **Load native library resources**
   - Merge `ms-appx:///Flowery.WinUI/Themes/Generic.xaml` after `XamlControlsResources`.
   - A separate toolkit dependency is not required.

### Performance & Threading

- **Low-Priority Dispatching**: Updates to the neumorphic visuals are queued with `DispatcherQueuePriority.Low`. This prevents the UI thread from hanging when hundreds of controls update simultaneously (e.g., when toggling mode in the Sidebar).
- **Outer Shadows**: Windows Composition renders the native outer shadows.
- **Explicit Inner Shadows**: SkiaSharp creates alpha masks on CPU raster surfaces before WinUI displays them.

## Effects Usage

Neumorphic effects are integrated directly into the `DaisyBaseContentControl` class.

### Global Configuration (Opt-In Defaults)

Global settings provide **default values** for controls that don't have local overrides. They are **opt-in**, not forced:

- Controls with explicit local `NeumorphicEnabled`, `NeumorphicMode`, etc. ignore the global settings.
- `DaisyButton` has a **heuristic** that excludes certain buttons from global neumorphic:
  - Transparent variants (Ghost, Link) are excluded
  - Default variant + Default style buttons are excluded (protects sidebars, menus)
- Container controls (`DaisyButtonGroup`, `DaisyJoin`) explicitly disable neumorphic on their children.

**Automatic state synchronization:**

- Setting `GlobalNeumorphicMode` to a non-None value (Raised, Inset, Flat) automatically sets `GlobalNeumorphicEnabled = true`
- Setting `GlobalNeumorphicMode = None` automatically sets `GlobalNeumorphicEnabled = false`
- Setting `GlobalNeumorphicEnabled = true` when Mode was None **restores the last non-None mode** (defaults to Raised)
- The last non-None mode is remembered, so toggling Enabled on/off preserves the user's mode preference

This bidirectional synchronization ensures the dropdown selector works intuitively.

```csharp
// Set mode preference
DaisyBaseContentControl.GlobalNeumorphicMode = DaisyNeumorphicMode.Inset;

// Disable (remembers Inset)
DaisyBaseContentControl.GlobalNeumorphicEnabled = false;  // or Mode = None

// Re-enable (restores Inset, not Raised!)
DaisyBaseContentControl.GlobalNeumorphicEnabled = true;

// Fires GlobalNeumorphicChanged event so controls can refresh
```

### Per-Control Configuration (XAML)

```xml
<daisy:DaisyButton
    Content="Raised Button"
    NeumorphicEnabled="True"
    NeumorphicMode="Raised" />

<daisy:DaisyCard
    NeumorphicEnabled="True"
    NeumorphicMode="Inset"
    NeumorphicIntensity="0.7" />
```

### Available Modes (`DaisyNeumorphicMode`)

| Mode | Style Guide Term | Description |
| :--- | :--- | :--- |
| `None` | — | No effect applied. |
| `Raised` | Outer | Dark and light outer Composition shadows. |
| `Inset` | Inner | Element appears "pressed into" the surface. Uses gradient overlays. |
| `Flat` | — | Single subtle shadow (half of Raised elevation). |

**Button Interaction Pattern** (from style guide): For interactive buttons, animate between Raised (normal) and Inset (pressed) states on MouseDown/MouseUp to create a satisfying click feedback.

## 📐 Properties

| Property | Type | Default | Description |
| :--- | :--- | :--- | :--- |
| `NeumorphicEnabled` | `bool?` | `null` | Enables/disables the effect. Falls back to global setting. |
| `NeumorphicMode` | `DaisyNeumorphicMode?` | `null` | The visual style (Raised, Inset, etc.). Falls back to global. |
| `NeumorphicIntensity` | `double?` | `null` | Adjusts shadow opacity/strength (0.0-1.0). Falls back to global. |
| `NeumorphicDarkShadowColor` | `Color?` | `null` | Override dark shadow color. Falls back to theme-derived global. |
| `NeumorphicLightShadowColor` | `Color?` | `null` | Override light shadow color. Falls back to theme-derived global. |

### Windows-Specific Properties

| Property | Type | Default | Description |
| :--- | :--- | :--- | :--- |
| `DaisyNeumorphicHelper.UseRoundedShadowMask` | `bool` | `true` | When true, applies rounded rectangle mask to match button corner radius. |

## Developer Notes

### Native Helper Files

```text
Flowery.WinUI/Controls/
├── DaisyShadow.cs                    # Dependency properties and observable collection
├── DaisyShadowContainer.cs           # Outer Composition shadows and explicit inset masks
├── DaisyNeumorphicHelper.cs           # Layer ownership, mode selection, and gradient overlays
└── DaisyNeumorphicHelper.Windows.cs   # Direct native Composition shadow path
```

The helper tracks its installed child visual.
Cleanup detaches that visual only when the helper still owns it.

### Theme Integration (Shadow Colors)

Shadow colors are **automatically derived from the theme** when `DaisyThemeManager.ApplyTheme()` is called:

| Theme Type | Dark Shadow | Light Shadow |
| :--- | :--- | :--- |
| **Dark themes** | Pure black `#000000` | Pure white `#FFFFFF` |
| **Light themes** | Darkened Base300 (40%) | Pure white `#FFFFFF` |

The derivation happens in `DaisyThemeManager.UpdateNeumorphicShadowColors()` and updates:

- `DaisyBaseContentControl.GlobalNeumorphicDarkShadowColor`
- `DaisyBaseContentControl.GlobalNeumorphicLightShadowColor`

**Override Chain** (per-element → global):

```csharp
// Per-element via attached property (highest priority)
var darkColor = DaisyNeumorphic.GetDarkShadowColor(element)
    ?? DaisyBaseContentControl.GlobalNeumorphicDarkShadowColor; // Theme-derived fallback
```

### Native Windows Rendering

The native solution uses Windows Composition for outer shadows.
SkiaSharp provides explicit inset alpha masks for DaisyShadowContainer.
Browser and mobile elevation backends are outside the native build.

### Container Controls (DaisyButtonGroup, DaisyJoin)

**IMPORTANT**: Container controls like `DaisyButtonGroup` and `DaisyJoin` are the **single neumorphic surface** - they draw ONE unified shadow/border around all children.

Child controls inside these containers have neumorphic **explicitly disabled** via `DaisyNeumorphic.SetIsEnabled(child, false)`. This prevents:

- Overlapping shadows between adjacent buttons
- Duplicate borders that look wrong
- Visual artifacts from multiple neumorphic effects competing

The children are flat visual segments inside the container's neumorphic shell. The container itself (which inherits from `DaisyBaseContentControl`) provides the neumorphic effect for the whole group.

### Lifecycle Management

The base class automatically manages the creation and disposal of the neumorphic helper during `Loaded` and `Unloaded` events.
