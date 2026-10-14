<!-- Supplementary documentation for DaisyBadge -->
<!-- This content is merged into auto-generated docs by generate_docs.py -->

# Overview

DaisyBadge is a compact label for counts, statuses, or tags.
It supports ten color variants, five size presets, and an outline mode.
Use it alone or inside buttons and lists.

## Variant Options

| Variant | Description |
| --- | --- |
| Default | Neutral gray fill with contrasting text; fits most backgrounds. |
| Neutral | Muted neutral tone for secondary tags. |
| Primary | High-emphasis brand color. |
| Secondary | Alternate brand color. |
| Accent | Highlight color for special items. |
| Ghost | Subtle neutral fill; minimal visual weight. |
| Info | Informational blue tone. |
| Success | Positive/confirming green tone. |
| Warning | Cautionary amber tone. |
| Error | Critical/destructive red tone. |

## Size Options

The default tokens below use device-independent pixels. Applications can override them.

| Size | Minimum Height / Horizontal Padding / Font Size | Use Case |
| --- | --- | --- |
| ExtraSmall | 16 / 4 / 7 | Small counts inline with text |
| Small | 16 / 6 / 9 | Compact labels for dense lists |
| Medium (default) | 20 / 8 / 10 | General-purpose badges |
| Large | 24 / 12 / 12 | Emphasized tags |
| ExtraLarge | 28 / 16 / 14 | Large labels and counts |

## Outline Mode

| Property | Effect |
| --- | --- |
| `IsOutline=True` | Makes the badge transparent with a colored border and matching text. Border/text color follows `Variant` (Primary/Secondary/Accent) or uses base content color for Default. |

## Quick Examples

```xml
<!-- Basic badge -->
<controls:DaisyBadge Content="New" />

<!-- Color variants -->
<controls:DaisyBadge Content="Primary" Variant="Primary" />
<controls:DaisyBadge Content="Info" Variant="Info" />
<controls:DaisyBadge Content="Error" Variant="Error" />

<!-- Outline style -->
<controls:DaisyBadge Content="Outline" Variant="Primary" IsOutline="True" />
<controls:DaisyBadge Content="Ghost" Variant="Ghost" IsOutline="True" />

<!-- Sizes -->
<StackPanel Orientation="Horizontal" Spacing="6">
    <controls:DaisyBadge Content="XS" Size="ExtraSmall" />
    <controls:DaisyBadge Content="Small" Size="Small" />
    <controls:DaisyBadge Content="Medium" Size="Medium" />
    <controls:DaisyBadge Content="Large" Size="Large" />
    <controls:DaisyBadge Content="XL" Size="ExtraLarge" />
</StackPanel>

<!-- Inside a button -->
<controls:DaisyButton Variant="Ghost">
    <StackPanel Orientation="Horizontal" Spacing="8">
        <TextBlock Text="Messages" />
        <controls:DaisyBadge Content="5" Variant="Error" Size="Small" />
    </StackPanel>
</controls:DaisyButton>

<!-- Circular status dot (explicit sizing) -->
<controls:DaisyBadge Variant="Success" CornerRadius="6" Padding="0" 
                     Width="12" Height="12" MinWidth="12" MinHeight="12" />
```

## Tips & Best Practices

- Choose outline mode when placing badges on colored surfaces to avoid heavy fills.
- Keep text short (1–10 characters) to maintain the pill shape and legibility.
- Pair `Info/Success/Warning/Error` with matching semantic contexts (errors, confirmations, cautions).
- When embedding in other controls, pick a smaller size (Small/ExtraSmall) so the badge supports rather than competes with the parent.

## Explicit Sizing

When you set **explicit sizing properties** (`Width`, `Height`, `MinWidth`, `MinHeight`, `Padding`, `CornerRadius`), the badge respects those values instead of using token-derived defaults. This is essential for:

- **Circular status dots**: Set `Width="12" Height="12" MinWidth="12" MinHeight="12" CornerRadius="6" Padding="0"` for a perfect 12px circle.
- **Custom dimensions**: Override the size preset's default height when you need non-standard sizing.

Without explicit `MinHeight`, the badge uses the Size preset's token value (e.g., 20px for Medium), which may be larger than your explicit `Height`.
