using System;
using System.Threading.Tasks;
using Flowery.Controls;
using Flowery.Enums;
using Flowery.Helpers;
using Flowery.Theming;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.UI;

namespace Flowery.WinUI.RuntimeTests.Controls
{
    [TestClass]
    public class Given_ThemeContrast
    {
        [TestMethod]
        public void When_PaletteHasNoOverrides_ForegroundsFollowTheBaseHue()
        {
            var light = DaisyPaletteFactory.Create("Light");

            Assert.AreEqual(ColorOf(light, "DaisyPrimaryColor"), ColorOf(light, "DaisyPrimaryForegroundColor"));
            Assert.AreEqual(ColorOf(light, "DaisySecondaryColor"), ColorOf(light, "DaisySecondaryForegroundColor"));
            Assert.AreEqual(ColorOf(light, "DaisyAccentColor"), ColorOf(light, "DaisyAccentForegroundColor"));
            Assert.AreEqual(ColorOf(light, "DaisyPrimaryColor"), ColorOf(light, "DaisyButtonPrimaryForegroundColor"));
            Assert.IsFalse(light.ContainsKey("DaisyButtonPrimaryDisabledBrush"));
        }

        private static readonly (string Theme, string Key, string Expected)[] ContrastFixes =
        [
            ("Business", "DaisyPrimaryContentColor", "#FFFFFF"),
            ("Business", "DaisyButtonPrimaryForegroundColor", "#FFFFFF"),
            ("Business", "DaisyButtonPrimaryDisabledColor", "#2B4055"),
            ("Black", "DaisyPrimaryForegroundColor", "#D6D6D6"),
            ("Black", "DaisyAccentForegroundColor", "#D6D6D6"),
            ("Coffee", "DaisySecondaryForegroundColor", "#8DB9BB"),
            ("Coffee", "DaisyAccentForegroundColor", "#75C4DC"),
            ("Cupcake", "DaisyPrimaryForegroundColor", "#238F80"),
            ("Cupcake", "DaisyAccentForegroundColor", "#9F2D00"),
            ("Dark", "DaisyPrimaryForegroundColor", "#8D8AFF"),
            ("Dark", "DaisyButtonPrimaryForegroundColor", "#8D8AFF"),
            ("Lemonade", "DaisyAccentForegroundColor", "#756500"),
            ("Luxury", "DaisySecondaryForegroundColor", "#8BA8D8"),
            ("Luxury", "DaisyAccentForegroundColor", "#C394B5"),
            ("Pastel", "DaisyPrimaryForegroundColor", "#AF8BCB"),
            ("Pastel", "DaisySecondaryForegroundColor", "#D38F9B"),
            ("Retro", "DaisyPrimaryForegroundColor", "#801518"),
            ("Retro", "DaisySecondaryForegroundColor", "#00642E"),
            ("Smooth", "DaisyBase300Color", "#555555"),
            ("Wireframe", "DaisyPrimaryForegroundColor", "#242424"),
            ("Wireframe", "DaisyAccentForegroundColor", "#242424")
        ];

        [TestMethod]
        public void When_ThemeHasContrastFix_PaletteUsesCorrectedColor()
        {
            foreach (var (theme, key, expected) in ContrastFixes)
            {
                var palette = DaisyPaletteFactory.Create(theme);
                Assert.AreEqual(FloweryColorHelpers.ColorFromHex(expected), ColorOf(palette, key), $"{theme} {key}");
            }
        }

        [TestMethod]
        public async Task When_DarkThemeApplied_OutlineButtonAndIconTextUseLighterPrimary()
        {
            var previous = DaisyThemeManager.CurrentThemeName ?? "Dark";
            try
            {
                Assert.IsTrue(DaisyThemeManager.ApplyTheme("Light") || DaisyThemeManager.CurrentThemeName == "Light");
                Assert.IsTrue(DaisyThemeManager.ApplyTheme("Dark"));

                var button = new DaisyButton { Content = "Outline", Variant = DaisyButtonVariant.Primary, ButtonStyle = DaisyButtonStyle.Outline };
                RuntimeTestHelpers.AttachToHost(button);
                await RuntimeTestHelpers.EnsureLoadedAsync(button);
                Assert.AreEqual(FloweryColorHelpers.ColorFromHex("#8D8AFF"), (button.Foreground as SolidColorBrush)?.Color);

                var iconText = new DaisyIconText { Text = "Primary", Variant = DaisyBadgeVariant.Primary };
                RuntimeTestHelpers.AttachToHost(iconText);
                await RuntimeTestHelpers.EnsureLoadedAsync(iconText);
                Assert.AreEqual(FloweryColorHelpers.ColorFromHex("#8D8AFF"), (iconText.Foreground as SolidColorBrush)?.Color);
            }
            finally
            {
                DaisyThemeManager.ApplyTheme(previous);
            }
        }

        [TestMethod]
        public async Task When_BusinessThemeApplied_DisabledPrimaryButtonUsesOpaqueFill()
        {
            var previous = DaisyThemeManager.CurrentThemeName ?? "Dark";
            try
            {
                Assert.IsTrue(DaisyThemeManager.ApplyTheme("Business"));

                var button = new DaisyButton { Content = "Disabled", Variant = DaisyButtonVariant.Primary, IsEnabled = false };
                RuntimeTestHelpers.AttachToHost(button);
                await RuntimeTestHelpers.EnsureLoadedAsync(button);

                var fill = (button.Background as SolidColorBrush)?.Color;
                Assert.AreEqual(FloweryColorHelpers.ColorFromHex("#2B4055"), fill);
                Assert.AreEqual(1.0, (button.Background as SolidColorBrush)!.Opacity, 0.001);
            }
            finally
            {
                DaisyThemeManager.ApplyTheme(previous);
            }
        }

        [TestMethod]
        public void When_SwitchingToThemeWithoutForegroundKeys_PreviousValuesDoNotLinger()
        {
            var previous = DaisyThemeManager.CurrentThemeName ?? "Dark";
            var primary = Color.FromArgb(255, 0x12, 0x34, 0x56);
            DaisyThemeManager.RegisterTheme(
                new DaisyThemeInfo("PlainKeysTheme", true),
                () => new ResourceDictionary
                {
                    ["DaisyPrimaryColor"] = primary,
                    ["DaisyPrimaryBrush"] = new SolidColorBrush(primary)
                });
            try
            {
                Assert.IsTrue(DaisyThemeManager.ApplyTheme("Business"));
                Assert.IsTrue(DaisyResourceLookup.TryGetResource(Application.Current.Resources, "DaisyButtonPrimaryDisabledBrush", out _));

                Assert.IsTrue(DaisyThemeManager.ApplyTheme("PlainKeysTheme"));
                var resources = Application.Current.Resources;
                Assert.IsFalse(DaisyResourceLookup.TryGetResource(resources, "DaisyButtonPrimaryDisabledBrush", out _),
                    "An optional key from the previous theme must be removed.");
                Assert.AreEqual(primary, (DaisyResourceLookup.GetBrush(resources, "DaisyPrimaryForegroundBrush") as SolidColorBrush)?.Color);
                Assert.AreEqual(primary, (DaisyResourceLookup.GetBrush(resources, "DaisyButtonPrimaryForegroundBrush") as SolidColorBrush)?.Color);
            }
            finally
            {
                DaisyThemeManager.ApplyTheme(previous);
            }
        }

        private static Color ColorOf(ResourceDictionary palette, string key)
        {
            Assert.IsTrue(palette.TryGetValue(key, out var value), $"Missing {key}");
            return (Color)value;
        }
    }
}
