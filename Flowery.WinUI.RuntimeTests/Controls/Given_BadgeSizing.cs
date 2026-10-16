using System;
using System.Linq;
using System.Threading.Tasks;
using Flowery.Controls;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Foundation;

namespace Flowery.WinUI.RuntimeTests.Controls
{
    [TestClass]
    public class Given_BadgeSizing
    {
        private static readonly DaisySize[] Sizes =
            [DaisySize.ExtraSmall, DaisySize.Small, DaisySize.Medium, DaisySize.Large, DaisySize.ExtraLarge];

        [TestMethod]
        public async Task When_BadgeSizeChanges_AllFiveTiersGrowWithoutClipping()
        {
            double[] fonts = [10, 12, 14, 18, 20];
            double[] minimumHeights = [16, 20, 24, 28, 32];
            var badge = CreateBadge();
            double previousWidth = 0, previousHeight = 0, previousTextWidth = 0, previousTextHeight = 0;
            double previousContentWidth = 0, previousContentHeight = 0;
            try
            {
                RuntimeTestHelpers.AttachToHost(badge);
                await RuntimeTestHelpers.EnsureLoadedAsync(badge).WaitAsync(TimeSpan.FromSeconds(5));
                for (var index = 0; index < Sizes.Length; index++)
                {
                    badge.Size = Sizes[index];
                    await CompleteLayoutAsync(badge);
                    var border = GetBadgeBorder(badge);
                    var presenter = (ContentPresenter)border.Child;
                    var text = Descendants<TextBlock>(presenter).Single();
                    var description = $"Badge {Sizes[index]}";
                    Assert.AreEqual(fonts[index], badge.FontSize, description);
                    Assert.AreEqual(fonts[index], text.FontSize, description + " text font");
                    Assert.AreEqual(minimumHeights[index], border.MinHeight, description + " minimum height");
                    Assert.IsTrue(border.ActualHeight >= minimumHeights[index] - 0.01, description + " actual height");
                    Assert.IsTrue(border.ActualWidth > previousWidth, description + " width must grow");
                    Assert.IsTrue(border.ActualHeight >= previousHeight, description + " height must not shrink");
                    Assert.IsTrue(text.ActualWidth > previousTextWidth, description + " text width must grow");
                    Assert.IsTrue(text.ActualHeight >= previousTextHeight, description + " text height must not shrink");
                    Assert.IsTrue(presenter.ActualWidth >= previousContentWidth, description + " content width must not shrink");
                    Assert.IsTrue(presenter.ActualHeight >= previousContentHeight, description + " content height must not shrink");
                    AssertContained(border, badge, description + " border");
                    AssertContained(presenter, border, description + " content");
                    AssertContained(text, presenter, description + " text");
                    Assert.AreEqual(border.ActualHeight, badge.ActualHeight, 0.01,
                        description + " must not have a second fixed-height border");
                    previousWidth = border.ActualWidth;
                    previousHeight = border.ActualHeight;
                    previousTextWidth = text.ActualWidth;
                    previousTextHeight = text.ActualHeight;
                    previousContentWidth = presenter.ActualWidth;
                    previousContentHeight = presenter.ActualHeight;
                }
            }
            finally
            {
                RuntimeTestHelpers.GetHostPanel().Children.Clear();
            }
        }

        [TestMethod]
        public async Task When_BadgeHasExplicitDimensionsAndPadding_SizeChangesPreserveThem()
        {
            var padding = new Thickness(9, 5, 13, 7);
            var badge = CreateBadge();
            badge.Width = 180;
            badge.Height = 48;
            badge.Padding = padding;
            try
            {
                RuntimeTestHelpers.AttachToHost(badge);
                await RuntimeTestHelpers.EnsureLoadedAsync(badge).WaitAsync(TimeSpan.FromSeconds(5));
                foreach (var size in Sizes)
                {
                    badge.Size = size;
                    await CompleteLayoutAsync(badge);
                    var border = GetBadgeBorder(badge);
                    var presenter = (ContentPresenter)border.Child;
                    var text = Descendants<TextBlock>(presenter).Single();
                    var description = $"Explicit Badge {size}";
                    Assert.AreEqual(180d, badge.Width, description + " Width");
                    Assert.AreEqual(48d, badge.Height, description + " Height");
                    Assert.AreEqual(padding, badge.Padding, description + " Padding");
                    Assert.AreEqual(180d, border.ActualWidth, 0.01, description + " border width");
                    Assert.AreEqual(48d, border.ActualHeight, 0.01, description + " border height");
                    Assert.AreEqual(padding, border.Padding, description + " inner padding");
                    AssertContained(border, badge, description + " border");
                    AssertContained(presenter, border, description + " content");
                    AssertContained(text, presenter, description + " text");
                }
            }
            finally
            {
                RuntimeTestHelpers.GetHostPanel().Children.Clear();
            }
        }

        private static DaisyBadge CreateBadge() => new()
        {
            BadgeContent = "gypQ7",
            Size = DaisySize.ExtraSmall,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            NeumorphicEnabled = false
        };

        private static Border GetBadgeBorder(DaisyBadge badge)
        {
            Assert.IsInstanceOfType<Border>(badge.Content);
            var border = (Border)badge.Content;
            Assert.IsInstanceOfType<ContentPresenter>(border.Child);
            return border;
        }

        private static void AssertContained(FrameworkElement child, FrameworkElement parent, string description)
        {
            Assert.IsTrue(child.ActualWidth > 0 && child.ActualHeight > 0, description + " must be laid out");
            var bounds = child.TransformToVisual(parent)
                .TransformBounds(new Rect(0, 0, child.ActualWidth, child.ActualHeight));
            const double tolerance = 0.01;
            Assert.IsTrue(bounds.X >= -tolerance && bounds.Y >= -tolerance
                && bounds.Right <= parent.ActualWidth + tolerance
                && bounds.Bottom <= parent.ActualHeight + tolerance,
                $"{description} exceeds its parent: child={bounds}, parent={parent.ActualWidth}x{parent.ActualHeight}.");
        }

        private static async Task CompleteLayoutAsync(FrameworkElement element)
        {
            var completion = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
            if (!element.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
                {
                    element.UpdateLayout();
                    completion.TrySetResult(null);
                }))
                throw new InvalidOperationException("The native dispatcher rejected the badge layout verification.");
            await completion.Task.WaitAsync(TimeSpan.FromSeconds(5));
        }

        private static System.Collections.Generic.IEnumerable<T> Descendants<T>(DependencyObject root)
            where T : DependencyObject
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
