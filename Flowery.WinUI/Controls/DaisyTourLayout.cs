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

    // Loaded handlers can still resize controls created during preparation, and a scroll
    // viewer that has not rendered yet applies an accepted offset to its content transform
    // only in the next frame. Bounds are measured after that frame.
    private static async Task NextFrameAsync(CancellationToken token)
    {
        var rendered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        EventHandler<object> handler = (_, _) => rendered.TrySetResult();
        CompositionTarget.Rendering += handler;
        try { await rendered.Task.WaitAsync(token); }
        finally { CompositionTarget.Rendering -= handler; }
    }

    /// <param name="frame">Space around each target, such as its highlight frame, that must become visible too.</param>
    internal static async Task PrepareTargetsAsync(IReadOnlyList<FrameworkElement> targets, FrameworkElement host,
        CancellationToken token, double frame = 0)
    {
        await SettleAsync(host, token);
        if (targets.Any(target => !target.IsLoaded))
        {
            await NextFrameAsync(token);
            await SettleAsync(host, token);
        }
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
            Rect? primary = null;
            foreach (var target in targets)
            {
                if (!IsDescendant(target, viewer) || !TryBounds(target, host, out _))
                    continue;
                var rect = target.TransformToVisual(viewer).TransformBounds(
                    new Rect(-frame, -frame, target.ActualWidth + 2 * frame, target.ActualHeight + 2 * frame));
                if (ReferenceEquals(target, targets[0])) primary = rect;
                if (union is { } previous) { previous.Union(rect); union = previous; }
                else union = rect;
            }
            if (union is not { } area)
                continue;
            // When all targets cannot fit, the step target (listed first) takes precedence.
            if (primary is { } main && (area.Width > viewer.ViewportWidth || area.Height > viewer.ViewportHeight))
                area = main;
            var x = ScrollDelta(area.X, area.Width, viewer.ViewportWidth);
            var y = ScrollDelta(area.Y, area.Height, viewer.ViewportHeight);
            var horizontal = Math.Clamp(viewer.HorizontalOffset + x, 0, viewer.ScrollableWidth);
            var vertical = Math.Clamp(viewer.VerticalOffset + y, 0, viewer.ScrollableHeight);
            var changed = horizontal != viewer.HorizontalOffset || vertical != viewer.VerticalOffset;
            if (viewer.ChangeView(horizontal, vertical, null, true) && changed)
                await NextFrameAsync(token);
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

    // Directions clockwise from east in 45° steps; positions above the target come first.
    private static readonly int[] AnnotationOrder = [6, 7, 5, 0, 4, 2, 1, 3];

    internal static Rect PlaceAnnotation(Rect target, Rect card, Size viewport, Size surface) =>
        PlaceAnnotation(target, card, viewport, surface, [], -1, false, out _);

    /// <summary>
    /// Places a surface beside the target in one of eight directions. A direction is valid when the
    /// surface stays inside the viewport and covers neither the target, the card, nor another highlight.
    /// The previous direction is kept while it remains valid. Direction is -1 when none is valid.
    /// </summary>
    internal static Rect PlaceAnnotation(Rect target, Rect card, Size viewport, Size surface,
        IReadOnlyList<Rect> highlights, int previous, bool rightToLeft, out int direction)
    {
        const double margin = 12;
        var order = AnnotationOrder.Select(index => rightToLeft ? (12 - index) % 8 : index);
        foreach (var index in previous >= 0 ? order.Prepend(previous) : order)
        {
            var candidate = AnnotationCandidate(target, surface, index);
            if (candidate.X >= margin && candidate.Y >= margin && candidate.Right <= viewport.Width - margin &&
                candidate.Bottom <= viewport.Height - margin && Overlap(candidate, card) == 0 &&
                highlights.All(highlight => Overlap(candidate, highlight) == 0))
            {
                direction = index;
                return candidate;
            }
        }
        // No direction fits: keep the surface in the viewport and cover as little as possible.
        direction = -1;
        return order.Select(index => AnnotationCandidate(target, surface, index))
            .Select(candidate => new Rect(
                Math.Clamp(candidate.X, margin, Math.Max(margin, viewport.Width - surface.Width - margin)),
                Math.Clamp(candidate.Y, margin, Math.Max(margin, viewport.Height - surface.Height - margin)),
                surface.Width, surface.Height))
            .OrderBy(candidate => Overlap(candidate, target) * 4 + Overlap(candidate, card) * 2 +
                highlights.Sum(highlight => Overlap(candidate, highlight)))
            .First();
    }

    private static Rect AnnotationCandidate(Rect target, Size surface, int direction)
    {
        const double gap = 28;
        var (dx, dy) = direction switch
        {
            0 => (1, 0), 1 => (1, 1), 2 => (0, 1), 3 => (-1, 1), 4 => (-1, 0), 5 => (-1, -1), 6 => (0, -1), _ => (1, -1)
        };
        // Diagonal positions start at the corner; the tail spans both axes there.
        var offset = dx != 0 && dy != 0 ? gap / 2 : gap;
        var x = dx > 0 ? target.Right + offset : dx < 0 ? target.Left - offset - surface.Width : target.X + (target.Width - surface.Width) / 2;
        var y = dy > 0 ? target.Bottom + offset : dy < 0 ? target.Top - offset - surface.Height : target.Y + (target.Height - surface.Height) / 2;
        return new Rect(x, y, surface.Width, surface.Height);
    }

    private static double Overlap(Rect first, Rect second)
    {
        first.Intersect(second);
        return first.IsEmpty ? 0 : first.Width * first.Height;
    }
}
