using System;
using System.Reflection;
using System.Threading.Tasks;
using Flowery.Controls;
using Flowery.Services;
using Flowery.Theming;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Flowery.WinUI.RuntimeTests.Controls;

[TestClass]
public class Given_HighContrastJoin
{
    [TestMethod]
    public async Task When_SelectedButtonChanges_JoinOwnsAndRestoresOnlyItsContrastState()
    {
        var original = DaisySystemAppearance.HighContrast;
        var first = new DaisyButton { Content = "First" };
        var second = new DaisyButton { Content = "Second" };
        var join = new DaisyJoin { ActiveIndex = 0, Content = new StackPanel { Children = { first, second } } };
        try
        {
            SetContrast(true);
            RuntimeTestHelpers.AttachToHost(join);
            await RuntimeTestHelpers.EnsureLoadedAsync(join).WaitAsync(TimeSpan.FromSeconds(5));
            await RuntimeTestHelpers.EnsureLoadedAsync(first).WaitAsync(TimeSpan.FromSeconds(5));
            await Flush(join);
            Assert.IsTrue(first.IsExternallyControlled);
            Assert.IsFalse(second.IsExternallyControlled);
            AssertColor("DaisyPrimaryBrush", first.Background);
            AssertColor("DaisyPrimaryContentBrush", first.Foreground);
            first.IsEnabled = false;
            AssertColor("DaisyBase100Brush", first.Background);
            AssertColor("DaisyDisabledForegroundBrush", first.Foreground);
            AssertColor("DaisyDisabledForegroundBrush", first.BorderBrush);
            first.IsEnabled = true;
            AssertColor("DaisyPrimaryBrush", first.Background);
            AssertColor("DaisyPrimaryContentBrush", first.Foreground);
            join.ActiveIndex = 1;
            Assert.IsFalse(first.IsExternallyControlled);
            Assert.IsTrue(second.IsExternallyControlled);
            AssertColor("DaisyBase100Brush", first.Background);
            AssertColor("DaisyPrimaryBrush", second.Background);
            SetContrast(false);
            await Flush(join);
            Assert.IsFalse(first.IsExternallyControlled);
            Assert.IsFalse(second.IsExternallyControlled);
            SetContrast(true);
            await Flush(join);
            Assert.IsTrue(second.IsExternallyControlled);
            RuntimeTestHelpers.GetHostPanel().Children.Remove(join);
            await Flush(join);
            Assert.IsFalse(second.IsExternallyControlled);
        }
        finally
        {
            RuntimeTestHelpers.GetHostPanel().Children.Remove(join);
            await Flush(join);
            SetContrast(original);
        }
    }

    private static void AssertColor(string role, Brush brush) => Assert.AreEqual(
        ((SolidColorBrush)DaisyResourceLookup.GetBrush(role)).Color, ((SolidColorBrush)brush).Color, role);

    private static void SetContrast(bool enabled)
    {
        typeof(DaisySystemAppearance).GetField("_highContrast", BindingFlags.Static | BindingFlags.NonPublic)!.SetValue(null, enabled);
        var handlers = (EventHandler?)typeof(DaisySystemAppearance).GetField("ColorsChanged", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null);
        handlers?.Invoke(null, EventArgs.Empty);
    }

    private static async Task Flush(FrameworkElement element)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Assert.IsTrue(element.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () => completion.TrySetResult()));
        await completion.Task.WaitAsync(TimeSpan.FromSeconds(5));
        element.UpdateLayout();
    }
}
