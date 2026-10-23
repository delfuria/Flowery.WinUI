# FlowerySizeManager

FlowerySizeManager selects a discrete size tier for loaded Daisy controls.
It refreshes their rendering without writing authored Size values.

## Size ownership

Size defaults to DaisySize.Auto. ResolvedSize reports the effective concrete tier.
Explicit XAML, code, styles, BasedOn styles and bindings retain normal dependency-property precedence.
An explicit Medium remains explicit even when the global tier is Medium.
ClearValue restores the underlying style or Auto default.

Auto resolves through the nearest composite override, the enabled global default, then a composite or control fallback.
Most controls fall back to Medium; ModifierKeys, Divider and TagPicker use Small, and Fab uses Large.
A concrete child Size always wins over its composite's default.

IgnoreGlobalSize uses the nearest explicitly authored attached value on the control or its ancestors.
A child can opt back in with IgnoreGlobalSize=False.
Loaded registrations are released on unload and restored on reload.
Responsive TextBlocks follow the same load/unload lifetime.

## Public API

```csharp
using Flowery.Controls;
using Flowery.Enums;

// ApplySize accepts concrete tiers only. Auto is a control policy.
FlowerySizeManager.ApplySize(DaisySize.Large);
DaisySize current = FlowerySizeManager.CurrentSize;

// Both switches default to true.
FlowerySizeManager.UseGlobalSizeByDefault = true;
FlowerySizeManager.EnableGlobalAutoSize = true;

// Refresh explicitly even when automatic refresh is disabled.
FlowerySizeManager.RefreshAllSizes();

// Reset selects Small. Initial CurrentSize is Medium.
FlowerySizeManager.Reset();
```

ApplySize has no string overload. ApplySize(Auto) throws ArgumentOutOfRangeException.
EnableGlobalAutoSize controls automatic refresh of existing visuals.
It does not freeze CurrentSize or ResolvedSize.
UseGlobalSizeByDefault controls whether Auto uses the global fallback.
Changing that policy or re-enabling automatic refresh updates loaded controls.

MainWindow remains an application window reference.
Registration also covers loaded controls in other windows and popup roots.
Continuous scaling and operating-system DPI are separate mechanisms.

```xml
<StackPanel controls:FlowerySizeManager.IgnoreGlobalSize="True">
    <controls:DaisyButton Content="Control fallback" />
    <controls:DaisyButton Size="Medium" Content="Explicit Medium" />
    <controls:DaisyButton controls:FlowerySizeManager.IgnoreGlobalSize="False"
                          Content="Global default" />
</StackPanel>
```

## Native Windows metrics

| Tier | Control height | Primary font | Sidebar width |
| --- | --- | --- | --- |
| ExtraSmall | 24 | 10 | 190 |
| Small | 28 | 12 | 205 |
| Medium | 32 | 14 | 220 |
| Large | 36 | 18 | 235 |
| ExtraLarge | 40 | 20 | 250 |

These are device-independent units. Application resource overrides can replace tokens.
DaisyResourceLookup.GetDefaultHeight returns the native code fallback.
GetSidebarWidth returns the sidebar ladder.
Specialized controls, such as Badge and Kbd, use their own height tokens.

| ResponsiveFont tier | XS / S / M / L / XL |
| --- | --- |
| Primary | 10 / 12 / 14 / 18 / 20 |
| Secondary | 9 / 10 / 12 / 14 / 16 |
| Tertiary | 8 / 9 / 11 / 12 / 14 |
| SectionHeader | 12 / 14 / 16 / 20 / 22 |
| Header | 14 / 16 / 20 / 24 / 28 |

```xml
<TextBlock Text="Body" controls:FlowerySizeManager.ResponsiveFont="Primary" />
```

## Custom controls

Derive from DaisyBaseContentControl to use the existing registration contract.
Use public EnsureTokens and ResolvedSize when calculating dimensions.

```csharp
using Flowery.Controls;
using Flowery.Enums;
using Flowery.Theming;
using Microsoft.UI.Xaml;

public class MyDaisyControl : DaisyBaseContentControl
{
    public static readonly DependencyProperty SizeProperty =
        DependencyProperty.Register(nameof(Size), typeof(DaisySize),
            typeof(MyDaisyControl), new PropertyMetadata(DaisySize.Auto,
                static (sender, _) => ((MyDaisyControl)sender).ApplySizing()));

    public DaisySize Size
    {
        get => (DaisySize)GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    protected override DaisySize? GetControlSize() => Size;
    protected override void OnResolvedSizeChanged()
    {
        base.OnResolvedSizeChanged();
        ApplySizing();
    }

    protected override void OnLoaded()
    {
        base.OnLoaded();
        ApplySizing();
    }

    private void ApplySizing()
    {
        DaisyResourceLookup.EnsureTokens();
        Height = DaisyResourceLookup.GetDefaultHeight(ResolvedSize);
        FontSize = DaisyResourceLookup.GetDefaultFontSize(ResolvedSize);
    }
}
```

The theme lifecycle helper handles theme subscriptions only.
Do not add another global Size writer or assign Size in a size-refresh callback.
DaisySizeDropdown selects a concrete global tier and keeps its own authored control Size separate.
