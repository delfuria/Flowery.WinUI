using System.Runtime.CompilerServices;
using Flowery.Composition;
using Flowery.Services;
using Microsoft.UI.Xaml;

namespace Flowery.Theming;

/// <summary>
/// Applies accessibility preferences to content created by applications and controls.
/// </summary>
public static class DaisyHighContrast
{
    private static readonly ConditionalWeakTable<FrameworkElement, OpacitySubscription> OpacitySubscriptions = new();

    /// <summary>
    /// Identifies an element's normal opacity. Windows contrast themes keep this content opaque.
    /// </summary>
    public static readonly DependencyProperty ContentOpacityProperty = DependencyProperty.RegisterAttached(
        "ContentOpacity", typeof(double), typeof(DaisyHighContrast), new PropertyMetadata(1d, OnContentOpacityChanged));

    /// <summary>
    /// Sets the normal opacity and keeps the element opaque in Windows contrast themes.
    /// </summary>
    /// <param name="element">The content element.</param>
    /// <param name="value">The opacity used outside Windows contrast themes.</param>
    public static void SetContentOpacity(FrameworkElement element, double value)
    {
        if (GetContentOpacity(element) == value)
            element.Opacity = DaisyResourceLookup.GetContentOpacity(value);
        else
            element.SetValue(ContentOpacityProperty, value);
    }

    /// <summary>
    /// Sets the normal opacity and returns the element for programmatic content creation.
    /// </summary>
    /// <param name="element">The content element.</param>
    /// <param name="value">The opacity used outside Windows contrast themes.</param>
    /// <returns>The supplied element.</returns>
    public static T WithContentOpacity<T>(T element, double value) where T : FrameworkElement
    {
        SetContentOpacity(element, value);
        return element;
    }

    /// <summary>
    /// Gets the element's normal opacity outside Windows contrast themes.
    /// </summary>
    /// <param name="element">The content element.</param>
    public static double GetContentOpacity(FrameworkElement element) => (double)element.GetValue(ContentOpacityProperty);

    private static void OnContentOpacityChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        if (sender is FrameworkElement element)
            OpacitySubscriptions.GetValue(element, static owner => new OpacitySubscription(owner)).Apply();
    }

    private sealed class OpacitySubscription
    {
        private readonly FrameworkElement _owner;
        private bool _subscribed;

        internal OpacitySubscription(FrameworkElement owner)
        {
            _owner = owner;
            owner.Loaded += OnLoaded;
            owner.Unloaded += OnUnloaded;
            if (CompositionVisualTree.GetConnectedRoot(owner) is not null) Subscribe();
        }

        internal void Apply() => _owner.Opacity = DaisyResourceLookup.GetContentOpacity(GetContentOpacity(_owner));

        private void OnLoaded(object sender, RoutedEventArgs args)
        {
            Subscribe();
            Apply();
        }

        private void Subscribe()
        {
            if (_subscribed) return;
            DaisySystemAppearance.Initialize(_owner.XamlRoot);
            DaisySystemAppearance.ColorsChanged += OnColorsChanged;
            _subscribed = true;
        }

        private void OnUnloaded(object sender, RoutedEventArgs args)
        {
            DaisySystemAppearance.ColorsChanged -= OnColorsChanged;
            _subscribed = false;
        }

        private void OnColorsChanged(object? sender, EventArgs args) => Apply();
    }
}
