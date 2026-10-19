using System;
using System.Reflection;
using System.Threading.Tasks;
using Flowery.Services;
using Flowery.Theming;
using Flowery.WinUI.Kanban.Controls;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Flowery.WinUI.RuntimeTests.Controls;

[TestClass]
public class Given_HighContrastKanban
{
    [TestMethod]
    public async Task When_CardSelectionAndEnabledStateChange_SystemPairsAndBoundariesRemainVisible()
    {
        var original = DaisySystemAppearance.HighContrast;
        var card = new FlowTaskCard { Title = "Contrast card", Width = 240 };
        try
        {
            SetContrast(true);
            RuntimeTestHelpers.AttachToHost(card);
            await RuntimeTestHelpers.EnsureLoadedAsync(card).WaitAsync(TimeSpan.FromSeconds(5));
            await Flush(card);
            var border = (Microsoft.UI.Xaml.Controls.Border)typeof(FlowTaskCard)
                .GetField("_rootBorder", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(card)!;
            Assert.IsNotNull(border);
            AssertColor("DaisyBase100Brush", card.Background);
            AssertColor("DaisyBaseContentBrush", card.Foreground);
            AssertColor("DaisyBorderBrush", border.BorderBrush);
            Assert.IsTrue(border.BorderThickness.Left >= 1);
            card.IsSelected = true;
            AssertColor("DaisyPrimaryBrush", border.BorderBrush);
            Assert.AreEqual(new Thickness(2), border.BorderThickness);
            card.IsSelected = false;
            AssertColor("DaisyBorderBrush", border.BorderBrush);
            card.IsEnabled = false;
            AssertColor("DaisyDisabledForegroundBrush", card.Foreground);
            card.IsEnabled = true;
            AssertColor("DaisyBaseContentBrush", card.Foreground);
        }
        finally
        {
            RuntimeTestHelpers.GetHostPanel().Children.Remove(card);
            await Flush(card);
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
