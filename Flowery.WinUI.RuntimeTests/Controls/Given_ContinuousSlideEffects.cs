using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Flowery.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Flowery.WinUI.RuntimeTests.Controls
{
    [TestClass]
    public class Given_ContinuousSlideEffects
    {
        [TestMethod]
        public async Task When_AutoStartIsDisabledBeforeLoading_PendingStartIsCanceled()
        {
            var target = CreateTarget();
            FlowerySlideEffects.SetEffect(target, FlowerySlideEffect.ZoomIn);
            FlowerySlideEffects.SetAutoStart(target, false);
            FlowerySlideEffects.SetDuration(target, 2);
            try
            {
                RuntimeTestHelpers.AttachToHost(target);
                await RuntimeTestHelpers.EnsureLoadedAsync(target).WaitAsync(TimeSpan.FromSeconds(5));
                // A canceled animation need not produce further render frames.
                // Verify after the Loaded callbacks and their dispatcher turn finish.
                await CompleteDispatcherTurnAsync(target);
                Assert.IsTrue(IsIdentity(target), "A canceled pending effect changed the loaded element.");
                FlowerySlideEffects.StartEffect(target);
                await WaitForFrameAsync(() => GetTransform(target).ScaleX > 1.01);
            }
            finally
            {
                FlowerySlideEffects.StopEffect(target);
                RuntimeTestHelpers.GetHostPanel().Children.Clear();
            }
        }

        [TestMethod]
        public async Task When_ManualEffectUnloads_ItStopsAndAutomaticEffectRearmsOnReload()
        {
            var target = CreateTarget();
            FlowerySlideEffects.SetAutoStart(target, false);
            FlowerySlideEffects.SetEffect(target, FlowerySlideEffect.ZoomIn);
            FlowerySlideEffects.SetDuration(target, 2);
            try
            {
                RuntimeTestHelpers.AttachToHost(target);
                await RuntimeTestHelpers.EnsureLoadedAsync(target).WaitAsync(TimeSpan.FromSeconds(5));
                FlowerySlideEffects.StartEffect(target);
                await WaitForFrameAsync(() => GetTransform(target).ScaleX > 1.01);
                await DetachAsync(target);
                Assert.IsTrue(IsIdentity(target), "A manually started animation must release its transform on unload.");

                FlowerySlideEffects.SetAutoStart(target, true);
                RuntimeTestHelpers.AttachToHost(target);
                await RuntimeTestHelpers.EnsureLoadedAsync(target).WaitAsync(TimeSpan.FromSeconds(5));
                await WaitForFrameAsync(() => GetTransform(target).ScaleX > 1.01);
                await DetachAsync(target);
                Assert.IsTrue(IsIdentity(target), "An automatic animation must stop on unload.");
                RuntimeTestHelpers.AttachToHost(target);
                await RuntimeTestHelpers.EnsureLoadedAsync(target).WaitAsync(TimeSpan.FromSeconds(5));
                await WaitForFrameAsync(() => GetTransform(target).ScaleX > 1.01);
            }
            finally
            {
                FlowerySlideEffects.StopEffect(target);
                RuntimeTestHelpers.GetHostPanel().Children.Clear();
            }
        }

        [TestMethod]
        public async Task When_PanAndZoomIsLocked_ScaleStaysConstantWhileContentMoves()
        {
            var target = CreateTarget();
            var viewport = new Border
            {
                Width = 256,
                Height = 192,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Child = target
            };
            FlowerySlideEffects.SetAutoStart(target, false);
            FlowerySlideEffects.SetEffect(target, FlowerySlideEffect.PanAndZoom);
            FlowerySlideEffects.SetDuration(target, 2);
            FlowerySlideEffects.SetZoomIntensity(target, 0.3);
            FlowerySlideEffects.SetPanAndZoomLockZoom(target, true);
            FlowerySlideEffects.SetPanAndZoomPanSpeed(target, 10);
            try
            {
                RuntimeTestHelpers.AttachToHost(viewport);
                await RuntimeTestHelpers.EnsureLoadedAsync(target).WaitAsync(TimeSpan.FromSeconds(5));
                FlowerySlideEffects.StartEffect(target);
                double? firstScale = null;
                double firstX = 0, firstY = 0;
                await WaitForFrameAsync(() =>
                {
                    var transform = GetTransform(target);
                    if (firstScale is null)
                    {
                        firstScale = transform.ScaleX;
                        firstX = transform.TranslateX;
                        firstY = transform.TranslateY;
                        Assert.IsTrue(firstScale > 1.5, "The attached zoom intensity must reach the pan-and-zoom cover scale.");
                    }
                    Assert.AreEqual(firstScale.Value, transform.ScaleX, 0.000001, "Pan-only must keep its horizontal scale.");
                    Assert.AreEqual(firstScale.Value, transform.ScaleY, 0.000001, "Pan-only must keep its vertical scale.");
                    return Math.Abs(transform.TranslateX - firstX) + Math.Abs(transform.TranslateY - firstY) >= 2;
                });
            }
            finally
            {
                FlowerySlideEffects.StopEffect(target);
                RuntimeTestHelpers.GetHostPanel().Children.Clear();
            }
        }

        private static Border CreateTarget() => new()
        {
            Width = 256,
            Height = 192,
            Background = new SolidColorBrush(Microsoft.UI.Colors.CornflowerBlue),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top
        };

        private static CompositeTransform GetTransform(UIElement target) =>
            target.RenderTransform as CompositeTransform
            ?? throw new AssertFailedException("The effect did not create a native transform.");

        private static bool IsIdentity(UIElement target) => target.RenderTransform is not CompositeTransform transform
            || (transform.ScaleX == 1 && transform.ScaleY == 1 && transform.TranslateX == 0 && transform.TranslateY == 0);

        private static Task CompleteDispatcherTurnAsync(FrameworkElement target)
        {
            var completion = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
            if (!target.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,
                () => completion.TrySetResult(null)))
                throw new InvalidOperationException("The native dispatcher rejected the animation verification action.");
            return completion.Task.WaitAsync(TimeSpan.FromSeconds(5));
        }

        private static async Task WaitForFrameAsync(Func<bool> condition)
        {
            var completion = new TaskCompletionSource<object?>();
            var elapsed = Stopwatch.StartNew();
            EventHandler<RenderedEventArgs>? handler = null;
            handler = (_, _) =>
            {
                try
                {
                    if (condition()) completion.TrySetResult(null);
                    else if (elapsed.Elapsed > TimeSpan.FromSeconds(3))
                        completion.TrySetException(new AssertFailedException("The native animation did not reach the expected frame."));
                }
                catch (Exception error) { completion.TrySetException(error); }
            };
            CompositionTarget.Rendered += handler;
            try { await completion.Task.WaitAsync(TimeSpan.FromSeconds(5)); }
            finally { CompositionTarget.Rendered -= handler; }
        }

        private static async Task DetachAsync(FrameworkElement target)
        {
            var completion = new TaskCompletionSource<object?>();
            RoutedEventHandler? handler = null;
            handler = (_, _) => completion.TrySetResult(null);
            target.Unloaded += handler;
            try
            {
                RuntimeTestHelpers.GetHostPanel().Children.Clear();
                await completion.Task.WaitAsync(TimeSpan.FromSeconds(5));
            }
            finally { target.Unloaded -= handler; }
        }
    }
}
