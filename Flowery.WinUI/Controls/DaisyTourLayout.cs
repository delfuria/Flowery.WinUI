using System.Threading;
using System.Threading.Tasks;
using Flowery.Composition;
using Microsoft.UI.Dispatching;

namespace Flowery.Controls;

internal static class DaisyTourLayout
{
    internal static string Describe(FrameworkElement? target, FrameworkElement host)
    {
        if (target is null) return "No target was supplied.";
        var details = new List<string>();
        for (DependencyObject? current = target; current is not null; current = VisualTreeHelper.GetParent(current))
        {
            if (current is FrameworkElement element)
                details.Add($"{element.GetType().Name}({element.Name}): {element.Visibility}, opacity={element.Opacity}, size={element.ActualWidth}x{element.ActualHeight}");
        }
        return $"Same root={target.XamlRoot == host.XamlRoot}; connected={CompositionVisualTree.GetConnectedRoot(target) is not null}. {string.Join("; ", details)}";
    }

    internal static bool TryBounds(FrameworkElement target, FrameworkElement host, out Rect bounds)
    {
        bounds = default;
        if (CompositionVisualTree.GetConnectedRoot(target) is not { } root || root != host.XamlRoot)
            return false;
        for (DependencyObject? element = target; element is not null; element = VisualTreeHelper.GetParent(element))
        {
            if (element is UIElement { Visibility: Visibility.Collapsed } || element is UIElement { Opacity: <= 0 })
                return false;
        }
        if (target.ActualWidth <= 0 || target.ActualHeight <= 0)
            return false;
        bounds = target.TransformToVisual(host).TransformBounds(new Rect(0, 0, target.ActualWidth, target.ActualHeight));
        return double.IsFinite(bounds.X) && double.IsFinite(bounds.Y);
    }

    internal static Rect VisibleBounds(FrameworkElement target, FrameworkElement host, Rect bounds)
    {
        bounds.Intersect(new Rect(0, 0, host.ActualWidth, host.ActualHeight));
        for (DependencyObject? element = target; element is not null; element = VisualTreeHelper.GetParent(element))
        {
            if (element is ScrollViewer viewer)
            {
                var viewport = viewer.TransformToVisual(host).TransformBounds(
                    new Rect(0, 0, Math.Min(viewer.ActualWidth, viewer.ViewportWidth), Math.Min(viewer.ActualHeight, viewer.ViewportHeight)));
                bounds.Intersect(viewport);
            }
        }
        return bounds;
    }

    internal static async Task SettleAsync(FrameworkElement host, CancellationToken token)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!host.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
        {
            host.XamlRoot?.Content.UpdateLayout();
            host.UpdateLayout();
            completion.TrySetResult();
        }))
            throw new OperationCanceledException(token);
        await completion.Task.WaitAsync(token);
    }

    internal static async Task PrepareTargetsAsync(IReadOnlyList<FrameworkElement> targets, FrameworkElement host, CancellationToken token)
    {
        await SettleAsync(host, token);
        // Innermost scroll containers are adjusted first. Shared containers use the
        // union, so preparing a connector cannot scroll its first endpoint away.
        var viewers = new List<ScrollViewer>();
        foreach (var target in targets)
        {
            for (DependencyObject? current = target; current is not null; current = VisualTreeHelper.GetParent(current))
            {
                if (current is ScrollViewer viewer && !viewers.Contains(viewer))
                    viewers.Add(viewer);
            }
        }
        foreach (var viewer in viewers.OrderByDescending(Depth))
        {
            Rect? union = null;
            foreach (var target in targets)
            {
                if (!IsDescendant(target, viewer) || !TryBounds(target, host, out _))
                    continue;
                var rect = target.TransformToVisual(viewer).TransformBounds(new Rect(0, 0, target.ActualWidth, target.ActualHeight));
                if (union is { } previous) { previous.Union(rect); union = previous; }
                else union = rect;
            }
            if (union is not { } area)
                continue;
            var x = ScrollDelta(area.X, area.Width, viewer.ViewportWidth);
            var y = ScrollDelta(area.Y, area.Height, viewer.ViewportHeight);
            viewer.ChangeView(Math.Clamp(viewer.HorizontalOffset + x, 0, viewer.ScrollableWidth),
                Math.Clamp(viewer.VerticalOffset + y, 0, viewer.ScrollableHeight), null, true);
            await SettleAsync(host, token);
        }
    }

    private static double ScrollDelta(double start, double length, double viewport) =>
        start < 0 || length > viewport ? start : Math.Max(0, start + length - viewport);

    private static int Depth(DependencyObject element)
    {
        var depth = 0;
        for (var current = VisualTreeHelper.GetParent(element); current is not null; current = VisualTreeHelper.GetParent(current)) depth++;
        return depth;
    }

    internal static bool IsDescendant(DependencyObject element, DependencyObject ancestor)
    {
        for (DependencyObject? current = element; current is not null; current = VisualTreeHelper.GetParent(current))
        {
            if (current.Equals(ancestor)) return true;
        }
        return false;
    }

    internal static Rect Place(Rect target, Size card, Size viewport, DaisyTourPlacement preferred)
    {
        const double gap = 32;
        const double margin = 12;
        var centeredX = target.X + (target.Width - card.Width) / 2;
        var centeredY = target.Y + (target.Height - card.Height) / 2;
        var candidates = new Dictionary<DaisyTourPlacement, Rect>
        {
            [DaisyTourPlacement.Bottom] = new(centeredX, target.Bottom + gap, card.Width, card.Height),
            [DaisyTourPlacement.Top] = new(centeredX, target.Top - card.Height - gap, card.Width, card.Height),
            [DaisyTourPlacement.Right] = new(target.Right + gap, centeredY, card.Width, card.Height),
            [DaisyTourPlacement.Left] = new(target.Left - card.Width - gap, centeredY, card.Width, card.Height)
        };
        var order = new[] { preferred, DaisyTourPlacement.Bottom, DaisyTourPlacement.Top, DaisyTourPlacement.Right, DaisyTourPlacement.Left };
        foreach (var side in order.Distinct())
        {
            if (candidates.TryGetValue(side, out var rect) && rect.X >= margin && rect.Y >= margin &&
                rect.Right <= viewport.Width - margin && rect.Bottom <= viewport.Height - margin)
                return rect;
        }
        var fallback = candidates[preferred == DaisyTourPlacement.Auto ? DaisyTourPlacement.Bottom : preferred];
        return new Rect(Math.Clamp(fallback.X, margin, Math.Max(margin, viewport.Width - card.Width - margin)),
            Math.Clamp(fallback.Y, margin, Math.Max(margin, viewport.Height - card.Height - margin)), card.Width, card.Height);
    }

    internal static Rect PlaceAnnotation(Rect target, Rect card, Size viewport, Size surface)
    {
        var candidates = new List<Rect>();
        foreach (var obstacle in new[] { target, card })
        {
            foreach (var side in new[] { DaisyTourPlacement.Top, DaisyTourPlacement.Bottom, DaisyTourPlacement.Left, DaisyTourPlacement.Right })
                candidates.Add(Place(obstacle, surface, viewport, side));
        }
        return candidates.OrderBy(candidate => Overlap(candidate, card) * 2 + Overlap(candidate, target))
            .ThenBy(candidate => Math.Abs(candidate.X - target.X) + Math.Abs(candidate.Y - target.Y)).First();
    }

    private static double Overlap(Rect first, Rect second)
    {
        first.Intersect(second);
        return first.IsEmpty ? 0 : first.Width * first.Height;
    }
}
