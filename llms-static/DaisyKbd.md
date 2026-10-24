<!-- Supplementary documentation for DaisyKbd -->
<!-- This content is merged into auto-generated docs by generate_docs.py -->

# Overview

DaisyKbd renders keyboard keycaps for inline hints (e.g., shortcuts). It provides **5 size presets**, monospace text, a subtle 3D border, and centered content. Use it to display keys like `Ctrl`, `Shift`, or `Cmd` in docs and tooltips.

## Size Options

`Size` defaults to `Auto`. `ResolvedSize` supplies the effective tier without replacing explicit values, styles, or bindings.

| Size | Height | Padding | Font Size |
| --- | --- | --- | --- |
| ExtraSmall | 16 | 2,0 | 10 |
| Small | 20 | 3,0 | 12 |
| Medium | 24 | 4,0 | 14 |
| Large | 32 | 6,0 | 18 |
| ExtraLarge | 40 | 8,0 | 20 |

> [!NOTE]
> DaisyKbd uses **fixed heights** for each size to match DaisyUI's keycap design.

## Quick Examples

```xml
<StackPanel Orientation="Horizontal" Spacing="4">
    <TextBlock Text="Press" VerticalAlignment="Center" />
    <controls:DaisyKbd Text="Ctrl" />
    <TextBlock Text="+" VerticalAlignment="Center" />
    <controls:DaisyKbd Text="S" />
    <TextBlock Text="to save." VerticalAlignment="Center" />
</StackPanel>

<controls:DaisyKbd Size="Small" Text="Esc" />
<controls:DaisyKbd Size="Large" Text="Shift" />
```

## Tips & Best Practices

- Keep content short (1–5 characters) for a key-like appearance.
- Use consistent sizes within a sentence to avoid visual jitter.
- Combine with instructions in tooltips or docs to clarify keyboard interactions.
