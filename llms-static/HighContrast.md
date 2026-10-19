# Windows contrast themes

Windows contrast themes take precedence over Daisy palettes and decorative brush overrides.
The library observes the native window and updates existing controls when the system colors change.
Controls also refresh their appearance after unload and reload.

## Color roles

| Resource | Windows role | Use |
| --- | --- | --- |
| `DaisyBase100Brush`, `DaisyBase200Brush`, `DaisyBase300Brush` | Window | Surfaces |
| `DaisyBaseContentBrush`, `DaisyBorderBrush` | WindowText | Body text and boundaries |
| `DaisyFocusBrush` | Highlight | Focus indication |
| Semantic accent brushes and their Content brushes | Highlight and HighlightText | Paired interaction and selection colors |
| `DaisyDisabledForegroundBrush` | GrayText | Disabled content |
| `DaisyLinkBrush` | Hotlight | Hyperlinks |

Do not use a surface brush for a visible boundary.
Use `DaisyResourceLookup.GetBorderBrush()` in C# or `{ThemeResource DaisyBorderBrush}` in XAML.
The border brush follows Base300 in ordinary Daisy themes.

The library ignores lightweight decorative brush overrides during contrast mode.
Those overrides apply again when contrast mode ends.
Color-picker samples, images and other color data retain their actual colors.
Their surrounding text, outlines and selection indicators use the system roles.
Decorative shadows and glass effects give way to solid surfaces and visible boundaries.
Loading indicators use a fully opaque rotating spinner in contrast mode.
The spinner follows the Windows animation setting; ordinary themes retain the selected loading variant.
The custom input controls supply system-aware colors for their native editors and prevent automatic backplates from covering their borders.

## Caption opacity

Reduced opacity can make text unreadable even when its foreground uses the correct system color.
Use the attached property for captions that normally use reduced opacity:

```xml
<TextBlock xmlns:theming="using:Flowery.Theming"
           Text="Description"
           theming:DaisyHighContrast.ContentOpacity="0.7" />
```

For programmatic content, use `DaisyHighContrast.SetContentOpacity(element, 0.7)`.
`WithContentOpacity(element, 0.7)` returns the supplied element for inline content creation.
These helpers retain the ordinary opacity and use full opacity in contrast mode.
They observe system changes while the element is loaded and release the observer on unload.

Use these helpers for authored captions and control states.
Keep animation visibility, hidden elements and data transparency under their existing policies.

## Application acceptance

Check the running application in all four Windows contrast themes.
Include keyboard focus, hover, pressed, selected, indeterminate, disabled and validation states where the control supports them.
Keep popups and dialogs open during a theme change.
Check the return to the ordinary theme and control unload/reload.
Custom application content remains responsible for its own semantic color pairs.

Microsoft documents the system color pairs in [Contrast themes](https://learn.microsoft.com/en-us/windows/apps/design/accessibility/high-contrast-themes).
