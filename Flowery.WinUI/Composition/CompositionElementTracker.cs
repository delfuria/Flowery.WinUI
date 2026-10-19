using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Flowery.Composition;

internal readonly record struct CompositionElementBounds(Rect Bounds, double RasterizationScale, XamlRoot XamlRoot);

/// <summary>Reports changed XAML bounds in a host's DIP coordinates after layout or scrolling.</summary>
internal sealed class CompositionElementTracker : IDisposable
{
    private readonly FrameworkElement _target;
    private readonly FrameworkElement _host;
    private readonly Action<CompositionElementBounds?> _changed;
    private readonly HashSet<ScrollViewer> _scrollViewers = [];
    private XamlRoot? _root;
    private CompositionElementBounds? _bounds;
    private bool _queued;
    private bool _disposed;

    internal CompositionElementTracker(FrameworkElement target, FrameworkElement host, Action<CompositionElementBounds?> changed)
    {
        _target = target;
        _host = host;
        _changed = changed;
        target.Loaded += OnLoaded;
        target.Unloaded += OnUnloaded;
        host.Loaded += OnLoaded;
        host.Unloaded += OnUnloaded;
        target.LayoutUpdated += OnLayoutUpdated;
        QueueUpdate();
    }

    private void OnLoaded(object sender, RoutedEventArgs args)
    {
        StopObserving();
        QueueUpdate();
    }
    private void OnUnloaded(object sender, RoutedEventArgs args)
    {
        StopObserving();
        Publish(null);
    }
    private void OnLayoutUpdated(object? sender, object args) => QueueUpdate();
    private void OnRootChanged(XamlRoot sender, XamlRootChangedEventArgs args) => QueueUpdate();
    private void OnViewChanged(object? sender, ScrollViewerViewChangedEventArgs args) => QueueUpdate();

    private void QueueUpdate()
    {
        if (_disposed || _queued)
            return;
        _queued = true;
        if (!_target.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, Update))
            _queued = false;
    }

    private void Update()
    {
        _queued = false;
        if (_disposed)
            return;
        if (CompositionVisualTree.GetConnectedRoot(_target) is not { } root ||
            root != CompositionVisualTree.GetConnectedRoot(_host))
        {
            StopObserving();
            Publish(null);
            return;
        }

        if (_root != root)
        {
            StopObserving();
            _root = root;
            root.Changed += OnRootChanged;
            ObserveScrollViewers(_target);
            ObserveScrollViewers(_host);
        }

        if (_target.Visibility != Visibility.Visible || _target.ActualWidth <= 0 || _target.ActualHeight <= 0)
        {
            Publish(null);
            return;
        }

        var bounds = _target.TransformToVisual(_host).TransformBounds(new Rect(0, 0, _target.ActualWidth, _target.ActualHeight));
        Publish(new CompositionElementBounds(bounds, root.RasterizationScale, root));
    }

    private void ObserveScrollViewers(DependencyObject element)
    {
        for (DependencyObject? current = element; current is not null; current = VisualTreeHelper.GetParent(current))
        {
            if (current is ScrollViewer scrollViewer && _scrollViewers.Add(scrollViewer))
                scrollViewer.ViewChanged += OnViewChanged;
        }
    }

    private void Publish(CompositionElementBounds? bounds)
    {
        if (_bounds == bounds)
            return;
        _bounds = bounds;
        _changed(bounds);
    }

    private void StopObserving()
    {
        if (_root is not null)
            _root.Changed -= OnRootChanged;
        _root = null;
        foreach (var scrollViewer in _scrollViewers)
            scrollViewer.ViewChanged -= OnViewChanged;
        _scrollViewers.Clear();
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _target.Loaded -= OnLoaded;
        _target.Unloaded -= OnUnloaded;
        _host.Loaded -= OnLoaded;
        _host.Unloaded -= OnUnloaded;
        _target.LayoutUpdated -= OnLayoutUpdated;
        StopObserving();
    }
}
