using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Flowery.Controls;
using Flowery.Services;
using Flowery.Theming;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Flowery.WinUI.RuntimeTests.Controls;

[TestClass]
public class Given_HighContrastNavigation
{
    [TestMethod]
    public async Task When_HoverGalleryContrastChanges_DividersUseOpaqueSystemColors()
    {
        var original = DaisySystemAppearance.HighContrast;
        var custom = new SolidColorBrush(Colors.Magenta);
        var gallery = new DaisyHoverGallery { Width = 240, Height = 80, DividerBrush = custom };
        gallery.Items.Add(new Border());
        gallery.Items.Add(new Border());
        try
        {
            SetContrast(false);
            await Load(gallery);
            var canvas = Part<Canvas>(gallery, "_dividersCanvas");
            Assert.AreSame(custom, ((Microsoft.UI.Xaml.Shapes.Rectangle)canvas.Children[0]).Fill);
            SetContrast(true);
            await Flush(gallery);
            var line = (Microsoft.UI.Xaml.Shapes.Rectangle)canvas.Children[0];
            AssertColor("DaisyBorderBrush", line.Fill);
            Assert.AreEqual(1d, line.Opacity);
            await SetEnabled(gallery, false);
            AssertColor("DaisyDisabledForegroundBrush", ((Microsoft.UI.Xaml.Shapes.Rectangle)canvas.Children[0]).Fill);
            await SetEnabled(gallery, true);
            SetContrast(false);
            await Flush(gallery);
            Assert.AreSame(custom, ((Microsoft.UI.Xaml.Shapes.Rectangle)canvas.Children[0]).Fill);
        }
        finally
        {
            RuntimeTestHelpers.GetHostPanel().Children.Clear();
            await Flush(gallery);
            SetContrast(original);
        }
    }

    [TestMethod]
    public async Task When_TextAreaContrastAndEnabledStateChange_ResizeGripRefreshes()
    {
        var original = DaisySystemAppearance.HighContrast;
        var area = new DaisyTextArea { Text = "Resize this field" };
        try
        {
            SetContrast(false);
            await Load(area);
            var grip = (Border)Part<Grid>(area, "_resizeGrip").Children[0];
            SetContrast(true);
            await Flush(area);
            AssertColor("DaisyBaseContentBrush", grip.Background);
            await SetEnabled(area, false);
            AssertColor("DaisyDisabledForegroundBrush", grip.Background);
            await SetEnabled(area, true);
            AssertColor("DaisyBaseContentBrush", grip.Background);
            SetContrast(false);
            await Flush(area);
            AssertColor("DaisyBaseContentBrush", grip.Background);
        }
        finally
        {
            RuntimeTestHelpers.GetHostPanel().Children.Clear();
            await Flush(area);
            SetContrast(original);
        }
    }

    [TestMethod]
    public async Task When_MenuStatesChange_SystemSelectionAndDisabledColorsStayPaired()
    {
        var original = DaisySystemAppearance.HighContrast;
        var item = new DaisyMenuItem { Content = "Navigation" };
        var custom = new SolidColorBrush(Colors.Magenta);
        item.Resources["DaisyMenuItemActiveBackground"] = custom;
        item.Resources["DaisyMenuItemActiveForeground"] = custom;
        item.Resources["DaisyMenuItemHoverBackground"] = custom;
        try
        {
            SetContrast(true);
            await Load(item);
            var border = Part<Border>(item, "_rootBorder");
            AssertColor("DaisyBaseContentBrush", item.Foreground);

            SetHover(item, "_isPointerOver", true, "RebuildVisual");
            AssertColor("DaisyPrimaryBrush", border.Background);
            AssertColor("DaisyPrimaryContentBrush", item.Foreground);

            item.IsActive = true;
            AssertColor("DaisyPrimaryBrush", border.Background);
            AssertColor("DaisyPrimaryContentBrush", item.Foreground);
            item.IsDisabled = true;
            AssertColor("DaisyBase100Brush", border.Background);
            AssertColor("DaisyDisabledForegroundBrush", item.Foreground);
            Assert.AreEqual(1d, border.Opacity);
            Assert.IsFalse(item.IsTabStop);

            item.IsDisabled = false;
            await SetEnabled(item, false);
            AssertColor("DaisyDisabledForegroundBrush", item.Foreground);
            await SetEnabled(item, true);
            AssertColor("DaisyPrimaryContentBrush", item.Foreground);

            SetContrast(false);
            await Flush(item);
            Assert.AreSame(custom, border.Background);
            Assert.AreSame(custom, item.Foreground);
        }
        finally
        {
            RuntimeTestHelpers.GetHostPanel().Children.Clear();
            await Flush(item);
            SetContrast(original);
        }
    }

