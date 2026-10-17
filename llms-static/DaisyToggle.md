<!-- Supplementary documentation for DaisyToggle -->
<!-- This content is merged into auto-generated docs by generate_docs.py -->

# Overview

DaisyToggle is a styled `ToggleSwitch` with **8 color variants** and **5 size presets**. It offers customizable thumb size via `TogglePadding` (knob margin) and uses smooth knob animation on state changes. Use it for binary on/off controls.

## Variant Options

| Variant | Description |
| --- | --- |
| Default | Neutral track and knob; darkens when checked. |
| Primary / Secondary / Accent | Colored track when checked. |
| Success / Warning / Info / Error | Semantic track colors when checked. |

## Size Options

| Size | Track (W×H) | Knob | Use Case |
| --- | --- | --- | --- |
| ExtraSmall | 28×14 | 8px | Dense layouts, tables. |
| Small | 32×16 | 10px | Compact forms. |
| Medium (default) | 40×20 | 12px | General purpose. |
| Large | 48×24 | 14px | Touch-friendly UIs. |
| ExtraLarge | 56×28 | 16px | Hero sections or large touch targets. |

The track is as tall as the line height of the label text. Medium follows the WinUI `ToggleSwitch` (40×20 track, 12 px knob), and all sizes keep its proportions.

## Additional Styling

| Property | Description |
| --- | --- |
| `TogglePadding` | Horizontal gap between knob and track border. The default (`NaN`) centers the knob with the same gap on every side; set a number to override it. |

## Quick Examples

```xml
<controls:DaisyToggle Content="Toggle" />
<controls:DaisyToggle Content="Primary" Variant="Primary" IsChecked="True" />
<controls:DaisyToggle Content="Error" Variant="Error" Size="Small" IsChecked="True" />
<controls:DaisyToggle Content="Large Switch" Variant="Accent" Size="Large" />
```

## Tips & Best Practices

- Match `Size` to surrounding controls; use Large for mobile or card headers, Small/XS for dense lists.
- Use semantic variants to convey meaning (Success for enabled/ready, Error for risky toggles).
- Leave `TogglePadding` at its automatic default unless a design needs a different knob inset; larger values shorten the knob travel.
- Bind `IsChecked` for state management; it inherits ToggleSwitch behavior.
