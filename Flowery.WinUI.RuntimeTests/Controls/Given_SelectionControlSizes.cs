using System.Linq;
using System.Threading.Tasks;
using Flowery.Controls;
using Flowery.Enums;
using Flowery.Helpers;
using Flowery.Theming;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Flowery.WinUI.RuntimeTests.Controls
{
    [TestClass]
    public class Given_SelectionControlSizes
    {
        private static readonly DaisySize[] Tiers =
            [DaisySize.ExtraSmall, DaisySize.Small, DaisySize.Medium, DaisySize.Large, DaisySize.ExtraLarge];

        [TestMethod]
        public async Task When_CheckBoxSized_BoxMatchesLabelLineHeightAndRadiusIsAFifthOfIt()
        {
            double[] boxes = [14, 16, 20, 24, 28];
            double[] radii = [3, 3, 4, 5, 6]; // Math.Round(box / 5): a larger share turns the small boxes into circles
            for (var i = 0; i < Tiers.Length; i++)
            {
                var checkBox = new DaisyCheckBox { Content = "Label", Size = Tiers[i], IsChecked = true };
                RuntimeTestHelpers.AttachToHost(checkBox);
                await RuntimeTestHelpers.EnsureLoadedAsync(checkBox);
                await Task.Delay(100); // WinUI builds the visual content after Loaded
                checkBox.UpdateLayout();

                var box = Descendants<Border>(checkBox).First(b => b.Width > 0 && b.Width == b.Height);
                Assert.AreEqual(boxes[i], box.Width, $"CheckBox {Tiers[i]}");
                Assert.AreEqual(new CornerRadius(radii[i]), box.CornerRadius, $"CheckBox {Tiers[i]} corner radius");
            }
        }

        [TestMethod]
        public async Task When_RadioSized_IndicatorUsesItsOwnSizes()
        {
            double[] outer = [14, 16, 20, 24, 28];
            double[] dots = [8, 10, 12, 14, 16];
            for (var i = 0; i < Tiers.Length; i++)
            {
                var radio = new DaisyRadio { Content = "Label", Size = Tiers[i], IsChecked = true };
                RuntimeTestHelpers.AttachToHost(radio);
                await RuntimeTestHelpers.EnsureLoadedAsync(radio);
                await Task.Delay(100);
                radio.UpdateLayout();

                var squares = Descendants<FrameworkElement>(radio)
                    .Where(e => e.Width > 0 && e.Width == e.Height)
                    .Select(e => e.Width)
                    .Distinct()
                    .OrderByDescending(w => w)
                    .ToList();
                CollectionAssert.Contains(squares, outer[i], $"Radio {Tiers[i]} outer");
                CollectionAssert.Contains(squares, dots[i], $"Radio {Tiers[i]} dot");
            }
        }

        [TestMethod]
        public async Task When_RadioLoaded_RingAndDotSkipLayoutRounding()
        {
            var radio = new DaisyRadio { Content = "Label", IsChecked = true };
            RuntimeTestHelpers.AttachToHost(radio);
            await RuntimeTestHelpers.EnsureLoadedAsync(radio);

            // Like the WinUI RadioButton: rounding ring and dot separately moves the dot off center at 125 % or 175 %.
            var ring = Descendants<Border>(radio).Single(b => b.Name == "PART_IndicatorBorder");
            var dot = Descendants<Ellipse>(radio).Single(e => e.Name == "PART_InnerDot");
            Assert.IsFalse(ring.UseLayoutRounding, "Radio ring UseLayoutRounding");
            Assert.IsFalse(dot.UseLayoutRounding, "Radio dot UseLayoutRounding");
        }

        [TestMethod]
        public async Task When_ToggleSized_TrackAndKnobUseTokenSizesAndKnobStaysInside()
        {
            double[] widths = [28, 32, 40, 48, 56];
            double[] heights = [14, 16, 20, 24, 28];
            double[] knobs = [8, 10, 12, 14, 16];
            for (var i = 0; i < Tiers.Length; i++)
            {
                var toggle = new DaisyToggle { Size = Tiers[i], IsOn = true };
                RuntimeTestHelpers.AttachToHost(toggle);
                await RuntimeTestHelpers.EnsureLoadedAsync(toggle);
                await Task.Delay(300); // let the knob animation finish

                var (track, knob) = GetToggleParts(toggle);
                Assert.AreEqual(widths[i], track.Width, $"Toggle {Tiers[i]} width");
                Assert.AreEqual(heights[i], track.Height, $"Toggle {Tiers[i]} height");
                Assert.AreEqual(knobs[i], knob.Width, $"Toggle {Tiers[i]} knob");

                // The knob is positioned inside the track border; the automatic padding equals the vertical gap,
                // so the checked knob keeps the same gap to the right edge as to the top and bottom.
                var innerWidth = track.Width - track.BorderThickness.Left - track.BorderThickness.Right;
                var innerHeight = track.Height - track.BorderThickness.Top - track.BorderThickness.Bottom;
                var gap = (innerHeight - knobs[i]) / 2;
                var offset = KnobOffset(knob);
                Assert.AreEqual(innerWidth - knobs[i] - gap, offset, 0.01, $"Toggle {Tiers[i]} checked knob offset");
                Assert.AreEqual(heights[i], offset - gap, 0.01, $"Toggle {Tiers[i]} knob travels one track height (WinUI ToggleSwitch)");
            }
        }

        [TestMethod]
        public async Task When_ToggleUnchecked_KnobSitsAtTheAutomaticGap()
        {
            for (var i = 0; i < Tiers.Length; i++)
            {
                var toggle = new DaisyToggle { Size = Tiers[i] };
                RuntimeTestHelpers.AttachToHost(toggle);
                await RuntimeTestHelpers.EnsureLoadedAsync(toggle);
                await Task.Delay(100);

                var (track, knob) = GetToggleParts(toggle);
                var innerHeight = track.Height - track.BorderThickness.Top - track.BorderThickness.Bottom;
                var gap = (innerHeight - knob.Width) / 2;
                Assert.AreEqual(gap, KnobOffset(knob), 0.01, $"Toggle {Tiers[i]} unchecked knob offset");
            }
        }

        [TestMethod]
        public async Task When_TogglePaddingIsSet_ItReplacesTheAutomaticGapInBothStates()
        {
            const double padding = 1; // the automatic gap of the default Medium size is 3
            var toggle = new DaisyToggle { TogglePadding = padding };
            RuntimeTestHelpers.AttachToHost(toggle);
            await RuntimeTestHelpers.EnsureLoadedAsync(toggle);
            await Task.Delay(100);

            var (track, knob) = GetToggleParts(toggle);
            Assert.AreEqual(padding, KnobOffset(knob), 0.01, "Unchecked knob offset");

            toggle.IsOn = true;
            await Task.Delay(300); // let the knob animation finish
            var innerWidth = track.Width - track.BorderThickness.Left - track.BorderThickness.Right;
            Assert.AreEqual(innerWidth - knob.Width - padding, KnobOffset(knob), 0.01, "Checked knob offset");
        }

        [TestMethod]
        public async Task When_CheckedKnobMatchesTheTrack_ItGetsContrastAnOutlineAndAVariantBorder()
        {
            // A track painted in the Secondary color hides a Secondary knob unless the toggle adjusts it.
            var secondary = (SolidColorBrush)DaisyResourceLookup.GetBrush("DaisySecondaryBrush");
            var toggle = new DaisyToggle { Variant = DaisyToggleVariant.Secondary, IsOn = true };
            toggle.Resources["DaisyToggleTrackBackground"] = new SolidColorBrush(secondary.Color);
            RuntimeTestHelpers.AttachToHost(toggle);
            await RuntimeTestHelpers.EnsureLoadedAsync(toggle);
            await Task.Delay(100);

            var (track, knob) = GetToggleParts(toggle);
            var knobColor = ((SolidColorBrush)knob.Fill).Color;
            Assert.AreNotEqual(secondary.Color, knobColor, "The knob is adjusted away from the track color.");
            Assert.IsTrue(FloweryColorHelpers.GetColorDifference(knobColor, secondary.Color) >= 20,
                "The adjusted knob stands out from the track.");
            Assert.AreEqual(1, knob.StrokeThickness, "The knob has an outline.");
            Assert.IsInstanceOfType<SolidColorBrush>(knob.Stroke, "The outline has a color.");
            Assert.AreEqual(secondary.Color, ((SolidColorBrush)track.BorderBrush).Color, "The checked track border takes the variant color.");

            toggle.IsOn = false;
            await Task.Delay(100);
            Assert.AreNotEqual(secondary.Color, ((SolidColorBrush)track.BorderBrush).Color, "The unchecked track border is neutral again.");
        }

        [TestMethod]
        public async Task When_ToggleResizedAfterLoad_MeasuresLikeAToggleCreatedAtThatSize()
        {
            // A global size change sets Size on toggles that are already loaded; nothing from the first size may stick.
            (DaisySize Initial, DaisySize Target)[] changes =
                [(DaisySize.Medium, DaisySize.ExtraSmall), (DaisySize.ExtraSmall, DaisySize.ExtraLarge)];
            foreach (var (initial, target) in changes)
            {
                var resized = await LoadToggleAsync(initial);
                resized.Size = target;
                await Task.Delay(100);
                resized.UpdateLayout();
                var resizedWidth = resized.ActualWidth;
                var resizedHeight = resized.ActualHeight;

                var fresh = await LoadToggleAsync(target);
                Assert.AreEqual(fresh.ActualWidth, resizedWidth, 0.01, $"Toggle {initial} -> {target} width");
                Assert.AreEqual(fresh.ActualHeight, resizedHeight, 0.01, $"Toggle {initial} -> {target} height");
            }
        }

        [TestMethod]
        public void When_IndicatorSizesPageIsRead_ItMirrorsTheLiveTokens()
        {
            var root = FindRepositoryRoot();
            Assert.IsNotNull(root, $"Flowery.WinUI.sln was not found above {AppContext.BaseDirectory}.");

            var page = File.ReadAllText(System.IO.Path.Combine(root, "Flowery.WinUI.RuntimeTests", "VisualChecks", "indicator-sizes.html"));

            // Writes DaisyTokenDefaults into the application resources, as loading any Daisy control does.
            DaisyResourceLookup.EnsureTokens();

            // The page draws toggle, checkbox and radio labels with FONT, so all three font tokens must match it.
            (string Name, string Prefix, string Suffix)[] mirrors =
            [
                ("FONT", "DaisyToggle", "FontSize"),
                ("FONT", "DaisyCheckbox", "FontSize"),
                ("FONT", "DaisyRadio", "FontSize"),
                ("CONTROL", "DaisySize", "Height"),
                ("toggleWidth", "DaisyToggle", "Width"),
                ("toggleHeight", "DaisyToggle", "Height"),
                ("knob", "DaisyToggleKnob", "Size"),
                ("checkbox", "DaisyCheckbox", "Size"),
                ("checkmark", "DaisyCheckmark", "Size"),
                ("radio", "DaisyRadio", "Size"),
                ("radioDot", "DaisyRadioDot", "Size")
            ];
            foreach (var (name, prefix, suffix) in mirrors)
            {
                var values = ReadPageArray(page, name);
                for (var i = 0; i < Tiers.Length; i++)
                {
                    var key = prefix + DaisyResourceLookup.GetSizeKeyFull(Tiers[i]) + suffix;
                    Assert.AreEqual(DaisyResourceLookup.GetDouble(key, double.NaN), values[i],
                        $"indicator-sizes.html {name}[{Tiers[i]}] must equal {key}");
                }
            }

            var radii = ReadPageArray(page, "checkboxRadius");
            for (var i = 0; i < Tiers.Length; i++)
            {
                var key = "DaisyCheckbox" + DaisyResourceLookup.GetSizeKeyFull(Tiers[i]) + "Size";
                Assert.AreEqual(Math.Round(DaisyResourceLookup.GetDouble(key, double.NaN) / 5), radii[i],
                    $"indicator-sizes.html checkboxRadius[{Tiers[i]}] must equal Math.Round({key} / 5) (DaisyCheckBox.ApplySizing)");
            }
        }

        [TestMethod]
        public async Task When_ToggleHasNoHeader_ItIsExactlyAsWideAsItsTrack()
        {
            var bare = await LoadToggleAsync(DaisySize.Medium);
            var (track, _) = GetToggleParts(bare);
            Assert.AreEqual(track.Width, bare.ActualWidth, 0.01, "Toggle without Header must not reserve the header gap");

            var labeled = await LoadToggleAsync(DaisySize.Medium);
            labeled.Header = "Label";
            await Task.Delay(100);
            labeled.UpdateLayout();
            Assert.IsTrue(labeled.ActualWidth > track.Width + 8, "Toggle with Header shows the label after the 8 px gap");
        }

        private static async Task<DaisyToggle> LoadToggleAsync(DaisySize size)
        {
            // Top-left alignment keeps the host grid from stretching the toggle, so ActualWidth/Height are its own size.
            var toggle = new DaisyToggle
            {
                Size = size,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top
            };
            RuntimeTestHelpers.AttachToHost(toggle);
            await RuntimeTestHelpers.EnsureLoadedAsync(toggle);
            await Task.Delay(100);
            toggle.UpdateLayout();
            return toggle;
        }

        private static (Border Track, Ellipse Knob) GetToggleParts(DaisyToggle toggle) =>
            (Descendants<Border>(toggle).Single(b => b.Name == "SwitchArea"),
                Descendants<Ellipse>(toggle).Single(e => e.Name == "Knob"));

        private static double KnobOffset(Ellipse knob) => ((TranslateTransform)knob.RenderTransform).X;

        private static string? FindRepositoryRoot()
        {
            for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
            {
                if (File.Exists(System.IO.Path.Combine(directory.FullName, "Flowery.WinUI.sln")))
                    return directory.FullName;
            }

            return null;
        }

        // Reads a numeric JS array of the page, e.g. "var FONT = [10, 12, ...]" or "knob: [8, 10, ...]".
        private static double[] ReadPageArray(string page, string name)
        {
            var match = Regex.Match(page, $@"\b{name}\s*[:=]\s*\[([^\]]*)\]");
            Assert.IsTrue(match.Success, $"indicator-sizes.html has no array {name}");
            var values = match.Groups[1].Value
                .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Select(value => double.Parse(value, CultureInfo.InvariantCulture))
                .ToArray();
            Assert.HasCount(Tiers.Length, values, $"indicator-sizes.html {name} needs one value per size tier");
            return values;
        }

        private static System.Collections.Generic.IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
        {
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                if (child is T match) yield return match;
                foreach (var nested in Descendants<T>(child)) yield return nested;
            }
        }
    }
}
