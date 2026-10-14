using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Flowery.Controls;
using Flowery.Helpers;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Foundation;

namespace Flowery.WinUI.RuntimeTests.Controls
{
    [TestClass]
    public class Given_NativeSlideLifetime
    {
        [TestMethod]
        public async Task When_CompositionTransitionsStop_OnlyTheirNativeVisualsAreRemoved()
        {
            var oldSlide = new Border { Background = new SolidColorBrush(Colors.Blue) };
            var newSlide = new Border { Background = new SolidColorBrush(Colors.Red) };
            var container = CreateContainer(oldSlide, newSlide);
            RuntimeTestHelpers.AttachToHost(container);
            await RuntimeTestHelpers.EnsureLoadedAsync(container).WaitAsync(TimeSpan.FromSeconds(5));
            await FlushLayoutAsync(container);
            var owner = (ContainerVisual)ElementCompositionPreview.GetElementVisual(container);
            using var foreign = owner.Compositor.CreateSpriteVisual();
            foreign.Size = new Vector2(1, 1);
            using var foreignBrush = owner.Compositor.CreateColorBrush(Colors.Green);
            foreign.Brush = foreignBrush;
            ElementCompositionPreview.SetElementChildVisual(container, foreign);

            try
            {
                foreach (var transition in Enum.GetValues<FlowerySlideTransition>()
                    .Where(FlowerySlideTransitionHelpers.IsCompositionTransition))
                {
                    var previousVisuals = owner.Children.ToArray();
                    var completions = 0;
                    FlowerySlideTransitionHelpers.ApplyTransition(container, oldSlide, newSlide, transition,
                        new FlowerySlideTransitionParams
                        {
                            Duration = TimeSpan.FromSeconds(2),
                            SliceCount = 3,
                            CheckerboardSize = 3,
                            SliceStaggerMs = 30,
                            PixelateSize = 32,
                            DissolveDensity = 0.2
                        },
                        () => completions++);
                    Assert.IsTrue(FlowerySlideTransitionHelpers.IsTransitioning(newSlide), transition.ToString());
                    Assert.IsTrue(owner.Children.Count > previousVisuals.Length,
                        transition + " must attach real native transition visuals before cancellation.");
                    FlowerySlideTransitionHelpers.StopTransition(newSlide);
                    Assert.IsFalse(FlowerySlideTransitionHelpers.IsTransitioning(newSlide), transition.ToString());
                    CollectionAssert.AreEquivalent(previousVisuals, owner.Children.ToArray(),
                        transition + " must remove its native proxies while retaining pre-existing visuals.");
                    Assert.AreSame(foreign, ElementCompositionPreview.GetElementChildVisual(container),
                        transition + " must preserve an externally owned child visual.");
                    Assert.AreEqual(0, completions, transition + " cancellation must not invoke normal completion.");
                    Assert.AreEqual(1d, oldSlide.Opacity);
                    Assert.AreEqual(1d, newSlide.Opacity);
                }
            }
            finally
            {
                FlowerySlideTransitionHelpers.StopTransition(newSlide);
                ElementCompositionPreview.SetElementChildVisual(container, null);
                RuntimeTestHelpers.GetHostPanel().Children.Clear();
            }
        }

        [TestMethod]
        public async Task When_TransitionCompletesOrStops_BackgroundAndClipOwnershipIsPreserved()
        {
            var oldSlide = new Border { Background = new SolidColorBrush(Colors.Blue) };
            var newSlide = new Border { Background = new SolidColorBrush(Colors.Red) };
            var container = CreateContainer(oldSlide, newSlide);
            var originalBackground = new SolidColorBrush(Colors.Green);
            container.Background = originalBackground;
            var originalClip = new RectangleGeometry { Rect = new Rect(0, 0, 120, 80) };
            newSlide.Clip = originalClip;
            try
            {
                RuntimeTestHelpers.AttachToHost(container);
                await RuntimeTestHelpers.EnsureLoadedAsync(container).WaitAsync(TimeSpan.FromSeconds(5));
                await FlushLayoutAsync(container);
                FlowerySlideTransitionHelpers.ApplyTransition(container, oldSlide, newSlide, FlowerySlideTransition.FadeThroughBlack);
                Assert.AreNotSame(originalBackground, container.Background);
                FlowerySlideTransitionHelpers.StopTransition(newSlide);
                Assert.AreSame(originalBackground, container.Background);

                var completed = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
                FlowerySlideTransitionHelpers.ApplyTransition(container, oldSlide, newSlide, FlowerySlideTransition.FadeThroughWhite,
                    new FlowerySlideTransitionParams { Duration = TimeSpan.FromMilliseconds(120) },
                    () => completed.TrySetResult(null));
                await completed.Task.WaitAsync(TimeSpan.FromSeconds(5));
                Assert.AreSame(originalBackground, container.Background);
                Assert.IsFalse(FlowerySlideTransitionHelpers.IsTransitioning(newSlide));

                FlowerySlideTransitionHelpers.ApplyTransition(container, oldSlide, newSlide, FlowerySlideTransition.FadeThroughBlack);
                var replacementBackground = new SolidColorBrush(Colors.Yellow);
                container.Background = replacementBackground;
                FlowerySlideTransitionHelpers.StopTransition(newSlide);
                Assert.AreSame(replacementBackground, container.Background,
                    "Cancellation must preserve a background replaced by the caller during the transition.");

                FlowerySlideTransitionHelpers.ApplyTransition(container, oldSlide, newSlide, FlowerySlideTransition.WipeLeft);
                Assert.AreNotSame(originalClip, newSlide.Clip);
                FlowerySlideTransitionHelpers.StopTransition(newSlide);
                Assert.AreSame(originalClip, newSlide.Clip);

                FlowerySlideTransitionHelpers.ApplyTransition(container, oldSlide, newSlide, FlowerySlideTransition.WipeRight);
                var replacementClip = new RectangleGeometry { Rect = new Rect(4, 4, 100, 60) };
                newSlide.Clip = replacementClip;
                FlowerySlideTransitionHelpers.StopTransition(newSlide);
                Assert.AreSame(replacementClip, newSlide.Clip,
                    "Cancellation must preserve a clip replaced by the caller during the transition.");
            }
            finally
            {
                FlowerySlideTransitionHelpers.StopTransition(newSlide);
                RuntimeTestHelpers.GetHostPanel().Children.Clear();
            }
        }

