using System.Linq;
using System.Threading.Tasks;
using Flowery.Controls;
using Flowery.Services;
using Flowery.Theming;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.UI;
using static Flowery.WinUI.RuntimeTests.RuntimeTestHelpers;

namespace Flowery.WinUI.RuntimeTests.Controls;

[TestClass]
public class Given_SemanticBrushes
{
    [TestMethod]
    public void When_CustomPalettesChange_SemanticDerivationPreservesOverridesAndStableBrushes()
    {
        var originalTheme = DaisyThemeManager.CurrentThemeName ?? "Dark";
        var source = new SolidColorBrush(Color.FromArgb(128, 10, 20, 30));
        var authored = new SolidColorBrush(Colors.Crimson) { Opacity = 0.4 };
        var authoredOpacity = authored.Opacity;
        DaisyThemeManager.RegisterTheme(new DaisyThemeInfo("SemanticCustomTest", false), () => new ResourceDictionary
        {
            MergedDictionaries = { new ResourceDictionary
            {
                ["DaisyBaseContentBrush"] = source,
                ["DaisyPrimaryBrush"] = new SolidColorBrush(Colors.Blue),
                ["DaisyBaseContentMutedColor"] = Colors.Transparent,
                ["DaisyButtonPrimarySoftBrush"] = authored
            } }
        });
        DaisyThemeManager.RegisterTheme(new DaisyThemeInfo("SemanticPartialTest", false), () => new ResourceDictionary
        {
            ["DaisyPrimaryColor"] = Colors.Green
        });
        try
        {
            DaisyThemeManager.ApplyTheme("Light");
            var stable = DaisyResourceLookup.GetBrush("DaisyBaseContentSubtleBrush");
            Assert.IsTrue(DaisyThemeManager.ApplyTheme("SemanticCustomTest"));
            AssertColor("DaisyBaseContentMutedBrush", Colors.Transparent);
            AssertColor("DaisyBaseContentSubtleBrush", Color.FromArgb(64, 10, 20, 30));
            AssertColor("DaisyBaseContentDisabledBrush", Color.FromArgb(64, 10, 20, 30));
            AssertColor("DaisyButtonPrimarySoftBrush", Color.FromArgb(102, 220, 20, 60));
            Assert.AreEqual(Colors.Crimson, authored.Color);
            Assert.AreEqual(authoredOpacity, authored.Opacity);
            Assert.AreEqual((byte)128, source.Color.A);
            Assert.AreEqual(source.Color, (Color)Application.Current.Resources["DaisyBaseContentColor"]);
            AssertColor("DaisyBaseContentBrush", source.Color);
            Assert.AreSame(stable, DaisyResourceLookup.GetBrush("DaisyBaseContentSubtleBrush"));

            Assert.IsTrue(DaisyThemeManager.ApplyTheme("SemanticPartialTest"));
            AssertColor("DaisyBaseContentMutedBrush", Color.FromArgb(90, 10, 20, 30));
            AssertColor("DaisyBaseContentSubtleBrush", Color.FromArgb(64, 10, 20, 30));
            AssertColor("DaisyButtonPrimarySoftBorderBrush", Color.FromArgb(115, 0, 128, 0));
            Assert.AreSame(stable, DaisyResourceLookup.GetBrush("DaisyBaseContentSubtleBrush"));

            var palette = DaisyPaletteFactory.Create("Light");
            foreach (var variant in new[] { "Primary", "Secondary", "Accent", "Info", "Success", "Warning", "Error" })
                foreach (var (suffix, alpha) in new[] { ("Soft", (byte)38), ("SoftHover", (byte)76), ("SoftBorder", (byte)115) })
                {
                    var brush = (SolidColorBrush)palette["DaisyButton" + variant + suffix + "Brush"];
                    Assert.AreEqual(alpha, brush.Color.A);
                    Assert.AreEqual(1d, brush.Opacity);
                }
            Assert.AreEqual((byte)31, ((SolidColorBrush)DaisyPaletteFactory.Create("Business")["DaisyButtonPrimarySoftBrush"]).Color.A);
            DaisySystemAppearance.SetContrastColors(palette, Colors.Black, Colors.White, Colors.Blue, Colors.Yellow, Colors.Gray, Colors.Cyan);
            Assert.AreEqual(Colors.White, ((SolidColorBrush)palette["DaisyBaseContentMutedBrush"]).Color);
            Assert.AreEqual(Colors.White, ((SolidColorBrush)palette["DaisyBaseContentSubtleBrush"]).Color);
            Assert.AreEqual(Colors.Gray, ((SolidColorBrush)palette["DaisyBaseContentDisabledBrush"]).Color);
            Assert.AreEqual(Colors.Black, ((SolidColorBrush)palette["DaisyButtonPrimarySoftBrush"]).Color);
            Assert.AreEqual(Colors.Blue, ((SolidColorBrush)palette["DaisyButtonPrimarySoftHoverBrush"]).Color);
            Assert.AreEqual(Colors.White, ((SolidColorBrush)palette["DaisyButtonPrimarySoftBorderBrush"]).Color);
        }
        finally { DaisyThemeManager.ApplyTheme(originalTheme); }
    }

    [TestMethod]
    public async Task When_MenuItemBecomesATitle_ItUsesHeadingAndDisabledSemanticBrushes()
    {
        var originalTheme = DaisyThemeManager.CurrentThemeName ?? "Dark";
        var title = new DaisyMenuItem { Content = "Section", IsActive = true };
        var disabled = new DaisyMenuItem { Content = "Disabled", IsDisabled = true };
        var menu = new DaisyMenu { Content = new StackPanel { Children = { title, disabled } } };
        try
        {
            DaisyThemeManager.ApplyTheme("Light");
            AttachToHost(menu);
            await CompleteLayoutAsync(menu, title, disabled);
            title.IsTitle = true;
            await CompleteLayoutAsync(menu);
            var border = (Border)title.Content;
            Assert.IsFalse(title.IsTabStop);
            Assert.IsFalse(border.IsHitTestVisible);
            Assert.AreEqual(new Thickness(0, 0, 0, 1), border.BorderThickness);
            Assert.AreEqual(Microsoft.UI.Text.FontWeights.Bold, ((ContentPresenter)border.Child).FontWeight);
            AssertColor("DaisyPrimaryBrush", ((SolidColorBrush)title.Foreground).Color);
            AssertColor("DaisyBaseContentDisabledBrush", ((SolidColorBrush)disabled.Foreground).Color);
            Assert.AreEqual(1d, ((Border)disabled.Content).Opacity);
            var indicator = ((Grid)((Border)menu.Content).Child).Children.OfType<Border>().Single();
            Assert.AreEqual(0d, indicator.Opacity);
            title.IsTitle = false;
            await CompleteLayoutAsync(menu);
            Assert.IsTrue(title.IsTabStop);
            Assert.IsTrue(border.IsHitTestVisible);
            Assert.AreEqual(new Thickness(0), border.BorderThickness);
        }
        finally
        {
            GetHostPanel().Children.Remove(menu);
            await CompleteLayoutAsync(GetHostPanel());
            DaisyThemeManager.ApplyTheme(originalTheme);
        }
    }

    private static void AssertColor(string key, Color expected)
    {
        var actual = (SolidColorBrush)DaisyResourceLookup.GetBrush(key);
        Assert.AreEqual(expected, actual.Color, key);
        Assert.AreEqual(1d, actual.Opacity, key);
    }
}
