using System;
using System.Numerics;
using System.Threading.Tasks;
using Flowery.Controls;
using Flowery.WinUI.Gallery.Examples;
using Microsoft.UI.Composition;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Flowery.WinUI.RuntimeTests.Controls
{
    [TestClass]
    public class Given_CompositionShadowLifetime
    {
        [TestMethod]
        public async Task When_TechDemoResizesOrReloads_ShadowTracksSizeAndPreservesForeignVisual()
        {
            var button = new DaisyButtonTechDemo
            {
                Width = 140,
                CornerRadius = new CornerRadius(8),
                TechMode = DaisyButtonTechMode.DropShadow
            };
            SpriteVisual? foreignVisual = null;
            try
            {
                RuntimeTestHelpers.AttachToHost(button);
                await RuntimeTestHelpers.EnsureLoadedAsync(button);
                button.UpdateLayout();
                await FlushDispatcherAsync(button.DispatcherQueue);
                var oldBorder = FindBorder(button);
                var oldVisual = GetShadowVisual(oldBorder);
                var oldSurface = GetMaskSurface(oldVisual);

                button.Width = 230;
                button.UpdateLayout();
                await FlushDispatcherAsync(button.DispatcherQueue);
                Assert.AreSame(oldVisual, GetShadowVisual(oldBorder), "Resize must update the existing shadow.");
                Assert.AreSame(oldSurface, GetMaskSurface(oldVisual), "Resize must retain the mask surface.");
                Assert.AreEqual(new Vector2((float)button.ActualWidth, (float)button.ActualHeight), oldVisual.Size);
                Assert.AreEqual(oldVisual.Size, oldSurface.SourceSize);

                button.Template = (ControlTemplate)XamlReader.Load(
                    "<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' " +
                    "xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' xmlns:controls='using:Flowery.Controls' " +
                    "TargetType='controls:DaisyButton'><Border x:Name='ButtonBorder'><ContentPresenter /></Border></ControlTemplate>");
                button.ApplyTemplate();
                button.UpdateLayout();
                await FlushDispatcherAsync(button.DispatcherQueue);
                var newBorder = FindBorder(button);
                Assert.AreNotSame(oldBorder, newBorder);
                Assert.IsNull(ElementCompositionPreview.GetElementChildVisual(oldBorder), "The old template must release its shadow.");
                Assert.AreNotSame(oldVisual, GetShadowVisual(newBorder));

                foreignVisual = ElementCompositionPreview.GetElementVisual(newBorder).Compositor.CreateSpriteVisual();
                ElementCompositionPreview.SetElementChildVisual(newBorder, foreignVisual);
                button.TechMode = DaisyButtonTechMode.None;
                Assert.AreSame(foreignVisual, ElementCompositionPreview.GetElementChildVisual(newBorder),
                    "Clearing the demo must preserve a replacement child visual.");
                ElementCompositionPreview.SetElementChildVisual(newBorder, null);
                button.TechMode = DaisyButtonTechMode.DropShadow;

                RuntimeTestHelpers.GetHostPanel().Children.Clear();
                await FlushDispatcherAsync(button.DispatcherQueue);
                Assert.IsFalse(button.IsLoaded);
                Assert.IsNull(ElementCompositionPreview.GetElementChildVisual(newBorder));
                button.Width = 0;
                RuntimeTestHelpers.AttachToHost(button);
                await RuntimeTestHelpers.EnsureLoadedAsync(button);
                button.Width = 180;
                button.UpdateLayout();
                await FlushDispatcherAsync(button.DispatcherQueue);
                var reloadedVisual = GetShadowVisual(FindBorder(button));
                Assert.AreEqual(reloadedVisual.Size, GetMaskSurface(reloadedVisual).SourceSize);
            }
            finally
            {
                RuntimeTestHelpers.GetHostPanel().Children.Clear();
                await FlushDispatcherAsync(button.DispatcherQueue);
                foreignVisual?.Dispose();
            }
        }

        [TestMethod]
        public async Task When_DirectNeumorphicShadowResizesOrReloads_MaskUpdatesAndSuspendPreservesForeignVisual()
        {
            var previousRoundedMask = DaisyNeumorphicHelper.UseRoundedShadowMask;
            DaisyNeumorphicHelper.UseRoundedShadowMask = true;
            var owner = new ContentControl
            {
                Width = 120,
                Height = 70,
                CornerRadius = new CornerRadius(10),
                Template = (ControlTemplate)XamlReader.Load(
                    "<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' " +
                    "TargetType='ContentControl'><Border><TextBlock Text='Direct shadow' /></Border></ControlTemplate>")
            };
            DaisyNeumorphicHelper? helper = null;
            SpriteVisual? foreignVisual = null;
            try
            {
                // Register lifecycle handlers while detached so entering the tree schedules native Loaded.
                helper = new DaisyNeumorphicHelper(owner);
                RuntimeTestHelpers.AttachToHost(owner);
                await RuntimeTestHelpers.EnsureLoadedAsync(owner);
                owner.ApplyTemplate();
                owner.UpdateLayout();
                await FlushDispatcherAsync(owner.DispatcherQueue);
                helper.Update(true, DaisyNeumorphicMode.Raised, 0.6, 8);
                var visual = GetShadowVisual(owner);
                var surface = GetMaskSurface(visual);

                owner.Width = 200;
                owner.Height = 100;
                owner.UpdateLayout();
                await FlushDispatcherAsync(owner.DispatcherQueue);
                Assert.AreSame(visual, GetShadowVisual(owner));
                Assert.AreSame(surface, GetMaskSurface(visual));
                Assert.AreEqual(new Vector2(200, 100), visual.Size);
                Assert.AreEqual(visual.Size, surface.SourceSize, "The direct mask viewport must track the owner.");

                RuntimeTestHelpers.GetHostPanel().Children.Clear();
                await FlushDispatcherAsync(owner.DispatcherQueue);
                Assert.IsFalse(owner.IsLoaded);
                Assert.IsNull(ElementCompositionPreview.GetElementChildVisual(owner), "Unload must release the direct fallback shadow.");
                owner.Width = 0;
                RuntimeTestHelpers.AttachToHost(owner);
                await RuntimeTestHelpers.EnsureLoadedAsync(owner);
                owner.Width = 160;
                owner.UpdateLayout();
                await FlushDispatcherAsync(owner.DispatcherQueue);
                var reloaded = GetShadowVisual(owner);
                Assert.AreNotSame(visual, reloaded);
                Assert.AreEqual(reloaded.Size, GetMaskSurface(reloaded).SourceSize);

                foreignVisual = ElementCompositionPreview.GetElementVisual(owner).Compositor.CreateSpriteVisual();
                ElementCompositionPreview.SetElementChildVisual(owner, foreignVisual);
                helper.Suspend();
                Assert.AreSame(foreignVisual, ElementCompositionPreview.GetElementChildVisual(owner),
                    "Suspending the helper must preserve a replacement child visual.");
                ElementCompositionPreview.SetElementChildVisual(owner, null);
            }
            finally
            {
                helper?.Dispose();
                RuntimeTestHelpers.GetHostPanel().Children.Clear();
                await FlushDispatcherAsync(owner.DispatcherQueue);
                foreignVisual?.Dispose();
                DaisyNeumorphicHelper.UseRoundedShadowMask = previousRoundedMask;
            }
        }

        private static SpriteVisual GetShadowVisual(UIElement owner)
        {
            var visual = ElementCompositionPreview.GetElementChildVisual(owner);
            Assert.IsInstanceOfType<SpriteVisual>(visual);
            Assert.IsInstanceOfType<DropShadow>(((SpriteVisual)visual).Shadow);
            return (SpriteVisual)visual;
        }

        private static CompositionVisualSurface GetMaskSurface(SpriteVisual visual)
        {
            var shadow = (DropShadow)visual.Shadow;
            Assert.IsInstanceOfType<CompositionSurfaceBrush>(shadow.Mask);
            var brush = (CompositionSurfaceBrush)shadow.Mask;
            Assert.IsInstanceOfType<CompositionVisualSurface>(brush.Surface);
            return (CompositionVisualSurface)brush.Surface;
        }

        private static Border FindBorder(DependencyObject root)
        {
            return TryFindBorder(root) ?? throw new InvalidOperationException("The button template did not create ButtonBorder.");
        }

        private static Border? TryFindBorder(DependencyObject root)
        {
            if (root is Border { Name: "ButtonBorder" } border)
                return border;
            for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
            {
                if (TryFindBorder(VisualTreeHelper.GetChild(root, index)) is { } found)
                    return found;
            }
            return null;
        }

        private static Task FlushDispatcherAsync(DispatcherQueue dispatcher)
        {
            var completion = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
            if (!dispatcher.TryEnqueue(DispatcherQueuePriority.Low, () => completion.TrySetResult(null)))
                throw new InvalidOperationException("The native dispatcher rejected the shadow verification action.");
            return completion.Task;
        }

    }
}