        [TestMethod]
        public async Task When_CarouselUnloadsDuringTransition_PendingSelectionReloadsAndNavigationContinues()
        {
            Border[] slides =
            [
                new() { Background = new SolidColorBrush(Colors.Red) },
                new() { Background = new SolidColorBrush(Colors.Green) },
                new() { Background = new SolidColorBrush(Colors.Blue) }
            ];
            var content = new StackPanel();
            foreach (var slide in slides)
                content.Children.Add(slide);
            var carousel = new DaisyCarousel
            {
                Width = 180,
                Height = 90,
                Content = content,
                SlideTransition = FlowerySlideTransition.Pixelate,
                TransitionDuration = 0.15,
                WrapAround = true,
                ShowNavigation = false,
                NeumorphicEnabled = false
            };
            try
            {
                RuntimeTestHelpers.AttachToHost(carousel);
                await RuntimeTestHelpers.EnsureLoadedAsync(carousel).WaitAsync(TimeSpan.FromSeconds(5));
                await FlushLayoutAsync(carousel);
                var container = Descendants<Grid>(carousel).Single(grid =>
                    grid.Children.OfType<Border>().Any(wrapper => slides.Any(slide => ReferenceEquals(wrapper.Child, slide))));
                carousel.SelectedIndex = 1;
                var transitioning = container.Children.OfType<Border>().Single(wrapper => ReferenceEquals(wrapper.Child, slides[1]));
                Assert.IsTrue(FlowerySlideTransitionHelpers.IsTransitioning(transitioning));
                carousel.SelectedIndex = 2;

                RuntimeTestHelpers.GetHostPanel().Children.Clear();
                await FlushLayoutAsync(carousel);
                Assert.IsFalse(carousel.IsLoaded);
                Assert.IsFalse(FlowerySlideTransitionHelpers.IsTransitioning(transitioning),
                    "Unloading must stop the active wrapper's native transition.");

                RuntimeTestHelpers.AttachToHost(carousel);
                await RuntimeTestHelpers.EnsureLoadedAsync(carousel).WaitAsync(TimeSpan.FromSeconds(5));
                await FlushLayoutAsync(carousel);
                Assert.AreEqual(2, carousel.SelectedIndex);
                Assert.AreSame(slides[2], container.Children.OfType<Border>().Single().Child,
                    "Reloading must present the selection requested while the cancelled transition was active.");

                var changed = new TaskCompletionSource<FlowerySlideChangedEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
                EventHandler<FlowerySlideChangedEventArgs> handler = (_, args) =>
                {
                    if (args.NewIndex == 0)
                        changed.TrySetResult(args);
                };
                carousel.SlideChanged += handler;
                try
                {
                    carousel.Next();
                    var result = await changed.Task.WaitAsync(TimeSpan.FromSeconds(5));
                    Assert.AreSame(slides[0], result.CurrentElement);
                    Assert.AreSame(slides[0], container.Children.OfType<Border>().Single().Child,
                        "Navigation must complete normally after reload.");
                }
                finally
                {
                    carousel.SlideChanged -= handler;
                }
            }
            finally
            {
                RuntimeTestHelpers.GetHostPanel().Children.Clear();
            }
        }

        private static Grid CreateContainer(UIElement oldSlide, UIElement newSlide)
        {
            var container = new Grid { Width = 120, Height = 80 };
            container.Children.Add(oldSlide);
            container.Children.Add(newSlide);
            return container;
        }

        private static async Task FlushLayoutAsync(FrameworkElement element)
        {
            var completion = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
            if (!element.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
                {
                    element.UpdateLayout();
                    completion.TrySetResult(null);
                }))
                throw new InvalidOperationException("The native dispatcher rejected transition lifetime verification.");
            await completion.Task.WaitAsync(TimeSpan.FromSeconds(5));
        }

        private static System.Collections.Generic.IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
        {
            for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
            {
                var child = VisualTreeHelper.GetChild(root, index);
                if (child is T typed)
                    yield return typed;
                foreach (var descendant in Descendants<T>(child))
                    yield return descendant;
            }
        }
    }
}
