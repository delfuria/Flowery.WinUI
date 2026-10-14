using System.Collections.ObjectModel;

namespace Flowery.Controls;

/// <summary>Describes a native shadow in device-independent pixels.</summary>
public sealed partial class DaisyShadow : DependencyObject
{
    public static readonly DependencyProperty ColorProperty = DependencyProperty.Register(
        nameof(Color), typeof(Color), typeof(DaisyShadow), new PropertyMetadata(Colors.Black, OnChanged));
    public static readonly DependencyProperty OffsetXProperty = DependencyProperty.Register(
        nameof(OffsetX), typeof(double), typeof(DaisyShadow), new PropertyMetadata(0d, OnChanged));
    public static readonly DependencyProperty OffsetYProperty = DependencyProperty.Register(
        nameof(OffsetY), typeof(double), typeof(DaisyShadow), new PropertyMetadata(0d, OnChanged));
    public static readonly DependencyProperty BlurRadiusProperty = DependencyProperty.Register(
        nameof(BlurRadius), typeof(double), typeof(DaisyShadow), new PropertyMetadata(5d, OnChanged));
    public static readonly DependencyProperty SpreadProperty = DependencyProperty.Register(
        nameof(Spread), typeof(double), typeof(DaisyShadow), new PropertyMetadata(0d, OnChanged));
    public static readonly DependencyProperty OpacityProperty = DependencyProperty.Register(
        nameof(Opacity), typeof(double), typeof(DaisyShadow), new PropertyMetadata(1d, OnChanged));
    public static readonly DependencyProperty IsInnerProperty = DependencyProperty.Register(
        nameof(IsInner), typeof(bool), typeof(DaisyShadow), new PropertyMetadata(false, OnChanged));

    public Color Color { get => (Color)GetValue(ColorProperty); set => SetValue(ColorProperty, value); }
    public double OffsetX { get => (double)GetValue(OffsetXProperty); set => SetValue(OffsetXProperty, value); }
    public double OffsetY { get => (double)GetValue(OffsetYProperty); set => SetValue(OffsetYProperty, value); }
    public double BlurRadius { get => (double)GetValue(BlurRadiusProperty); set => SetValue(BlurRadiusProperty, value); }
    public double Spread { get => (double)GetValue(SpreadProperty); set => SetValue(SpreadProperty, value); }
    public double Opacity { get => (double)GetValue(OpacityProperty); set => SetValue(OpacityProperty, value); }
    public bool IsInner { get => (bool)GetValue(IsInnerProperty); set => SetValue(IsInnerProperty, value); }

    internal event EventHandler? Changed;

    private static void OnChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        => ((DaisyShadow)sender).Changed?.Invoke(sender, EventArgs.Empty);
}

/// <summary>A mutable collection of native shadow descriptions.</summary>
public sealed class DaisyShadowCollection : ObservableCollection<DaisyShadow>
{
}
