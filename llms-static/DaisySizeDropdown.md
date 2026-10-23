# DaisySizeDropdown

A dropdown control for selecting a global size (`DaisySize`) that affects (almost) all Flowery.WinUI controls.

## Overview

`DaisySizeDropdown` selects the global concrete tier through `FlowerySizeManager`. Automatic controls update. Explicit sizes, styles, and bindings retain their values.

## Basic Usage

```xml
xmlns:controls="using:Flowery.Controls"

<controls:DaisySizeDropdown Width="180" />
```

This displays a dropdown with all five sizes (ExtraSmall, Small, Medium, Large, ExtraLarge) using localized display names.

## Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `SelectedSize` | `DaisySize` | `Medium` | The currently selected size |
| `Size` | `DaisySize` | `Auto` | Authored dropdown size. `ResolvedSize` supplies its effective tier independently of `SelectedSize`. |
| `ShowAbbreviations` | `bool` | `false` | Show abbreviations (XS, S, M, L, XL) instead of full names |
| `SizeOptions` | `IList<SizePreviewInfo>` | `null` | Custom size options to display |

---

## Customizing Available Sizes

By default, all five sizes are shown. You can customize which sizes appear and what they're called using the `SizeOptions` property.

### SizePreviewInfo Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `Size` | `DaisySize` | - | The enum value (required) |
| `Name` | `string` | - | Internal name for localization lookup (e.g., "Small") |
| `IsVisible` | `bool` | `true` | Set to `false` to hide this size from the dropdown |
| `DisplayNameOverride` | `string?` | `null` | Custom label that overrides localized text |
| `Abbreviation` | `string` | - | Short form for compact display (XS, S, M, L, XL) |

### Example: Show Only 3 Sizes with Custom Names

```xml
<controls:DaisySizeDropdown>
    <controls:DaisySizeDropdown.SizeOptions>
        <x:Array Type="{x:Type controls:SizePreviewInfo}">
            <controls:SizePreviewInfo Size="Small" Name="Small" 
                                      DisplayNameOverride="Compact" Abbreviation="C" />
            <controls:SizePreviewInfo Size="Medium" Name="Medium" 
                                      DisplayNameOverride="Normal" Abbreviation="N" />
            <controls:SizePreviewInfo Size="Large" Name="Large" 
                                      DisplayNameOverride="Large" Abbreviation="L" />
        </x:Array>
    </controls:DaisySizeDropdown.SizeOptions>
</controls:DaisySizeDropdown>
```

### Example: Hide ExtraSmall and ExtraLarge

```xml
<controls:DaisySizeDropdown>
    <controls:DaisySizeDropdown.SizeOptions>
        <x:Array Type="{x:Type controls:SizePreviewInfo}">
            <controls:SizePreviewInfo Size="ExtraSmall" Name="ExtraSmall" IsVisible="False" />
            <controls:SizePreviewInfo Size="Small" Name="Small" />
            <controls:SizePreviewInfo Size="Medium" Name="Medium" />
            <controls:SizePreviewInfo Size="Large" Name="Large" />
            <controls:SizePreviewInfo Size="ExtraLarge" Name="ExtraLarge" IsVisible="False" />
        </x:Array>
    </controls:DaisySizeDropdown.SizeOptions>
</controls:DaisySizeDropdown>
```

---

## Localization

Size names are automatically localized based on the current culture. The display name resolution order is:

1. **DisplayNameOverride** (if set) – your custom label
2. **Localized text** – from `FloweryLocalization` (e.g., "Klein" in German)
3. **Name** (fallback) – the enum name like "ExtraSmall"

### Supported Languages

Size names are localized in 12 languages:

- English, German, French, Spanish, Italian
- Japanese, Korean, Chinese (Simplified)
- Arabic, Turkish, Ukrainian, Hebrew

### Custom Localization Keys

If you need to override the built-in translations, add these keys to your localization:

```json
{
  "Size_ExtraSmall": "Extra Small",
  "Size_Small": "Small", 
  "Size_Medium": "Medium",
  "Size_Large": "Large",
  "Size_ExtraLarge": "Extra Large"
}
```

---

## Styling

The automatic dropdown updates its appearance when the global size changes. A concrete dropdown `Size` retains its tier while selection still synchronizes.

### Size Variants

```xml
<!-- Use the dropdown's own Size property for its appearance -->
<controls:DaisySizeDropdown Size="Small" Width="120" />
<controls:DaisySizeDropdown Size="Large" Width="200" />
```

### Automatic MinWidth

The control automatically calculates a `MinWidth` based on the current `Size` to ensure:

1. The size name text fits without clipping
2. The dropdown arrow/glyph is fully visible (requires ~26px)

This is particularly important at smaller sizes (ExtraSmall, Small) where the WinUI ComboBox template expects more space than the control height provides.

**Calculated MinWidth**: approximately `116px` (text area + dropdown glyph margin)

If you need a narrower dropdown, consider:

- Using `ShowAbbreviations="True"` to display shorter labels (XS, S, M, L, XL)
- Providing custom `SizeOptions` with shorter `DisplayNameOverride` values

```xml
<!-- Compact dropdown with abbreviations -->
<controls:DaisySizeDropdown ShowAbbreviations="True" Width="80" />
```

> **Note:** Setting `Width` below `MinWidth` will be overridden. To force a smaller width, you must also set `MinWidth` explicitly, but this may cause visual clipping.

---

## Programmatic Control

If you need to change the size without the UI:

```csharp
using Flowery.Controls;
using Flowery.Services;

// Apply a size globally
FlowerySizeManager.ApplySize(DaisySize.Large);

// Check current size
DaisySize current = FlowerySizeManager.CurrentSize;
```

The dropdown automatically syncs with `FlowerySizeManager.CurrentSize`.

---

## Integration with FlowerySizeManager

`DaisySizeDropdown` is designed to work seamlessly with the global sizing system:

- **On selection change**: Calls `FlowerySizeManager.ApplySize()`
- **On external size change**: Syncs to match the current global size
- **On culture change**: Refreshes localized display names

For comprehensive documentation on the sizing system, see [SizingAndScaling](https://tobitege.github.io/Flowery.WinUI/#SizingAndScaling).

---

## Best Practices

1. **Place in settings or sidebar** – Give users easy access to change the size preference.

2. **Consider visibility** – For most apps, 3 sizes (Small, Medium, Large) may be enough.

3. **Use meaningful names** – If "Small" doesn't fit your domain, use `DisplayNameOverride` (e.g., "Compact", "Dense").

4. **Test all sizes** – Ensure your layouts work at both ExtraSmall and ExtraLarge.

5. **Persist preference** – Save the user's selection and restore it on app startup.
