using System.Threading;
using System.Threading.Tasks;

namespace Flowery.WinUI.Kiosk;

internal static class KioskAnimation
{
    private static readonly Dictionary<FrameworkElement, Storyboard> ActiveFades = [];

    internal static void StopFade(FrameworkElement target)
    {
        if (ActiveFades.TryGetValue(target, out var storyboard))
            storyboard.Stop();
    }

    public static async Task FadeAsync(FrameworkElement target, double opacity, TimeSpan duration, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var storyboard = new Storyboard();
        var animation = new DoubleAnimation
        {
            To = opacity,
            Duration = new Duration(duration),
            EnableDependentAnimation = true
        };
        Storyboard.SetTarget(animation, target);
        Storyboard.SetTargetProperty(animation, "Opacity");
        storyboard.Children.Add(animation);
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void OnCompleted(object? sender, object args) => completion.TrySetResult();
        storyboard.Completed += OnCompleted;
        using var registration = cancellationToken.Register(() => completion.TrySetCanceled(cancellationToken));
        try
        {
            ActiveFades.Add(target, storyboard);
            storyboard.Begin();
            await completion.Task.WaitAsync(duration + TimeSpan.FromSeconds(10), cancellationToken);
            target.Opacity = opacity;
        }
        finally
        {
            storyboard.Completed -= OnCompleted;
            storyboard.Stop();
            if (ActiveFades.TryGetValue(target, out var active) && ReferenceEquals(active, storyboard))
                ActiveFades.Remove(target);
        }
    }

    public static async Task WaitForLayoutAsync(FrameworkElement element, CancellationToken cancellationToken)
    {
        if (element.IsLoaded && element.ActualWidth > 0 && element.ActualHeight > 0)
            return;
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void OnLayoutUpdated(object? sender, object args)
        {
            if (element.IsLoaded && element.ActualWidth > 0 && element.ActualHeight > 0)
                completion.TrySetResult();
        }
        element.LayoutUpdated += OnLayoutUpdated;
        try
        {
            await completion.Task.WaitAsync(TimeSpan.FromSeconds(15), cancellationToken);
        }
        finally
        {
            element.LayoutUpdated -= OnLayoutUpdated;
        }
    }
}
