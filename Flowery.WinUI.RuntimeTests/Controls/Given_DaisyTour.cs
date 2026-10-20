using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Flowery.Controls;
using Flowery.Enums;
using Flowery.Localization;
using Flowery.WinUI.Gallery.Examples;
using Flowery.WinUI.Gallery.Localization;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Foundation;

namespace Flowery.WinUI.RuntimeTests.Controls;

[TestClass]
public class Given_DaisyTour
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    [TestMethod]
    public Task When_GalleryTourStarts_DeclaredStepsResolveAndNavigationPreparesRealTargets() =>
        VerifyGalleryTourAsync(900, 600);

    [TestMethod]
    public Task When_GalleryTourUsesCiViewport_DeclaredStepsResolveAndNavigationPreparesRealTargets() =>
        VerifyGalleryTourAsync(752, 480);

    private static async Task VerifyGalleryTourAsync(double maxWidth, double maxHeight)
    {
        var previousCulture = FloweryLocalization.CurrentCultureName;
        var previousResolver = FloweryLocalization.CustomResolver;
        TourExamples? page = null;
        DaisyTour? tour = null;
        try
        {
            FloweryLocalization.SetCulture("en");
            page = new TourExamples { MaxWidth = maxWidth, MaxHeight = maxHeight };
            await LoadAsync(page);
            AssertInViewport(BoundsInRoot(page), page.XamlRoot.Size, "The Gallery page");
            tour = (DaisyTour)page.FindName("Tour");
            var primary = (DaisyButton)page.FindName("PrimaryTarget");
            var nested = (DaisyButton)page.FindName("NestedTarget");
            var tabs = (TabView)page.FindName("DemoTabs");
            var overview = (TabViewItem)page.FindName("OverviewTab");
            var details = (TabViewItem)page.FindName("DetailsTab");
            var mainScroll = (ScrollViewer)page.FindName("MainScrollViewer");
            Assert.HasCount(3, tour.Steps);
            var welcome = tour.Steps[0];
            var scrolling = tour.Steps[1];
            var deferred = tour.Steps[2];
            Assert.AreSame(primary, welcome.Target, "The first typed XAML target must resolve during loading.");
            Assert.AreSame(nested, scrolling.Target, "The second typed XAML target must resolve during loading.");
            Assert.AreEqual(GalleryLocalization.GetString("Gallery_Tour_WelcomeTitle"), welcome.Title);
            Assert.AreEqual(GalleryLocalization.GetString("Gallery_Tour_WelcomeDescription"), welcome.Description);
            Assert.AreEqual(GalleryLocalization.GetString("Gallery_Tour_ScrollTitle"), scrolling.Title);
            Assert.AreEqual(GalleryLocalization.GetString("Gallery_Tour_ScrollDescription"), scrolling.Description);
            Assert.IsFalse(string.IsNullOrWhiteSpace(welcome.Title));
            Assert.IsFalse(string.IsNullOrWhiteSpace(welcome.Description));
            Assert.IsFalse(string.IsNullOrWhiteSpace(scrolling.Title));
            Assert.IsFalse(string.IsNullOrWhiteSpace(scrolling.Description));
            var closed = new List<DaisyTourClosedEventArgs>();
            tour.Closed += (_, args) => { closed.Add(args); tour.Tag = args; };

            await StartGalleryTourAsync(page, tour);
            AssertActive(tour, welcome, 0);
            AssertTargetBounds(tour, primary);
            await tour.NextAsync().WaitAsync(Timeout);
            AssertActive(tour, scrolling, 1);
            Assert.IsTrue(mainScroll.VerticalOffset > 0);
            var innerScroll = Descendants<ScrollViewer>(page).Single(viewer =>
                !ReferenceEquals(viewer, mainScroll) && DaisyTourLayout.IsDescendant(nested, viewer));
            Assert.IsTrue(innerScroll.VerticalOffset > 0);
            AssertFits(nested, innerScroll);
            AssertFits(nested, mainScroll);
            AssertInViewport(BoundsInRoot(nested), page.XamlRoot.Size, "The nested Gallery target");
            await tour.NextAsync().WaitAsync(Timeout);
            AssertActive(tour, deferred, 2);
            Assert.AreSame(details, tabs.SelectedItem);
            Assert.IsInstanceOfType<DaisyButton>(details.Content);
            Assert.AreSame(details.Content, deferred.Target);
            Assert.IsTrue(deferred.Target!.IsLoaded);
            AssertTargetBounds(tour, deferred.Target);
            var createdTarget = deferred.Target;
            await tour.BackAsync().WaitAsync(Timeout);
            AssertActive(tour, scrolling, 1);
            Assert.AreSame(overview, tabs.SelectedItem);
            await tour.NextAsync().WaitAsync(Timeout);
            AssertActive(tour, deferred, 2);
            Assert.AreSame(createdTarget, deferred.Target, "Returning to the deferred tab must reuse its existing control.");
            await tour.NextAsync().WaitAsync(Timeout);
            AssertClosed(tour);
            Assert.HasCount(1, closed);
            Assert.AreEqual(DaisyTourCloseReason.Completed, closed[0].Reason);
            Assert.AreEqual("deferred-tab", closed[0].StepId);
            await StartGalleryTourAsync(page, tour);
            AssertActive(tour, welcome, 0);
            Assert.AreSame(primary, tour.CurrentStep!.Target);
            tour.Skip();
            Assert.HasCount(2, closed);
            Assert.AreEqual(DaisyTourCloseReason.Skipped, closed[1].Reason);
        }
        finally
        {
            try
            {
                tour?.Cancel();
                if (page is not null)
                {
                    RuntimeTestHelpers.GetHostPanel().Children.Remove(page);
                    await ArrangeAsync(page);
                }
            }
            finally
            {
                FloweryLocalization.SetCulture(previousCulture);
                FloweryLocalization.CustomResolver = previousResolver;
            }
        }
    }

    [TestMethod]
    public async Task When_Navigating_CompletesOnceAndReplaysFromTheFirstStep()
    {
        var target = CreateTarget();
        var tour = new DaisyTour();
        var preparations = new List<string>();
        var first = new DaisyTourStep { Id = "first", Target = target, Title = "First" };
        var last = new DaisyTourStep { Id = "last", Target = target, Title = "Last" };
        first.PrepareAsync = (step, _) => { preparations.Add(step.Id); return Task.CompletedTask; };
        last.PrepareAsync = (step, _) => { preparations.Add(step.Id); return Task.CompletedTask; };
        tour.Steps.Add(first);
        tour.Steps.Add(last);
        var closed = new List<DaisyTourClosedEventArgs>();
        tour.Closed += (_, args) => closed.Add(args);
        var root = CreateRoot(tour, target);
        try
        {
            await LoadAsync(root);
            await tour.StartAsync().WaitAsync(Timeout);
            AssertActive(tour, first, 0);
            await tour.NextAsync().WaitAsync(Timeout);
            AssertActive(tour, last, 1);
            await tour.BackAsync().WaitAsync(Timeout);
            AssertActive(tour, first, 0);
            CollectionAssert.AreEqual(new[] { "first", "last", "first" }, preparations);
            await tour.NextAsync().WaitAsync(Timeout);
            await tour.NextAsync().WaitAsync(Timeout);
            AssertClosed(tour);
            Assert.HasCount(1, closed);
            Assert.AreEqual(DaisyTourCloseReason.Completed, closed[0].Reason);
            Assert.AreEqual("last", closed[0].StepId);
            tour.Skip();
            tour.Cancel();
            Assert.HasCount(1, closed);

            await tour.StartAsync().WaitAsync(Timeout);
            AssertActive(tour, first, 0);
            tour.Skip();
            Assert.HasCount(2, closed);
            Assert.AreEqual(DaisyTourCloseReason.Skipped, closed[1].Reason);
            Assert.AreEqual("first", closed[1].StepId);
        }
        finally { await CleanupAsync(root, tour); }
    }

    [TestMethod]
    public async Task When_StartNextAndBackAwaitPreparation_TheCardNeverRendersAtAPlaceholderPosition()
    {
        var firstTarget = CreateTarget();
        var secondTarget = CreateTarget();
        secondTarget.Margin = new Thickness(400, 240, 0, 0);
        var startGate = new PreparationGate();
        var nextGate = new PreparationGate();
        var backGate = new PreparationGate();
        var first = new DaisyTourStep
        {
            Id = "first-staged", Title = "First prepared target", Target = firstTarget, PrepareAsync = startGate.PrepareAsync
        };
        var second = new DaisyTourStep
        {
            Id = "second-staged", Title = "Second prepared target", Target = secondTarget, PrepareAsync = nextGate.PrepareAsync
        };
        var tour = new DaisyTour();
        tour.Steps.Add(first);
        tour.Steps.Add(second);
        var root = CreateRoot(tour, firstTarget, secondTarget);
        try
        {
            await LoadAsync(root);
            var starting = tour.StartAsync();
            await startGate.Entered.Task.WaitAsync(Timeout);
            var surface = GetSurface(tour);
            var card = Descendants<Border>(surface).Single(border => AutomationProperties.GetAutomationId(border) == "DaisyTourCard");
            await RenderFrameAsync(root);
            Assert.IsTrue(tour.IsTransitioning);
            Assert.AreEqual(Visibility.Collapsed, card.Visibility, "Initial preparation must not render a centered instruction card.");
            startGate.Release.TrySetResult();
            await starting.WaitAsync(Timeout);
            await RenderFrameAsync(root);
            AssertActive(tour, first, 0);
            AssertPlacedCard(tour, card, firstTarget);

            var firstBounds = tour.CardBounds;
            var navigating = tour.NextAsync();
            await nextGate.Entered.Task.WaitAsync(Timeout);
            await RenderFrameAsync(root);
            AssertRetainedCard(tour, card, first, 0, firstBounds);
            nextGate.Release.TrySetResult();
            await navigating.WaitAsync(Timeout);
            await RenderFrameAsync(root);
            AssertActive(tour, second, 1);
            AssertPlacedCard(tour, card, secondTarget);

            first.PrepareAsync = backGate.PrepareAsync;
            var secondBounds = tour.CardBounds;
            var returning = tour.BackAsync();
            await backGate.Entered.Task.WaitAsync(Timeout);
            await RenderFrameAsync(root);
            AssertRetainedCard(tour, card, second, 1, secondBounds);
            backGate.Release.TrySetResult();
            await returning.WaitAsync(Timeout);
            await RenderFrameAsync(root);
            AssertActive(tour, first, 0);
            AssertPlacedCard(tour, card, firstTarget);
        }
        finally
        {
            startGate.Release.TrySetResult();
            nextGate.Release.TrySetResult();
            backGate.Release.TrySetResult();
            await CleanupAsync(root, tour);
        }
    }

    [TestMethod]
    public async Task When_InitialPreparationIsCancelled_TheHiddenCardCannotAppearAfterLateCompletion()
    {
        var target = CreateTarget();
        var gate = new PreparationGate();
        var tour = new DaisyTour();
        tour.Steps.Add(new DaisyTourStep { Id = "hidden-cancel", Target = target, PrepareAsync = gate.PrepareAsync });
        var closed = new List<DaisyTourClosedEventArgs>();
        tour.Closed += (_, args) => closed.Add(args);
        var root = CreateRoot(tour, target);
        using var cancellation = new CancellationTokenSource();
        try
        {
            await LoadAsync(root);
            var starting = tour.StartAsync(cancellation.Token);
            await gate.Entered.Task.WaitAsync(Timeout);
            var card = Descendants<Border>(GetSurface(tour)).Single(border => AutomationProperties.GetAutomationId(border) == "DaisyTourCard");
            await RenderFrameAsync(root);
            Assert.AreEqual(Visibility.Collapsed, card.Visibility);
            cancellation.Cancel();
            AssertClosed(tour);
            Assert.IsTrue(gate.Token.IsCancellationRequested);
            gate.Release.TrySetResult();
            await starting.WaitAsync(Timeout);
            await RenderFrameAsync(root);
            AssertClosed(tour);
            Assert.HasCount(1, closed);
            Assert.AreEqual(DaisyTourCloseReason.Cancelled, closed[0].Reason);
            Assert.AreEqual("hidden-cancel", closed[0].StepId);
            Assert.IsFalse(VisualTreeHelper.GetOpenPopupsForXamlRoot(root.XamlRoot).Any(popup =>
                Descendants<Border>(popup.Child).Any(border => ReferenceEquals(border, card))));
        }
        finally { gate.Release.TrySetResult(); await CleanupAsync(root, tour); }
    }

    [TestMethod]
    public async Task When_NextIsRepeatedDuringPreparation_ItDoesNotSkipTheDestination()
    {
        var target = CreateTarget();
        var tour = new DaisyTour();
        var entered = NewCompletion();
        var release = NewCompletion();
        var calls = 0;
        tour.Steps.Add(new DaisyTourStep { Id = "first", Target = target });
        var second = new DaisyTourStep
        {
            Id = "second", Target = target,
            PrepareAsync = async (_, _) => { calls++; entered.TrySetResult(); await release.Task; }
        };
        tour.Steps.Add(second);
        tour.Steps.Add(new DaisyTourStep { Id = "third", Target = target });
        var root = CreateRoot(tour, target);
        try
        {
            await LoadAsync(root);
            await tour.StartAsync().WaitAsync(Timeout);
            var navigation = tour.NextAsync();
            await entered.Task.WaitAsync(Timeout);
            Assert.IsTrue(tour.IsTransitioning);
            var repeatedNavigation = Task.WhenAll(tour.NextAsync(), tour.NextAsync());
            release.TrySetResult();
            await navigation.WaitAsync(Timeout);
            await repeatedNavigation.WaitAsync(Timeout);
            AssertActive(tour, second, 1);
            Assert.AreEqual(1, calls);
        }
        finally { release.TrySetResult(); await CleanupAsync(root, tour); }
    }

    [TestMethod]
    public async Task When_SkippedDuringPreparation_LateCompletionCannotReopenTheTour()
    {
        var target = CreateTarget();
        var entered = NewCompletion();
        var release = NewCompletion();
        var token = CancellationToken.None;
        var tour = new DaisyTour();
        tour.Steps.Add(new DaisyTourStep
        {
            Id = "pending", Target = target,
            PrepareAsync = async (_, cancellationToken) =>
            {
                token = cancellationToken;
                entered.TrySetResult();
                await release.Task;
            }
        });
        var closed = new List<DaisyTourClosedEventArgs>();
        tour.Closed += (_, args) => closed.Add(args);
        var root = CreateRoot(tour, target);
        try
        {
            await LoadAsync(root);
            var starting = tour.StartAsync();
            await entered.Task.WaitAsync(Timeout);
            await RenderFrameAsync(root);
            var card = Descendants<Border>(GetSurface(tour)).Single(border => AutomationProperties.GetAutomationId(border) == "DaisyTourCard");
            Assert.AreEqual(Visibility.Collapsed, card.Visibility);
            tour.Skip();
            Assert.IsTrue(token.IsCancellationRequested);
            AssertClosed(tour);
            release.TrySetResult();
            await starting.WaitAsync(Timeout);
            await ArrangeAsync(root);
            AssertClosed(tour);
            Assert.HasCount(1, closed);
            Assert.AreEqual(DaisyTourCloseReason.Skipped, closed[0].Reason);
            Assert.AreEqual("pending", closed[0].StepId);
        }
        finally { release.TrySetResult(); await CleanupAsync(root, tour); }
    }

    [TestMethod]
    public async Task When_ReplayedDuringPreparation_OldCompletionCannotReplaceTheNewSession()
    {
        var target = CreateTarget();
        var entered = NewCompletion();
        var release = NewCompletion();
        var calls = 0;
        var tour = new DaisyTour();
        var step = new DaisyTourStep
        {
            Id = "replay", Target = target,
            PrepareAsync = async (_, _) =>
            {
                if (++calls == 1) { entered.TrySetResult(); await release.Task; }
            }
        };
        tour.Steps.Add(step);
        var closed = new List<DaisyTourClosedEventArgs>();
        tour.Closed += (_, args) => closed.Add(args);
        var root = CreateRoot(tour, target);
        try
        {
            await LoadAsync(root);
            var oldStarting = tour.StartAsync();
            await entered.Task.WaitAsync(Timeout);
            await tour.StartAsync().WaitAsync(Timeout);
            AssertActive(tour, step, 0);
            release.TrySetResult();
            await oldStarting.WaitAsync(Timeout);
            AssertActive(tour, step, 0);
            Assert.AreEqual(2, calls);
            Assert.HasCount(1, closed);
            Assert.AreEqual(DaisyTourCloseReason.Cancelled, closed[0].Reason);
        }
        finally { release.TrySetResult(); await CleanupAsync(root, tour); }
    }

    [TestMethod]
    public async Task When_CallerCancelsOrHostUnloads_TheActiveSessionClosesOnce()
    {
        var target = CreateTarget();
        var tour = new DaisyTour();
        tour.Steps.Add(new DaisyTourStep { Id = "cancel", Target = target });
        var closed = new List<DaisyTourClosedEventArgs>();
        tour.Closed += (_, args) => closed.Add(args);
        var root = CreateRoot(tour, target);
        using var cancellation = new CancellationTokenSource();
        try
        {
            await LoadAsync(root);
            await tour.StartAsync(cancellation.Token).WaitAsync(Timeout);
            cancellation.Cancel();
            await ArrangeAsync(root);
            AssertClosed(tour);
            Assert.HasCount(1, closed);
            Assert.AreEqual(DaisyTourCloseReason.Cancelled, closed[0].Reason);

            await tour.StartAsync().WaitAsync(Timeout);
            root.Children.Remove(tour);
            await ArrangeAsync(root);
            AssertClosed(tour);
            Assert.HasCount(2, closed);
            Assert.AreEqual(DaisyTourCloseReason.Cancelled, closed[1].Reason);
        }
        finally { await CleanupAsync(root, tour); }
    }

    [TestMethod]
    public async Task When_StepsAreEmptyOrIdsAreInvalid_ValidationDoesNotOpenAnOverlay()
    {
        var target = CreateTarget();
        var tour = new DaisyTour();
        var root = CreateRoot(tour, target);
        try
        {
            await LoadAsync(root);
            await tour.StartAsync().WaitAsync(Timeout);
            AssertClosed(tour);
            tour.Steps.Add(new DaisyTourStep { Id = "duplicate", Target = target });
            tour.Steps.Add(new DaisyTourStep { Id = "duplicate", Target = target });
            await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => tour.StartAsync());
            AssertClosed(tour);
            tour.Steps.Clear();
            tour.Steps.Add(new DaisyTourStep { Id = string.Empty, Target = target });
            await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => tour.StartAsync());
            AssertClosed(tour);
        }
        finally { await CleanupAsync(root, tour); }
    }

    [TestMethod]
    public async Task When_TargetIsMissingHiddenOrPreparationFails_TheFailureIdentifiesTheStep()
    {
        var visible = CreateTarget();
        var detached = CreateTarget();
        var hidden = CreateTarget();
        hidden.Visibility = Visibility.Collapsed;
        var tour = new DaisyTour();
        var root = CreateRoot(tour, visible, hidden);
        var closed = new List<DaisyTourClosedEventArgs>();
        tour.Closed += (_, args) => closed.Add(args);
        try
        {
            await LoadAsync(root);
            foreach (var target in new FrameworkElement?[] { null, detached, hidden })
            {
                tour.Steps.Clear();
                tour.Steps.Add(new DaisyTourStep { Id = "unavailable", Target = target });
                await tour.StartAsync().WaitAsync(Timeout);
                AssertClosed(tour);
                Assert.AreEqual(DaisyTourCloseReason.TargetUnavailable, closed[^1].Reason);
                Assert.AreEqual("unavailable", closed[^1].StepId);
            }
            tour.Steps.Clear();
            tour.Steps.Add(new DaisyTourStep
            {
                Id = "failed-prepare", Target = visible,
                PrepareAsync = (_, _) => throw new InvalidOperationException("Preparation failed")
            });
            await tour.StartAsync().WaitAsync(Timeout);
            AssertClosed(tour);
            Assert.AreEqual(DaisyTourCloseReason.TargetUnavailable, closed[^1].Reason);
            Assert.AreEqual("failed-prepare", closed[^1].StepId);
            Assert.HasCount(4, closed);
        }
        finally { await CleanupAsync(root, tour); }
    }

    [TestMethod]
    public async Task When_PreparationCreatesTheTarget_ItResolvesAfterNativeLayout()
    {
        var target = CreateTarget();
        var tour = new DaisyTour();
        var root = CreateRoot(tour);
        var step = new DaisyTourStep { Id = "deferred" };
        step.PrepareAsync = (_, _) =>
        {
            root.Children.Insert(0, target);
            step.Target = target;
            return Task.CompletedTask;
        };
        tour.Steps.Add(step);
        try
        {
            await LoadAsync(root);
            await tour.StartAsync().WaitAsync(Timeout);
            AssertActive(tour, step, 0);
            AssertTargetBounds(tour, target);
        }
        finally { await CleanupAsync(root, tour); }
    }

    [TestMethod]
    public async Task When_TargetBelongsToAnotherWindow_ItIsReportedUnavailable()
    {
        var target = CreateTarget();
        var foreignRoot = new Grid { Children = { target } };
        var foreignWindow = new Window { Content = foreignRoot };
        var tour = new DaisyTour();
        tour.Steps.Add(new DaisyTourStep { Id = "foreign", Target = target });
        var root = CreateRoot(tour);
        DaisyTourClosedEventArgs? closed = null;
        tour.Closed += (_, args) => closed = args;
        try
        {
            foreignWindow.Activate();
            await RuntimeTestHelpers.EnsureLoadedAsync(foreignRoot).WaitAsync(Timeout);
            await LoadAsync(root);
            Assert.AreNotSame(root.XamlRoot, target.XamlRoot);
            await tour.StartAsync().WaitAsync(Timeout);
            AssertClosed(tour);
            Assert.IsNotNull(closed);
            Assert.AreEqual(DaisyTourCloseReason.TargetUnavailable, closed.Reason);
            Assert.AreEqual("foreign", closed.StepId);
            Assert.AreSame(target, closed.Target);
        }
        finally
        {
            foreignWindow.Close();
            RuntimeTestContext.Window?.Activate();
            await CleanupAsync(root, tour);
        }
    }

    [TestMethod]
    public async Task When_TargetIsInsideNestedScrollers_ItIsVisibleBeforeTheStepOpens()
    {
        var target = CreateTarget();
        target.Margin = new Thickness(24, 700, 0, 0);
        var innerContent = new Grid { Height = 1000, Children = { target } };
        var inner = new ScrollViewer { Height = 200, Content = innerContent, VerticalScrollBarVisibility = ScrollBarVisibility.Hidden };
        inner.Margin = new Thickness(0, 550, 0, 0);
        inner.VerticalAlignment = VerticalAlignment.Top;
        var outerContent = new Grid { Height = 1000, Children = { inner } };
        var outer = new ScrollViewer { Content = outerContent, VerticalScrollBarVisibility = ScrollBarVisibility.Hidden };
        var tour = new DaisyTour();
        var step = new DaisyTourStep { Id = "nested", Target = target };
        tour.Steps.Add(step);
        var root = CreateRoot(tour, outer);
        try
        {
            await LoadAsync(root);
            await tour.StartAsync().WaitAsync(Timeout);
            AssertActive(tour, step, 0);
            Assert.IsTrue(inner.VerticalOffset > 0, "The inner scroller must reveal the target.");
            Assert.IsTrue(outer.VerticalOffset > 0, "The outer scroller must reveal the inner viewport.");
            AssertFits(target, inner);
            AssertFits(target, outer);
            AssertTargetBounds(tour, target);
        }
        finally { await CleanupAsync(root, tour); }
    }

    [TestMethod]
    public async Task When_TargetReflows_TheHighlightTracksItsCurrentBounds()
    {
        var target = CreateTarget();
        var tour = new DaisyTour();
        var step = new DaisyTourStep { Id = "reflow", Target = target };
        tour.Steps.Add(step);
        var root = CreateRoot(tour, target);
        try
        {
            await LoadAsync(root);
            await tour.StartAsync().WaitAsync(Timeout);
            var original = tour.TargetBounds;
            var updated = NewCompletion();
            EventHandler handler = (_, _) =>
            {
                if (tour.TargetBounds != original) updated.TrySetResult();
            };
            tour.GeometryUpdated += handler;
            try
            {
                target.Margin = new Thickness(220, 190, 0, 0);
                target.Width = 130;
                target.Height = 70;
                root.UpdateLayout();
                await updated.Task.WaitAsync(Timeout);
            }
            finally { tour.GeometryUpdated -= handler; }
            AssertActive(tour, step, 0);
            Assert.AreNotEqual(original, tour.TargetBounds);
            AssertTargetBounds(tour, target);
            Assert.IsTrue(tour.CardBounds.Width > 0 && tour.CardBounds.Height > 0);
            Assert.IsTrue(tour.CardBounds.X >= 0 && tour.CardBounds.Y >= 0);
            Assert.IsTrue(tour.CardBounds.Right <= root.XamlRoot.Size.Width + 1);
            Assert.IsTrue(tour.CardBounds.Bottom <= root.XamlRoot.Size.Height + 1);
        }
        finally { await CleanupAsync(root, tour); }
    }

    [TestMethod]
    public async Task When_ArrowAnnotationRenders_NativePixelsContainItsStrokeAndFilledHead()
    {
        foreach (var preset in new[]
        {
            DaisyTourAnnotationPreset.ThinArrow, DaisyTourAnnotationPreset.ThickArrow,
            DaisyTourAnnotationPreset.BlockArrow, DaisyTourAnnotationPreset.CurvedArrow,
            DaisyTourAnnotationPreset.TwoEndedArrow
        })
            await VerifyAnnotationPixelsAsync(DaisyTourAnnotationKind.Arrow, preset);
    }

    [TestMethod]
    public async Task When_LineAnnotationRenders_NativePixelsContainItsStroke()
    {
        foreach (var style in new[]
        {
            DaisyTourStrokeStyle.Solid, DaisyTourStrokeStyle.Dotted, DaisyTourStrokeStyle.Dashed,
            DaisyTourStrokeStyle.DashDot, DaisyTourStrokeStyle.Custom
        })
            await VerifyAnnotationPixelsAsync(DaisyTourAnnotationKind.Line, strokeStyle: style);
    }

    [TestMethod]
    public Task When_CloudAnnotationRenders_NativePixelsContainItsStrokeAndFill() =>
        VerifyAnnotationPixelsAsync(DaisyTourAnnotationKind.Cloud);

    [TestMethod]
    public Task When_SpeechBubbleAnnotationRenders_NativePixelsContainItsStrokeAndFill() =>
        VerifyAnnotationPixelsAsync(DaisyTourAnnotationKind.SpeechBubble);

    [TestMethod]
    public Task When_CustomAnnotationRenders_NativePixelsContainItsStrokeAndFill() =>
        VerifyAnnotationPixelsAsync(DaisyTourAnnotationKind.Custom);

    [TestMethod]
    public void When_ConnectorBoundsChange_RetainedPathsFollowBothAnchorsAndStyleOverrides()
    {
        var presenter = new DaisyTourAnnotationPresenter();
        var pattern = new DaisyTourDashCollection { 2, 3, 1, 3 };
        var annotation = new DaisyTourAnnotation
        {
            Preset = DaisyTourAnnotationPreset.TwoEndedArrow,
            SourceAnchor = DaisyTourAnchor.Right,
            TargetAnchor = DaisyTourAnchor.Left,
            StrokeThickness = 7,
            DashArray = pattern,
            DashOffset = 4,
            Opacity = 0.6
        };
        var hostDefaults = new DaisyTourAnnotationStyle { StrokeThickness = 1 };
        var stepDefaults = new DaisyTourAnnotationStyle { StrokeThickness = 3 };
        var stroke = new SolidColorBrush(Colors.Blue);
        var fill = new SolidColorBrush(Colors.Orange);
        presenter.Update(annotation, new Rect(10, 20, 100, 40), new Rect(300, 100, 80, 60),
            stroke, fill, stroke, hostDefaults, stepDefaults);
        var path = (Path)presenter.Children[1];
        AssertEndpoints(path, new Point(110, 40), new Point(300, 130));
        Assert.AreSame(stroke, path.Stroke);
        Assert.AreEqual(7d, path.StrokeThickness);
        Assert.AreEqual(4d, path.StrokeDashOffset);
        Assert.AreEqual(0.6, presenter.Opacity, 0.000001, "WinUI stores opacity with native single-precision rounding.");
        CollectionAssert.AreEqual(new[] { 2d, 3d, 1d, 3d }, path.StrokeDashArray.ToArray());
        Assert.IsNotNull(((Path)presenter.Children[2]).Data);
        Assert.IsNotNull(((Path)presenter.Children[3]).Data);

        annotation.SourceAnchor = DaisyTourAnchor.Auto;
        annotation.TargetAnchor = DaisyTourAnchor.Auto;
        annotation.StrokeThickness = double.NaN;
        pattern[0] = 5;
        presenter.Update(annotation, new Rect(300, 20, 100, 40), new Rect(10, 20, 80, 40),
            stroke, fill, stroke, hostDefaults, stepDefaults);
        Assert.AreSame(path, presenter.Children[1], "Geometry changes must retain the existing path controls.");
        AssertEndpoints(path, new Point(300, 40), new Point(90, 40));
        Assert.AreEqual(3d, path.StrokeThickness, "Step defaults override host defaults when the annotation inherits.");
        Assert.AreEqual(5d, path.StrokeDashArray[0]);
    }

    [TestMethod]
    public void When_AnnotationPresetsOrContentChange_AllShapesRetainIndependentTextAndTailBehavior()
    {
        var presenter = new DaisyTourAnnotationPresenter();
        var annotation = new DaisyTourAnnotation();
        var brush = new SolidColorBrush(Colors.Blue);
        var source = new Rect(150, 150, 100, 50);
        var target = new Rect(400, 150, 60, 40);
        foreach (var preset in Enum.GetValues<DaisyTourAnnotationPreset>())
        {
            annotation.Preset = preset;
            presenter.Update(annotation, source, target, brush, brush, brush);
            Assert.IsNotNull(((Path)presenter.Children[1]).Data, preset.ToString());
        }

        foreach (var kind in new[] { DaisyTourAnnotationKind.Cloud, DaisyTourAnnotationKind.SpeechBubble })
        {
            annotation.Kind = kind;
            annotation.Tail = DaisyTourAnnotationTail.None;
            annotation.Text = string.Empty;
            presenter.Update(annotation, source, target, brush, brush, brush);
            Assert.AreEqual(Visibility.Collapsed, presenter.Children[4].Visibility);
            Assert.IsNull(((Path)presenter.Children[0]).Data);

            annotation.Tail = DaisyTourAnnotationTail.Thought;
            presenter.Update(annotation, source, target, brush, brush, brush);
            Assert.AreEqual(Visibility.Collapsed, presenter.Children[4].Visibility);
            Assert.IsNotNull(((Path)presenter.Children[0]).Data);

            annotation.Text = "A localized explanation can wrap across several lines.";
            annotation.Tail = DaisyTourAnnotationTail.None;
            presenter.Update(annotation, source, target, brush, brush, brush);
            Assert.AreEqual(Visibility.Visible, presenter.Children[4].Visibility);
            Assert.IsNull(((Path)presenter.Children[0]).Data);
        }
        var geometry = new EllipseGeometry { Center = new Point(10, 10), RadiusX = 10, RadiusY = 10 };
        annotation.Kind = DaisyTourAnnotationKind.Custom;
        annotation.Geometry = geometry;
        presenter.Update(annotation, source, target, brush, brush, brush);
        Assert.AreSame(geometry, ((Path)presenter.Children[1]).Data);
    }

    [TestMethod]
    public async Task When_OpenTourAppearanceChanges_TextButtonsAndProgressRemainCurrent()
    {
        var previousCulture = FloweryLocalization.CurrentCultureName;
        var previousTheme = DaisyThemeManager.CurrentThemeName;
        var previousSize = FlowerySizeManager.CurrentSize;
        var target = CreateTarget();
        var tour = new DaisyTour();
        var step = new DaisyTourStep { Id = "appearance", Target = target, Title = "Original title" };
        tour.Steps.Add(step);
        tour.Steps.Add(new DaisyTourStep { Id = "last", Target = target });
        var root = CreateRoot(tour, target);
        try
        {
            FloweryLocalization.SetCulture("en");
            Assert.IsTrue(DaisyThemeManager.ApplyTheme("Light"));
            await LoadAsync(root);
            await tour.StartAsync().WaitAsync(Timeout);
            var surface = GetSurface(tour);
            var next = Descendants<DaisyButton>(surface).Single(button => AutomationProperties.GetAutomationId(button) == "DaisyTourNext");
            var card = Descendants<Border>(surface).Single(border => AutomationProperties.GetAutomationId(border) == "DaisyTourCard");
            var lightColor = ((SolidColorBrush)card.Background).Color;
            foreach (var size in Enum.GetValues<DaisySize>())
            {
                FlowerySizeManager.ApplySize(size);
                await ArrangeAsync(root);
                Assert.AreEqual(size, tour.Size);
                Assert.AreEqual(size, next.Size);
                Assert.AreEqual(size, tour.Progress.Size);
                AssertActive(tour, step, 0);
                Assert.IsTrue(tour.CardBounds.Bottom <= root.XamlRoot.Size.Height + 1);
            }
            Assert.IsTrue(DaisyThemeManager.ApplyTheme("Dark"));
            await ArrangeAsync(root);
            Assert.AreNotEqual(lightColor, ((SolidColorBrush)card.Background).Color);
            step.Title = "Updated application title";
            step.Description = string.Join(" ", Enumerable.Repeat("Long localized content wraps without hiding navigation.", 20));
            FloweryLocalization.SetCulture("de");
            await ArrangeAsync(root);
            Assert.AreEqual(FloweryLocalization.GetStringInternal("Tour_Next"), next.Content);
            Assert.IsTrue(Descendants<TextBlock>(surface).Any(text => text.Text == step.Title));
            FloweryLocalization.SetCulture("ar");
            await ArrangeAsync(root);
            Assert.AreEqual(FlowDirection.RightToLeft, card.FlowDirection);
            Assert.AreEqual(FloweryLocalization.GetStringInternal("Tour_Next"), next.Content);
            AssertActive(tour, step, 0);
            Assert.IsTrue(next.IsEnabled);
            Assert.IsTrue(next.ActualWidth > 0 && next.ActualHeight > 0);
        }
        finally
        {
            try { await CleanupAsync(root, tour); }
            finally
            {
                FloweryLocalization.SetCulture(previousCulture);
                FlowerySizeManager.ApplySize(previousSize);
                if (previousTheme is not null) DaisyThemeManager.ApplyTheme(previousTheme);
            }
        }
    }

    [TestMethod]
    public async Task When_ConnectorTargetsReflow_BothEndpointsTrackAndUnavailableTargetIsIdentified()
    {
        var source = CreateTarget();
        var target = CreateTarget();
        source.Margin = new Thickness(20, 100, 0, 0);
        target.Margin = new Thickness(400, 100, 0, 0);
        var annotation = new DaisyTourAnnotation
        {
            Id = "relationship", Source = source, Target = target,
            Preset = DaisyTourAnnotationPreset.TwoEndedArrow
        };
        var step = new DaisyTourStep { Id = "connection", Target = source };
        step.Annotations.Add(annotation);
        var tour = new DaisyTour();
        tour.Steps.Add(step);
        var root = CreateRoot(tour, source, target);
        DaisyTourClosedEventArgs? closed = null;
        var closure = new TaskCompletionSource<DaisyTourClosedEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
        tour.Closed += (_, args) => { closed = args; closure.TrySetResult(args); };
        try
        {
            await LoadAsync(root);
            await tour.StartAsync().WaitAsync(Timeout);
            var presenter = Descendants<DaisyTourAnnotationPresenter>(GetSurface(tour)).Single();
            var path = (Path)presenter.Children[1];
            var sourceBounds = BoundsInRoot(source);
            var targetBounds = BoundsInRoot(target);
            AssertEndpoints(path, new Point(sourceBounds.Right, sourceBounds.Y + sourceBounds.Height / 2),
                new Point(targetBounds.Left, targetBounds.Y + targetBounds.Height / 2));
            source.Margin = new Thickness(400, 180, 0, 0);
            target.Margin = new Thickness(20, 180, 0, 0);
            await ArrangeAsync(root);
            AssertActive(tour, step, 0);
            Assert.AreSame(path, presenter.Children[1]);
            sourceBounds = BoundsInRoot(source);
            targetBounds = BoundsInRoot(target);
            AssertEndpoints(path, new Point(sourceBounds.Left, sourceBounds.Y + sourceBounds.Height / 2),
                new Point(targetBounds.Right, targetBounds.Y + targetBounds.Height / 2));
            target.Visibility = Visibility.Collapsed;
            await closure.Task.WaitAsync(Timeout);
            AssertClosed(tour);
            Assert.IsNotNull(closed);
            Assert.AreEqual(DaisyTourCloseReason.TargetUnavailable, closed.Reason);
            Assert.AreEqual("connection", closed.StepId);
            Assert.AreEqual("relationship", closed.AnnotationId);
            Assert.AreSame(target, closed.Target);
        }
        finally { await CleanupAsync(root, tour); }
    }

    [TestMethod]
    public async Task When_SurfaceAnnotationsHaveNoSource_TheirBodiesRemainOutsideTheCard()
    {
        var target = CreateTarget();
        target.Margin = new Thickness(240, 180, 0, 0);
        var tour = new DaisyTour();
        var annotation = new DaisyTourAnnotation
        {
            Preset = DaisyTourAnnotationPreset.Cloud,
            Text = string.Join(" ", Enumerable.Repeat("Long localized annotation text must wrap within its visible shape.", 80))
        };
        var step = new DaisyTourStep { Id = "surface", Target = target };
        step.Annotations.Add(annotation);
        tour.Steps.Add(step);
        var root = CreateRoot(tour, target);
        try
        {
            await LoadAsync(root);
            await tour.StartAsync().WaitAsync(Timeout);
            foreach (var size in Enum.GetValues<DaisySize>())
            {
                foreach (var preset in new[] { DaisyTourAnnotationPreset.Cloud, DaisyTourAnnotationPreset.SpeechBubble })
                {
                    var published = NewCompletion();
                    EventHandler handler = (_, _) => published.TrySetResult();
                    tour.GeometryUpdated += handler;
                    try
                    {
                        tour.Size = size;
                        annotation.Preset = preset;
                        root.UpdateLayout();
                        await published.Task.WaitAsync(Timeout);
                    }
                    finally { tour.GeometryUpdated -= handler; }
                    AssertActive(tour, step, 0);
                    var presenter = Descendants<DaisyTourAnnotationPresenter>(GetSurface(tour)).Single();
                    var bounds = ((Path)presenter.Children[1]).Data.Bounds;
                    var obscured = bounds;
                    obscured.Intersect(tour.CardBounds);
                    Assert.IsTrue(obscured.IsEmpty || obscured.Width * obscured.Height < bounds.Width * bounds.Height,
                        $"{preset}/{size} must not be completely covered by the card.");
                    AssertInViewport(bounds, root.XamlRoot.Size, $"{preset}/{size}");
                    var content = (Border)presenter.Children[4];
                    Assert.AreEqual(Visibility.Visible, content.Visibility);
                    Assert.IsTrue(content.Width <= content.MaxWidth && content.Height <= content.MaxHeight);
                }
            }
        }
        finally { await CleanupAsync(root, tour); }
    }

    [TestMethod]
    public async Task When_LongSurfaceAnnotationsUseNarrowViewports_TheirMeasuredGeometryFitsAtEverySize()
    {
        var presenter = new DaisyTourAnnotationPresenter();
        var root = new Grid { Children = { presenter } };
        var annotation = new DaisyTourAnnotation
        {
            Text = string.Join(" ", Enumerable.Repeat("Long localized explanation wraps in a narrow annotation.", 80))
        };
        var brush = new SolidColorBrush(Colors.Blue);
        try
        {
            await LoadAsync(root);
            var cases = from viewport in new[] { new Size(600, 400), new Size(260, 220) }
                        from size in Enum.GetValues<DaisySize>()
                        from preset in new[] { DaisyTourAnnotationPreset.Cloud, DaisyTourAnnotationPreset.SpeechBubble }
                        select (viewport, size, preset);
            foreach (var (viewport, size, preset) in cases)
            {
                root.Width = presenter.Width = viewport.Width;
                root.Height = presenter.Height = viewport.Height;
                var scale = size switch
                {
                    DaisySize.ExtraSmall => 0.8, DaisySize.Small => 0.9, DaisySize.Large => 1.15,
                    DaisySize.ExtraLarge => 1.3, _ => 1
                };
                annotation.Preset = preset;
                var measured = presenter.MeasureSurface(annotation, brush, scale: scale);
                var target = new Rect(viewport.Width / 2 - 20, 60, 40, 30);
                var card = new Rect(12, viewport.Height - 100, viewport.Width - 24, 88);
                var source = DaisyTourLayout.PlaceAnnotation(target, card, viewport, measured);
                presenter.Update(annotation, source, target, brush, brush, brush, scale: scale);
                await ArrangeAsync(root);
                var bounds = ((Path)presenter.Children[1]).Data.Bounds;
                AssertInViewport(bounds, viewport, $"{preset}/{size}/{viewport}");
                var content = (Border)presenter.Children[4];
                Assert.IsTrue(content.Width <= viewport.Width - 24 + 0.01);
                Assert.IsTrue(content.Height <= viewport.Height - 24 + 0.01);
                Assert.AreEqual(measured.Width, content.Width, 0.01);
                Assert.AreEqual(measured.Height, content.Height, 0.01);
            }
        }
        finally
        {
            RuntimeTestHelpers.GetHostPanel().Children.Remove(root);
            await ArrangeAsync(root);
        }
    }

    [TestMethod]
    public async Task When_EightStepsUseExtraLargeSize_TheCurrentCompactIndicatorRemainsVisible()
    {
        var target = CreateTarget();
        var tour = new DaisyTour { Size = DaisySize.ExtraLarge };
        for (var index = 0; index < 8; index++)
            tour.Steps.Add(new DaisyTourStep { Id = "step-" + index, Target = target });
        var root = CreateRoot(tour, target);
        try
        {
            await LoadAsync(root);
            tour.Size = DaisySize.ExtraLarge;
            await tour.StartAsync().WaitAsync(Timeout);
            for (var index = 0; index < 8; index++)
            {
                if (index > 0) await tour.NextAsync().WaitAsync(Timeout);
                await ArrangeAsync(root);
                AssertActive(tour, tour.Steps[index], index);
                var viewport = Descendants<ScrollViewer>(GetSurface(tour)).Single(viewer => ReferenceEquals(viewer.Content, tour.Progress));
                var items = Descendants<DaisyStepItem>(tour.Progress).ToArray();
                var contentPresenter = Descendants<ContentPresenter>(tour.Progress).FirstOrDefault();
                var modelPanel = tour.Progress.Content as Panel;
                var presentedPanel = contentPresenter?.Content as Panel;
                var firstItemParent = modelPanel?.Children.FirstOrDefault() is { } first
                    ? VisualTreeHelper.GetParent(first)?.GetType().Name : "none";
                Assert.HasCount(8, items,
                    $"Step={index}, ModelCount={tour.Progress.ItemCount}, ModelChildren={modelPanel?.Children.Count}, PresentedChildren={presentedPanel?.Children.Count}, SameContent={ReferenceEquals(contentPresenter?.Content, tour.Progress.Content)}, PresenterContent={contentPresenter?.Content?.GetType().Name}, PresenterVisualChildren={(contentPresenter is null ? -1 : VisualTreeHelper.GetChildrenCount(contentPresenter))}, FirstItemParent={firstItemParent}, ProgressSize={tour.Progress.ActualWidth}x{tour.Progress.ActualHeight}, ViewportSize={viewport.ActualWidth}x{viewport.ActualHeight}");
                Assert.IsTrue(items.All(item => item.IsCompact));
                Assert.AreEqual(index, tour.Progress.SelectedIndex);
                Assert.AreEqual(Visibility.Visible, viewport.Visibility);
                var currentItem = items[index];
                var indicators = Descendants<Microsoft.UI.Xaml.Shapes.Ellipse>(currentItem).ToArray();
                if (indicators.Length != 1)
                {
                    var itemPresenter = Descendants<ContentPresenter>(currentItem).FirstOrDefault();
                    var contentGrid = currentItem.Content as Grid;
                    var visualChildren = Enumerable.Range(0, VisualTreeHelper.GetChildrenCount(currentItem))
                        .Select(child => VisualTreeHelper.GetChild(currentItem, child).GetType().Name);
                    Assert.Fail($"Expected one indicator; found {indicators.Length}. Step={index}, ItemIndex={currentItem.Index}, Loaded={currentItem.IsLoaded}, Size={currentItem.ActualWidth}x{currentItem.ActualHeight}, TemplateNull={currentItem.Template is null}, ContentGridChildren={contentGrid?.Children.Count}, VisualChildren={string.Join(",", visualChildren)}, PresenterContent={itemPresenter?.Content?.GetType().Name}, SameContent={ReferenceEquals(itemPresenter?.Content, currentItem.Content)}, PresenterVisualChildren={(itemPresenter is null ? -1 : VisualTreeHelper.GetChildrenCount(itemPresenter))}");
                }
                var indicator = indicators.Single();
                AssertFits(indicator, viewport);
            }
        }
        finally { await CleanupAsync(root, tour); }
    }

    [TestMethod]
    public void When_AnnotationScaleChanges_FallbackDimensionsScaleAndExplicitStrokeRemainsFixed()
    {
        var presenter = new DaisyTourAnnotationPresenter();
        var annotation = new DaisyTourAnnotation { Preset = DaisyTourAnnotationPreset.ThinArrow };
        var brush = new SolidColorBrush(Colors.Blue);
        var source = new Rect(10, 10, 40, 40);
        var target = new Rect(400, 10, 40, 40);
        presenter.Update(annotation, source, target, brush, brush, brush, scale: 0.8);
        var body = (Path)presenter.Children[1];
        var head = (Path)presenter.Children[3];
        var smallStroke = body.StrokeThickness;
        var smallHead = head.Data.Bounds;
        presenter.Update(annotation, source, target, brush, brush, brush, scale: 1.3);
        Assert.AreEqual(smallStroke * 1.3 / 0.8, body.StrokeThickness, 0.0001);
        Assert.AreEqual(smallHead.Width * 1.3 / 0.8, head.Data.Bounds.Width, 0.0001);
        Assert.AreEqual(smallHead.Height * 1.3 / 0.8, head.Data.Bounds.Height, 0.0001);
        annotation.StrokeThickness = 7;
        presenter.Update(annotation, source, target, brush, brush, brush, scale: 0.8);
        Assert.AreEqual(7d, body.StrokeThickness);
        presenter.Update(annotation, source, target, brush, brush, brush, scale: 1.3);
        Assert.AreEqual(7d, body.StrokeThickness);
    }

    [TestMethod]
    public async Task When_TargetHighlightsOverlap_TheBackdropContainsOneCombinedCutout()
    {
        var first = CreateTarget();
        var second = CreateTarget();
        second.Margin = new Thickness(90, 24, 0, 0);
        var annotation = new DaisyTourAnnotation { Id = "overlap", Source = first, Target = second };
        var step = new DaisyTourStep { Id = "combined", Target = first };
        step.Annotations.Add(annotation);
        var tour = new DaisyTour();
        tour.Steps.Add(step);
        var root = CreateRoot(tour, first, second);
        try
        {
            await LoadAsync(root);
            await tour.StartAsync().WaitAsync(Timeout);
            var surface = (Canvas)GetSurface(tour);
            var backdrop = (Path)surface.Children[0];
            var outline = (Path)surface.Children[1];
            var cutouts = (GeometryGroup)backdrop.Data;
            Assert.AreEqual(FillRule.EvenOdd, cutouts.FillRule);
            Assert.HasCount(2, cutouts.Children, "Backdrop plus one combined hole prevents overlapping cutouts from restoring dimming.");
            Assert.HasCount(1, ((GeometryGroup)outline.Data).Children);
            var combined = cutouts.Children[1].Bounds;
            Assert.IsTrue(combined.Left <= BoundsInRoot(first).Left && combined.Right >= BoundsInRoot(second).Right);

            second.Margin = new Thickness(300, 24, 0, 0);
            await ArrangeAsync(root);
            Assert.HasCount(3, ((GeometryGroup)backdrop.Data).Children, "Separated targets need separate visible cutouts.");
        }
        finally { await CleanupAsync(root, tour); }
    }

    [TestMethod]
    public async Task When_PreparationIsPending_SkipIsUsableAndClosingRestoresPreviousFocus()
    {
        var target = CreateTarget();
        var previous = new Button { Content = "Previous focus", HorizontalAlignment = HorizontalAlignment.Right };
        var tour = new DaisyTour();
        var entered = NewCompletion();
        var release = NewCompletion();
        tour.Steps.Add(new DaisyTourStep { Id = "ready-focus", Target = target });
        tour.Steps.Add(new DaisyTourStep
        {
            Id = "pending-focus", Target = target,
            PrepareAsync = async (_, _) => { entered.TrySetResult(); await release.Task; }
        });
        var root = CreateRoot(tour, target, previous);
        try
        {
            await LoadAsync(root);
            Assert.IsTrue(previous.Focus(FocusState.Programmatic));
            Assert.AreSame(previous, FocusManager.GetFocusedElement(root.XamlRoot));
            await tour.StartAsync().WaitAsync(Timeout);
            var starting = tour.NextAsync();
            await entered.Task.WaitAsync(Timeout);
            await ArrangeAsync(root);
            var surface = GetSurface(tour);
            var skip = Descendants<DaisyButton>(surface).Single(button => AutomationProperties.GetAutomationId(button) == "DaisyTourSkip");
            var next = Descendants<DaisyButton>(surface).Single(button => AutomationProperties.GetAutomationId(button) == "DaisyTourNext");
            Assert.IsTrue(skip.IsEnabled);
            Assert.IsTrue(skip.ActualWidth > 0 && skip.ActualHeight > 0);
            Assert.IsFalse(next.IsEnabled);
            Assert.IsTrue(skip.Focus(FocusState.Programmatic));
            Assert.AreSame(skip, FocusManager.GetFocusedElement(root.XamlRoot));
            var peer = Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer.CreatePeerForElement(skip);
            Assert.IsNotNull(peer);
            var invoke = peer.GetPattern(Microsoft.UI.Xaml.Automation.Peers.PatternInterface.Invoke)
                as Microsoft.UI.Xaml.Automation.Provider.IInvokeProvider;
            Assert.IsNotNull(invoke);
            invoke.Invoke();
            await ArrangeAsync(root);
            AssertClosed(tour);
            Assert.AreSame(previous, FocusManager.GetFocusedElement(root.XamlRoot));
            release.TrySetResult();
            await starting.WaitAsync(Timeout);
            AssertClosed(tour);
        }
        finally { release.TrySetResult(); await CleanupAsync(root, tour); }
    }

    private static async Task VerifyAnnotationPixelsAsync(DaisyTourAnnotationKind kind,
        DaisyTourAnnotationPreset preset = DaisyTourAnnotationPreset.Default,
        DaisyTourStrokeStyle strokeStyle = DaisyTourStrokeStyle.Default)
    {
        var root = new Canvas { Width = 600, Height = 400, Background = new SolidColorBrush(Colors.Black) };
        var calibration = new Border { Width = 20, Height = 20, Background = new SolidColorBrush(Colors.Blue) };
        Canvas.SetLeft(calibration, 10);
        Canvas.SetTop(calibration, 10);
        var presenter = new DaisyTourAnnotationPresenter { Width = 600, Height = 400 };
        var presenterCalibration = new Border { Width = 20, Height = 20, Background = new SolidColorBrush(Colors.Yellow) };
        Canvas.SetLeft(presenterCalibration, 10);
        Canvas.SetTop(presenterCalibration, 40);
        presenter.Children.Add(presenterCalibration);
        root.Children.Add(calibration);
        root.Children.Add(presenter);
        var annotation = new DaisyTourAnnotation
        {
            Kind = kind, Preset = preset, StrokeStyle = strokeStyle,
            Path = strokeStyle switch
            {
                DaisyTourStrokeStyle.Dotted => DaisyTourAnnotationPath.Curved,
                DaisyTourStrokeStyle.Dashed => DaisyTourAnnotationPath.Elbow,
                _ => DaisyTourAnnotationPath.Default
            },
            DashArray = strokeStyle == DaisyTourStrokeStyle.Custom ? new DaisyTourDashCollection { 6, 2, 1, 2, 1, 2 } : null,
            Geometry = kind == DaisyTourAnnotationKind.Custom ? new EllipseGeometry { Center = new Point(60, 40), RadiusX = 60, RadiusY = 40 } : null,
            StrokeThickness = 12, Width = 120, Height = 80, HeadLength = 48, HeadWidth = 44,
            SourceAnchor = DaisyTourAnchor.Right, TargetAnchor = DaisyTourAnchor.Left,
            Stroke = new SolidColorBrush(Colors.Red), Fill = new SolidColorBrush(Colors.Lime)
        };
        try
        {
            await LoadAsync(root);
            var isSurface = kind is DaisyTourAnnotationKind.Cloud or DaisyTourAnnotationKind.SpeechBubble or DaisyTourAnnotationKind.Custom;
            var source = isSurface ? new Rect(140, 120, 120, 100) : new Rect(80, 180, 80, 40);
            var target = new Rect(440, strokeStyle == DaisyTourStrokeStyle.Dashed ? 260 : 180, 80, 40);
            presenter.Update(annotation, source, target,
                new SolidColorBrush(Colors.Red), new SolidColorBrush(Colors.Lime), new SolidColorBrush(Colors.White));
            await ArrangeAsync(root);
            Assert.AreEqual(Visibility.Collapsed, presenter.Children[4].Visibility,
                "The rendering probe must contain no annotation text or content.");
            var bitmap = new RenderTargetBitmap();
            await bitmap.RenderAsync(root, 600, 400).AsTask().WaitAsync(Timeout);
            var pixels = (await bitmap.GetPixelsAsync()).ToArray();
            Assert.IsTrue(bitmap.PixelWidth > 0 && bitmap.PixelHeight > 0);
            Assert.AreEqual(bitmap.PixelWidth * bitmap.PixelHeight * 4, pixels.Length);
            var calibrationPixels = CountRenderedColor(pixels, bitmap, new Rect(10, 10, 20, 20), Colors.Blue);
            var presenterCalibrationPixels = CountRenderedColor(pixels, bitmap, new Rect(10, 40, 20, 20), Colors.Yellow);
            Assert.IsTrue(calibrationPixels >= 100, "Native bitmap calibration must render before shape pixels are assessed.");
            var strokeRegion = isSurface
                ? new Rect(130, 110, 140, 120)
                : kind == DaisyTourAnnotationKind.Line ? new Rect(170, 150, 260, 150) : new Rect(220, 150, 160, 120);
            var fillRegion = isSurface
                ? new Rect(150, 140, 100, 60) : new Rect(388, 170, 60, 60);
            var strokePixels = CountRenderedColor(pixels, bitmap, strokeRegion, Colors.Red);
            var fillPixels = CountRenderedColor(pixels, bitmap, fillRegion, Colors.Lime);
            var startHeadPixels = preset == DaisyTourAnnotationPreset.TwoEndedArrow
                ? CountRenderedColor(pixels, bitmap, new Rect(150, 170, 60, 60), Colors.Lime) : 0;
            var paths = presenter.Children.OfType<Path>().Select((path, index) =>
                $"Path{index}: Actual={path.ActualWidth}x{path.ActualHeight}, Desired={path.DesiredSize}, DataBounds={path.Data?.Bounds}, Canvas={Canvas.GetLeft(path)},{Canvas.GetTop(path)}, Origin={path.TransformToVisual(presenter).TransformPoint(new Point())}, Stroke={DescribeBrush(path.Stroke)}, Fill={DescribeBrush(path.Fill)}, Thickness={path.StrokeThickness}, Visibility={path.Visibility}, Opacity={path.Opacity}");
            var diagnostic = $"{kind}/{preset}/{strokeStyle}: RedStrokePixels={strokePixels}, LimeFillPixels={fillPixels}, StartHeadPixels={startHeadPixels}, CalibrationPixels={calibrationPixels}, PresenterCalibrationPixels={presenterCalibrationPixels}, PresenterCalibrationActual={presenterCalibration.ActualWidth}x{presenterCalibration.ActualHeight}, PresenterCalibrationDesired={presenterCalibration.DesiredSize}, Bitmap={bitmap.PixelWidth}x{bitmap.PixelHeight}, PresenterActual={presenter.ActualWidth}x{presenter.ActualHeight}, RootActual={root.ActualWidth}x{root.ActualHeight}. {string.Join("; ", paths)}";
            var renderedCorrectly = strokePixels >= 100 && (kind == DaisyTourAnnotationKind.Line || fillPixels >= 20)
                && (preset != DaisyTourAnnotationPreset.TwoEndedArrow || startHeadPixels >= 20);
            Assert.IsTrue(renderedCorrectly, diagnostic);
        }
        finally
        {
            RuntimeTestHelpers.GetHostPanel().Children.Remove(root);
            await ArrangeAsync(root);
        }
    }

    private static int CountRenderedColor(byte[] pixels, RenderTargetBitmap bitmap, Rect region, Color expected)
    {
        var left = Math.Clamp((int)Math.Floor(region.Left * bitmap.PixelWidth / 600), 0, bitmap.PixelWidth);
        var right = Math.Clamp((int)Math.Ceiling(region.Right * bitmap.PixelWidth / 600), 0, bitmap.PixelWidth);
        var top = Math.Clamp((int)Math.Floor(region.Top * bitmap.PixelHeight / 400), 0, bitmap.PixelHeight);
        var bottom = Math.Clamp((int)Math.Ceiling(region.Bottom * bitmap.PixelHeight / 400), 0, bitmap.PixelHeight);
        var matches = 0;
        for (var y = top; y < bottom; y++)
        {
            for (var x = left; x < right; x++)
            {
                var offset = (y * bitmap.PixelWidth + x) * 4;
                if (pixels[offset + 3] >= 240 && Math.Abs(pixels[offset] - expected.B) <= 20 &&
                    Math.Abs(pixels[offset + 1] - expected.G) <= 20 && Math.Abs(pixels[offset + 2] - expected.R) <= 20)
                    matches++;
            }
        }
        return matches;
    }

    private static string DescribeBrush(Brush? brush) => brush is SolidColorBrush solid
        ? $"#{solid.Color.A:X2}{solid.Color.R:X2}{solid.Color.G:X2}{solid.Color.B:X2}" : brush?.GetType().Name ?? "null";

    private sealed class PreparationGate
    {
        internal TaskCompletionSource Entered { get; } = NewCompletion();
        internal TaskCompletionSource Release { get; } = NewCompletion();
        internal CancellationToken Token { get; private set; }

        internal Task PrepareAsync(DaisyTourStep _, CancellationToken token)
        {
            Token = token;
            Entered.TrySetResult();
            return Release.Task;
        }
    }

    private static void AssertRetainedCard(DaisyTour tour, Border card, DaisyTourStep previous, int index, Rect previousBounds)
    {
        Assert.IsTrue(tour.IsTransitioning);
        Assert.AreEqual(Visibility.Visible, card.Visibility);
        Assert.AreSame(previous, tour.CurrentStep);
        Assert.AreEqual(index, tour.CurrentIndex);
        Assert.AreEqual(index, tour.Progress.SelectedIndex);
        Assert.AreEqual(previousBounds, tour.CardBounds);
        Assert.AreEqual(previousBounds.X, Canvas.GetLeft(card), 0.001, "Preparation must retain the last valid card X coordinate.");
        Assert.AreEqual(previousBounds.Y, Canvas.GetTop(card), 0.001, "Preparation must retain the last valid card Y coordinate.");
        Assert.IsTrue(Descendants<TextBlock>(card).Any(text => text.Text == previous.Title));
        var count = string.Format(System.Globalization.CultureInfo.CurrentCulture,
            FloweryLocalization.GetStringInternal("Tour_StepCount"), index + 1, tour.Steps.Count);
        Assert.IsTrue(Descendants<TextBlock>(card).Any(text => text.Text == count));
    }

    private static void AssertPlacedCard(DaisyTour tour, Border card, FrameworkElement target)
    {
        Assert.AreEqual(Visibility.Visible, card.Visibility);
        Assert.AreEqual(tour.CardBounds.X, Canvas.GetLeft(card), 0.001);
        Assert.AreEqual(tour.CardBounds.Y, Canvas.GetTop(card), 0.001);
        Assert.IsTrue(tour.CardBounds.Width > 0 && tour.CardBounds.Height > 0);
        AssertTargetBounds(tour, target);
    }

    private static async Task RenderFrameAsync(FrameworkElement root)
    {
        var rendered = NewCompletion();
        EventHandler<object> handler = (_, _) => rendered.TrySetResult();
        Microsoft.UI.Xaml.Media.CompositionTarget.Rendering += handler;
        try
        {
            root.InvalidateArrange();
            root.UpdateLayout();
            await rendered.Task.WaitAsync(Timeout);
        }
        finally { Microsoft.UI.Xaml.Media.CompositionTarget.Rendering -= handler; }
    }

    private static async Task StartGalleryTourAsync(TourExamples page, DaisyTour tour)
    {
        var opened = NewCompletion();
        PropertyChangedEventHandler changed = (_, _) =>
        {
            if (tour.IsOpen && !tour.IsTransitioning && tour.CurrentIndex == 0) opened.TrySetResult();
        };
        EventHandler<DaisyTourClosedEventArgs> failed = (_, args) => opened.TrySetException(
            new InvalidOperationException($"Gallery tour closed while opening: {args.Reason}, Step={args.StepId}.", args.Error));
        tour.PropertyChanged += changed;
        tour.Closed += failed;
        try
        {
            var button = Descendants<DaisyButton>(page).Single(control =>
                Equals(control.Content, GalleryLocalization.GetString("Gallery_Tour_Start")));
            var peer = Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer.CreatePeerForElement(button);
            Assert.IsNotNull(peer);
            var invoke = peer.GetPattern(Microsoft.UI.Xaml.Automation.Peers.PatternInterface.Invoke)
                as Microsoft.UI.Xaml.Automation.Provider.IInvokeProvider;
            Assert.IsNotNull(invoke);
            invoke.Invoke();
            await opened.Task.WaitAsync(Timeout);
        }
        finally
        {
            tour.PropertyChanged -= changed;
            tour.Closed -= failed;
        }
    }

    private static Border CreateTarget() => new()
    {
        Width = 100, Height = 40, Margin = new Thickness(24),
        HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top,
        Child = new TextBlock { Text = "Tour target" }
    };

    private static Grid CreateRoot(DaisyTour tour, params UIElement[] children)
    {
        var root = new Grid { Width = 600, Height = 400 };
        tour.Closed += (_, args) => tour.Tag = args;
        foreach (var child in children) root.Children.Add(child);
        root.Children.Add(tour);
        return root;
    }

    private static void AssertActive(DaisyTour tour, DaisyTourStep step, int index)
    {
        var diagnostic = tour.Tag is DaisyTourClosedEventArgs closed
            ? $"Last closure: Reason={closed.Reason}, Step={closed.StepId}, Annotation={closed.AnnotationId}, Error={closed.Error}"
            : "No closure notification received.";
        Assert.IsTrue(tour.IsOpen, diagnostic);
        Assert.IsFalse(tour.IsTransitioning, diagnostic);
        Assert.AreSame(step, tour.CurrentStep, diagnostic);
        Assert.AreEqual(index, tour.CurrentIndex, diagnostic);
        Assert.AreEqual(index, tour.Progress.SelectedIndex, diagnostic);
    }

    private static void AssertClosed(DaisyTour tour)
    {
        Assert.IsFalse(tour.IsOpen);
        Assert.IsFalse(tour.IsTransitioning);
        Assert.IsNull(tour.CurrentStep);
        Assert.AreEqual(-1, tour.CurrentIndex);
    }

    private static void AssertTargetBounds(DaisyTour tour, FrameworkElement target)
    {
        var expected = BoundsInRoot(target);
        var actual = tour.TargetBounds;
        Assert.AreEqual(expected.X, actual.X, 1);
        Assert.AreEqual(expected.Y, actual.Y, 1);
        Assert.AreEqual(expected.Width, actual.Width, 1);
        Assert.AreEqual(expected.Height, actual.Height, 1);
    }

    private static Rect BoundsInRoot(FrameworkElement element) => element.TransformToVisual(element.XamlRoot.Content)
        .TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight));

    private static FrameworkElement GetSurface(DaisyTour tour) =>
        (FrameworkElement)VisualTreeHelper.GetOpenPopupsForXamlRoot(tour.XamlRoot)
            .Single(popup => Descendants<Border>(popup.Child).Any(border => AutomationProperties.GetAutomationId(border) == "DaisyTourCard")).Child;

    private static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            if (child is T typed) yield return typed;
            foreach (var descendant in Descendants<T>(child)) yield return descendant;
        }
    }

    private static void AssertFits(FrameworkElement child, FrameworkElement parent)
    {
        var bounds = child.TransformToVisual(parent).TransformBounds(new Rect(0, 0, child.ActualWidth, child.ActualHeight));
        Assert.IsTrue(bounds.X >= -1 && bounds.Right <= parent.ActualWidth + 1, "Target must fit horizontally in each scroll viewport.");
        Assert.IsTrue(bounds.Y >= -1 && bounds.Bottom <= parent.ActualHeight + 1, "Target must fit vertically in each scroll viewport.");
    }

    private static void AssertInViewport(Rect bounds, Size viewport, string diagnostic)
    {
        Assert.IsTrue(bounds.Width > 0 && bounds.Height > 0, diagnostic);
        Assert.IsTrue(bounds.Left >= -0.01 && bounds.Top >= -0.01 && bounds.Right <= viewport.Width + 0.01 &&
            bounds.Bottom <= viewport.Height + 0.01, $"{diagnostic} must fit in the overlay. Bounds={bounds}, Viewport={viewport}");
    }

    private static void AssertEndpoints(Path path, Point start, Point end)
    {
        Assert.IsInstanceOfType<PathGeometry>(path.Data);
        var figure = ((PathGeometry)path.Data).Figures.Single();
        Assert.AreEqual(start, figure.StartPoint);
        Assert.IsInstanceOfType<LineSegment>(figure.Segments.Last());
        Assert.AreEqual(end, ((LineSegment)figure.Segments.Last()).Point);
    }

    private static TaskCompletionSource NewCompletion() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static async Task LoadAsync(FrameworkElement root)
    {
        RuntimeTestHelpers.AttachToHost(root);
        await RuntimeTestHelpers.EnsureLoadedAsync(root).WaitAsync(Timeout);
        await ArrangeAsync(root);
    }

    private static async Task ArrangeAsync(FrameworkElement root)
    {
        root.UpdateLayout();
        // Target tracking queues geometry, which first yields for settled layout.
        for (var turn = 0; turn < 2; turn++)
        {
            var ready = NewCompletion();
            Assert.IsTrue(root.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () => ready.TrySetResult()));
            await ready.Task.WaitAsync(Timeout);
        }
    }

    private static async Task CleanupAsync(FrameworkElement root, DaisyTour tour)
    {
        tour.Cancel();
        RuntimeTestHelpers.GetHostPanel().Children.Remove(root);
        await ArrangeAsync(root);
    }
}
