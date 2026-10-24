using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Flowery.Controls;
using Flowery.Theming;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Flowery.WinUI.RuntimeTests.RuntimeTestHelpers;

namespace Flowery.WinUI.RuntimeTests.Controls;

[TestClass]
public class Given_NumberFlowBrushes
{
    [TestMethod]
    public async Task When_DigitForegroundChanges_StylesBindingsAndThemeFallbackSurviveReload()
    {
        var originalTheme = DaisyThemeManager.CurrentThemeName ?? "Dark";
        var host = GetHostPanel();
        var styledBrush = new SolidColorBrush(Colors.Crimson);
        var boundBrush = new SolidColorBrush(Colors.Gold);
        var transparent = new SolidColorBrush(Colors.Transparent);
        var source = new Border { Background = boundBrush };
        var styled = new DaisyNumberFlow
        {
            Value = 12.5m, FormatString = "N1", Prefix = "$", Suffix = "USD", ShowDigitBoxes = true,
            Style = new Style(typeof(DaisyNumberFlow)) { Setters = { new Setter(DaisyNumberFlow.DigitForegroundProperty, styledBrush) } }
        };
        var bound = new DaisyNumberFlow { Value = 12.5m, FormatString = "N1", Prefix = "$", Suffix = "USD" };
        bound.SetBinding(DaisyNumberFlow.DigitForegroundProperty, new Binding { Source = source, Path = new PropertyPath(nameof(Border.Background)) });
        var panel = new StackPanel { Children = { styled, bound } };
        try
        {
            DaisyThemeManager.ApplyTheme("Light");
            AttachToHost(panel);
            await CompleteLayoutAsync(panel, styled, bound);
            AssertParts(styled, styledBrush);
            AssertParts(bound, boundBrush);
            styled.DigitForeground = transparent;
            source.Background = transparent;
            await CompleteLayoutAsync(panel);
            AssertParts(styled, transparent);
            AssertParts(bound, transparent);
            Assert.IsNotNull(bound.GetBindingExpression(DaisyNumberFlow.DigitForegroundProperty));

            styled.ClearValue(DaisyNumberFlow.DigitForegroundProperty);
            source.Background = null;
            DaisyThemeManager.ApplyTheme("Dark");
            await CompleteLayoutAsync(panel);
            AssertParts(styled, styledBrush);
            AssertParts(bound, DaisyResourceLookup.GetBrush("DaisyBaseContentBrush"));
            var styledRoot = styled.Content;
            var boundRoot = bound.Content;
            var unloaded = new[] { styled, bound }.Select(WaitForUnloadedAsync).ToArray();
            host.Children.Remove(panel);
            await Task.WhenAll(unloaded);
            await CompleteLayoutAsync(host);
            DaisyThemeManager.ApplyTheme("Light");
            source.Background = boundBrush;
            host.Children.Add(panel);
            await CompleteLayoutAsync(panel, styled, bound);
            Assert.AreSame(styledRoot, styled.Content);
            Assert.AreSame(boundRoot, bound.Content);
            AssertParts(styled, styledBrush);
            AssertParts(bound, boundBrush);
            Assert.AreEqual(12.5m, bound.Value);
            Assert.IsNotNull(bound.GetBindingExpression(DaisyNumberFlow.DigitForegroundProperty));
        }
        finally
        {
            host.Children.Remove(panel);
            await CompleteLayoutAsync(host);
            DaisyThemeManager.ApplyTheme(originalTheme);
        }
    }

    private static void AssertParts(DaisyNumberFlow owner, Brush expected)
    {
        var row = (StackPanel)((Border)owner.Content).Child;
        var parts = (StackPanel)row.Children[1];
        var text = Descendants(parts).OfType<TextBlock>().ToArray();
        Assert.AreEqual(8, text.Length, "Three digits and one format symbol each have current and previous text.");
        foreach (var part in text) AssertBrush(expected, part.Foreground);
        AssertBrush(DaisyResourceLookup.GetBrush("DaisyBaseContentBrush"), ((TextBlock)row.Children[0]).Foreground);
        AssertBrush(DaisyResourceLookup.GetBrush("DaisyBaseContentBrush"), ((TextBlock)row.Children[2]).Foreground);
        Assert.AreEqual("$", ((TextBlock)row.Children[0]).Text);
        Assert.AreEqual("USD", ((TextBlock)row.Children[2]).Text);
        if (owner.ShowDigitBoxes)
            foreach (var part in parts.Children.OfType<Border>())
                AssertBrush(DaisyResourceLookup.GetBrush("DaisyNeutralBrush"), ((Border)((Grid)part.Child).Children[0]).Background);
    }

    private static void AssertBrush(Brush expected, object? actual)
    {
        Assert.IsInstanceOfType<SolidColorBrush>(actual);
        Assert.AreEqual(((SolidColorBrush)expected).Color, ((SolidColorBrush)actual).Color);
        Assert.AreEqual(expected.Opacity, ((Brush)actual).Opacity);
    }

    private static async Task WaitForUnloadedAsync(FrameworkElement element)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        RoutedEventHandler handler = (_, _) => completion.TrySetResult();
        element.Unloaded += handler;
        try { await completion.Task.WaitAsync(TimeSpan.FromSeconds(5)); }
        finally { element.Unloaded -= handler; }
    }

    private static IEnumerable<DependencyObject> Descendants(DependencyObject owner)
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(owner); index++)
        {
            var child = VisualTreeHelper.GetChild(owner, index);
            yield return child;
            foreach (var descendant in Descendants(child)) yield return descendant;
        }
    }
}