    [TestMethod]
    public async Task When_TableStatesChange_CustomBrushesRestoreAfterSystemContrast()
    {
        var original = DaisySystemAppearance.HighContrast;
        var row = new DaisyTableRow { Content = new DaisyTableCell { Content = "Value" } };
        var table = new DaisyTable { Content = new StackPanel { Children = { row } } };
        var custom = new SolidColorBrush(Colors.Magenta);
        foreach (var suffix in new[] { "ActiveBackground", "ActiveForeground", "HoverBackground", "Foreground", "BorderBrush" })
            table.Resources["DaisyTableRow" + suffix] = custom;
        try
        {
            SetContrast(true);
            await Load(table);
            await RuntimeTestHelpers.EnsureLoadedAsync(row).WaitAsync(TimeSpan.FromSeconds(5));
            var border = Part<Border>(row, "_rootBorder");
            AssertColor("DaisyBorderBrush", Part<Border>(table, "_rootBorder").BorderBrush);
            AssertColor("DaisyBaseContentBrush", row.Foreground);
            AssertColor("DaisyBorderBrush", border.BorderBrush);

            SetHover(row, "_isHovering", true, "ApplyHoverState");
            AssertColor("DaisyBase100Brush", row.Background);
            AssertColor("DaisyFocusBrush", border.BorderBrush);
            row.IsActive = true;
            AssertColor("DaisyPrimaryBrush", row.Background);
            AssertColor("DaisyPrimaryContentBrush", row.Foreground);
            await SetEnabled(row, false);
            AssertColor("DaisyBase100Brush", row.Background);
            AssertColor("DaisyDisabledForegroundBrush", row.Foreground);
            AssertColor("DaisyDisabledForegroundBrush", border.BorderBrush);
            await SetEnabled(row, true);
            AssertColor("DaisyPrimaryContentBrush", row.Foreground);

            SetContrast(false);
            await Flush(table);
            Assert.AreSame(custom, row.Background);
            Assert.AreSame(custom, row.Foreground);
            Assert.AreSame(custom, border.BorderBrush);
        }
        finally
        {
            RuntimeTestHelpers.GetHostPanel().Children.Clear();
            await Flush(table);
            SetContrast(original);
        }
    }

    [TestMethod]
    public async Task When_TabVariantsChange_NativeStatesUseMatchingSystemColors()
    {
        var original = DaisySystemAppearance.HighContrast;
        var tabs = new DaisyTabs
        {
            Content = new StackPanel
            {
                Children =
                {
                    new DaisyTabItem { Header = "First", Content = new TextBlock { Text = "One" } },
                    new DaisyTabItem { Header = "Second", Content = new TextBlock { Text = "Two" } }
                }
            }
        };
        try
        {
            SetContrast(true);
            await Load(tabs);
            foreach (var variant in Enum.GetValues<DaisyTabVariant>())
            {
                tabs.Variant = variant;
                await Flush(tabs);
                var buttons = Descendants(tabs).OfType<Button>().Where(static button => button.Tag is int).OrderBy(static button => (int)button.Tag).ToArray();
                Assert.HasCount(2, buttons);
                for (var index = 0; index < buttons.Length; index++)
                {
                    var button = buttons[index];
                    AssertColor(index == tabs.SelectedIndex ? "DaisyPrimaryBrush" : "DaisyBase100Brush", button.Background);
                    AssertColor(index == tabs.SelectedIndex ? "DaisyPrimaryContentBrush" : "DaisyBaseContentBrush", button.Foreground);
                    Assert.AreEqual(DependencyProperty.UnsetValue, ((TextBlock)button.Content).ReadLocalValue(TextBlock.ForegroundProperty));
                    foreach (var state in new[] { "PointerOver", "Pressed" })
                    {
                        AssertColor("DaisyPrimaryBrush", (Brush)button.Resources["ButtonBackground" + state]);
                        AssertColor("DaisyPrimaryContentBrush", (Brush)button.Resources["ButtonForeground" + state]);
                    }
                    AssertColor("DaisyBase100Brush", (Brush)button.Resources["ButtonBackgroundDisabled"]);
                    AssertColor("DaisyDisabledForegroundBrush", (Brush)button.Resources["ButtonForegroundDisabled"]);
                    await SetEnabled(button, false);
                    var presenter = Descendants(button).OfType<ContentPresenter>().Single(candidate => ReferenceEquals(candidate.Content, button.Content));
                    AssertColor("DaisyDisabledForegroundBrush", presenter.Foreground);
                    SetContrast(false);
                    await Flush(tabs);
                    Assert.AreEqual(((SolidColorBrush)button.Resources["ButtonForegroundDisabled"]).Color,
                        ((SolidColorBrush)presenter.Foreground).Color);
                    SetContrast(true);
                    await Flush(tabs);
                    AssertColor("DaisyDisabledForegroundBrush", presenter.Foreground);
                    await SetEnabled(button, true);
                }
                tabs.SelectedIndex = 1 - tabs.SelectedIndex;
            }
        }
        finally
        {
            RuntimeTestHelpers.GetHostPanel().Children.Clear();
            await Flush(tabs);
            SetContrast(original);
        }
    }

    private static async Task SetEnabled(Control control, bool enabled)
    {
        control.IsEnabled = enabled;
        await Flush(control);
    }

    private static void SetHover(object control, string field, bool value, string apply)
    {
        var type = control.GetType();
        type.GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(control, value);
        type.GetMethod(apply, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(control, null);
    }

    private static void AssertColor(string role, Brush actual) => Assert.AreEqual(
        ((SolidColorBrush)DaisyResourceLookup.GetBrush(role)).Color, ((SolidColorBrush)actual).Color, role);

    private static T Part<T>(object control, string field) where T : class =>
        (T)control.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(control)!;

    private static void SetContrast(bool enabled)
    {
        typeof(DaisySystemAppearance).GetField("_highContrast", BindingFlags.Static | BindingFlags.NonPublic)!.SetValue(null, enabled);
        var handlers = (EventHandler?)typeof(DaisySystemAppearance).GetField("ColorsChanged", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null);
        handlers?.Invoke(null, EventArgs.Empty);
    }

    private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            yield return child;
            foreach (var descendant in Descendants(child)) yield return descendant;
        }
    }

    private static async Task Load(FrameworkElement element)
    {
        RuntimeTestHelpers.AttachToHost(element);
        await RuntimeTestHelpers.EnsureLoadedAsync(element).WaitAsync(TimeSpan.FromSeconds(5));
        await Flush(element);
    }

    private static async Task Flush(FrameworkElement element)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Assert.IsTrue(element.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () => completion.TrySetResult()));
        await completion.Task.WaitAsync(TimeSpan.FromSeconds(5));
        element.UpdateLayout();
    }
}
