using System.Reflection;
using Microsoft.UI.Dispatching;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Flowery.WinUI.RuntimeTests.Controls;

[TestClass]
public class Given_HighContrastThemeControls
{
    [TestMethod]
    public async Task When_SlideContrastEnds_TrackBackgroundAndIdleLabelOpacityRestore()
    {
        var original = DaisySystemAppearance.HighContrast;
        var background = new SolidColorBrush(Colors.Coral);
        var control = new DaisySlideToConfirm { Background = background };
        try
        {
            SetContrast(false);
            await Load(control);
            var track = Part<Border>(control, "_track");
            var label = Part<TextBlock>(control, "_label");
            Assert.AreSame(background, track.Background);
            Assert.AreEqual(0.7, label.Opacity, 0.000001);
            SetContrast(true);
            await Flush(control);
            AssertColor("DaisyBase100Brush", track.Background);
            Assert.AreEqual(1d, label.Opacity);
            SetContrast(false);
            await Flush(control);
            Assert.AreSame(background, track.Background);
            Assert.AreEqual(0.7, label.Opacity, 0.000001);
            Assert.AreEqual(new Thickness(0), Part<Border>(control, "_handle").BorderThickness);
        }
        finally
        {
            RuntimeTestHelpers.GetHostPanel().Children.Clear();
            await Flush(control);
            SetContrast(original);
        }
    }

    [TestMethod]
    public async Task When_CustomInteractiveTemplatesReceiveKeyboardFocus_SystemFocusVisualsAreEnabled()
    {
        var original = DaisySystemAppearance.HighContrast;
        var controls = new Control[]
        {
            new DaisyButton { Content = "Button" }, new DaisyCheckBox { Content = "Check" },
            new DaisyRadio { Content = "Radio" }, new DaisyToggle(), new DaisyThemeController(),
            new DaisyThemeRadio { Content = "Theme" }, new DaisyRange(), new DaisyRating(),
            new DaisySlideToConfirm(), new DaisyCollapse(), new DaisyDateTimeline(), new DaisyNumberFlow()
        };
        var panel = new StackPanel();
        foreach (var control in controls) panel.Children.Add(control);
        try
        {
            SetContrast(true);
            await Load(panel);
            foreach (var control in controls)
            {
                Assert.IsTrue(control.UseSystemFocusVisuals, control.GetType().Name);
                Assert.IsTrue(control.Focus(FocusState.Keyboard), control.GetType().Name);
                await Flush(control);
                Assert.AreEqual(FocusState.Keyboard, control.FocusState, control.GetType().Name);
            }
        }
        finally
        {
            RuntimeTestHelpers.GetHostPanel().Children.Clear();
            await Flush(panel);
            SetContrast(original);
        }
    }

    [TestMethod]
    public async Task When_IndeterminateCheckboxDisables_MixedStateKeepsItsSystemColorPair()
    {
        var original = DaisySystemAppearance.HighContrast;
        var control = new DaisyCheckBox { IsThreeState = true, IsChecked = null };
        try
        {
            SetContrast(true);
            await Load(control);
            var box = Part<Border>(control, "_checkboxBorder");
            var mark = Part<Path>(control, "_checkmark");
            Assert.IsNull(control.IsChecked);
            Assert.AreEqual(Visibility.Visible, mark.Visibility);
            AssertColor("DaisyPrimaryBrush", box.Background);
            AssertColor("DaisyPrimaryContentBrush", mark.Stroke);
            control.IsEnabled = false;
            await Flush(control);
            Assert.IsNull(control.IsChecked);
            Assert.AreEqual(Visibility.Visible, mark.Visibility);
            AssertColor("DaisyBase100Brush", box.Background);
            AssertColor("DaisyDisabledForegroundBrush", mark.Stroke);
            control.IsEnabled = true;
            await Flush(control);
            AssertColor("DaisyPrimaryBrush", box.Background);
            AssertColor("DaisyPrimaryContentBrush", mark.Stroke);
        }
        finally
        {
            RuntimeTestHelpers.GetHostPanel().Children.Clear();
            await Flush(control);
            SetContrast(original);
        }
    }

    [TestMethod]
    public async Task When_NumericInputRejectsText_ErrorCompletionRestoresItsCurrentFocusBorder()
    {
        var original = DaisySystemAppearance.HighContrast;
        var control = new DaisyNumericUpDown { Value = 20, ShowInputError = true, InputErrorDuration = 1 };
        try
        {
            SetContrast(true);
            await Load(control);
            var input = Part<TextBox>(control, "_textBox");
            var border = Part<Border>(control, "_rootBorder");
            Assert.IsTrue(input.Focus(FocusState.Keyboard));
            await Flush(control);
            AssertColor("DaisyFocusBrush", border.BorderBrush);
            input.Text = "invalid";
            var timer = Part<DispatcherTimer>(control, "_errorFlashTimer");
            var finished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            void OnFinished(object? sender, object args) => finished.TrySetResult();
            timer.Tick += OnFinished;
            try { await finished.Task.WaitAsync(TimeSpan.FromSeconds(5)); }
            finally { timer.Tick -= OnFinished; }
            await Flush(control);
            Assert.AreEqual(20m, control.Value);
            Assert.AreEqual(FocusState.Keyboard, input.FocusState);
            AssertColor("DaisyFocusBrush", border.BorderBrush);
        }
        finally
        {
            RuntimeTestHelpers.GetHostPanel().Children.Clear();
            await Flush(control);
            SetContrast(original);
        }
    }

    [TestMethod]
    public async Task When_ThemeControllerModesChange_CheckedAndDisabledGraphicsUseSystemPairs()
    {
        var originalContrast = DaisySystemAppearance.HighContrast;
        var originalTheme = DaisyThemeManager.CurrentThemeName;
        var panel = new StackPanel();
        try
        {
            SetContrast(true);
            await Load(panel);
            foreach (var mode in Enum.GetValues<ThemeControllerMode>())
            {
                var control = new DaisyThemeController { Mode = mode };
                panel.Children.Add(control);
                await RuntimeTestHelpers.EnsureLoadedAsync(control).WaitAsync(TimeSpan.FromSeconds(5));
                foreach (var enabled in new[] { true, false })
                {
                    control.IsEnabled = enabled;
                    foreach (var selected in new[] { false, true })
                    {
                        control.IsChecked = selected;
                        await Flush(control);
                        AssertColor(enabled ? "DaisyBaseContentBrush" : "DaisyDisabledForegroundBrush", control.Foreground);
                        var trackName = mode switch
                        {
                            ThemeControllerMode.Toggle => "ToggleMode",
                            ThemeControllerMode.Checkbox => "CheckboxMode",
                            ThemeControllerMode.ToggleWithText => "TextToggleMode",
                            ThemeControllerMode.ToggleWithIcons => "IconToggleMode",
                            _ => null
                        };
                        if (trackName != null)
                        {
                            var track = Descendants(control).OfType<Border>().Single(element => element.Name == trackName);
                            Assert.AreEqual(new Thickness(1), track.BorderThickness, mode.ToString());
                            AssertColor(enabled ? "DaisyBaseContentBrush" : "DaisyDisabledForegroundBrush", track.BorderBrush);
                            AssertColor(enabled && selected ? "DaisyPrimaryBrush" : "DaisyBase100Brush", track.Background);
                            var glyph = Descendants(track).OfType<Shape>().Single();
                            AssertColor(!enabled ? "DaisyDisabledForegroundBrush" : selected ? "DaisyPrimaryContentBrush" : "DaisyBaseContentBrush", glyph.Fill);
                        }
                        else
                        {
                            foreach (var icon in Descendants(control).OfType<Path>())
                                AssertColor(enabled ? "DaisyBaseContentBrush" : "DaisyDisabledForegroundBrush", icon.Fill);
                        }
                    }
                }
                control.IsEnabled = true;
                SetContrast(false);
                await Flush(control);
                foreach (var track in Descendants(control).OfType<Border>().Where(element => element.Name is "ToggleMode" or "CheckboxMode" or "TextToggleMode" or "IconToggleMode"))
                {
                    Assert.AreEqual(new Thickness(0), track.BorderThickness, mode.ToString());
                    AssertColor("DaisyBase300Brush", track.Background);
                }
                SetContrast(true);
                panel.Children.Remove(control);
                await Flush(panel);
            }
        }
        finally
        {
            RuntimeTestHelpers.GetHostPanel().Children.Clear();
            await Flush(panel);
            SetContrast(originalContrast);
            if (originalTheme != null) DaisyThemeManager.ApplyTheme(originalTheme);
        }
    }

    [TestMethod]
    public async Task When_ThemeRadioModesChange_CheckedHoverAndDisabledColorsStayPaired()
    {
        var original = DaisySystemAppearance.HighContrast;
        var panel = new StackPanel();
        try
        {
            SetContrast(true);
            await Load(panel);
            foreach (var mode in Enum.GetValues<ThemeRadioMode>())
            {
                var control = new DaisyThemeRadio { Mode = mode, Content = "Theme" };
                panel.Children.Add(control);
                await RuntimeTestHelpers.EnsureLoadedAsync(control).WaitAsync(TimeSpan.FromSeconds(5));
                foreach (var enabled in new[] { true, false })
                {
                    control.IsEnabled = enabled;
                    foreach (var selected in new[] { false, true })
                    {
                        control.IsChecked = selected;
                        foreach (var hovered in new[] { false, true })
                        {
                            typeof(DaisyThemeRadio).GetField("_isPointerOver", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(control, hovered);
                            typeof(DaisyThemeRadio).GetMethod("ApplyAll", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(control, null);
                            await Flush(control);
                            if (mode == ThemeRadioMode.Button)
                            {
                                var active = enabled && (selected || hovered);
                                AssertColor(active ? "DaisyPrimaryBrush" : "DaisyBase100Brush", control.Background);
                                AssertColor(!enabled ? "DaisyDisabledForegroundBrush" : active ? "DaisyPrimaryContentBrush" : "DaisyBaseContentBrush", control.Foreground);
                            }
                            else
                            {
                                var indicator = Part<Border>(control, "_radioIndicatorBorder");
                                AssertColor(enabled ? "DaisyBaseContentBrush" : "DaisyDisabledForegroundBrush", indicator.BorderBrush);
                                AssertColor(selected ? enabled ? "DaisyBaseContentBrush" : "DaisyDisabledForegroundBrush" : "DaisyBase100Brush", indicator.Background);
                                AssertColor("DaisyBase100Brush", Part<Ellipse>(control, "_radioCheckMark").Fill);
                                AssertColor(enabled ? "DaisyBaseContentBrush" : "DaisyDisabledForegroundBrush", control.Foreground);
                            }
                        }
                    }
                }
                panel.Children.Remove(control);
                await Flush(panel);
            }
        }
        finally
        {
            RuntimeTestHelpers.GetHostPanel().Children.Clear();
            await Flush(panel);
            SetContrast(original);
        }
    }

    [TestMethod]
    public async Task When_LoginBrandsChange_ContrastGlyphsRefreshAndOriginalBrandColorsRestore()
    {
        var original = DaisySystemAppearance.HighContrast;
        var buttons = Enum.GetValues<DaisyLoginBrand>().Select(brand => new DaisyLoginButton { Brand = brand }).ToArray();
        var panel = new StackPanel();
        foreach (var button in buttons) panel.Children.Add(button);
        try
        {
            SetContrast(false);
            await Load(panel);
            var originalColors = buttons.ToDictionary(button => button, GlyphColors);
            var originalTextColors = buttons.ToDictionary(button => button, button => ColorOf(Part<TextBlock>(button, "_labelTextBlock").Foreground));
            SetContrast(true);
            await Flush(panel);
            foreach (var button in buttons)
            {
                Assert.IsTrue(originalColors[button].Length > 0, button.Brand.ToString());
                AssertColor("DaisyBaseContentBrush", Part<TextBlock>(button, "_labelTextBlock").Foreground);
                foreach (var color in GlyphColors(button)) Assert.AreEqual(ColorOf(DaisyResourceLookup.GetBrush("DaisyBaseContentBrush")), color, button.Brand.ToString());
            }
            foreach (var button in buttons) button.IsEnabled = false;
            await Flush(panel);
            foreach (var button in buttons)
            {
                AssertColor("DaisyDisabledForegroundBrush", Part<TextBlock>(button, "_labelTextBlock").Foreground);
                foreach (var color in GlyphColors(button)) Assert.AreEqual(ColorOf(DaisyResourceLookup.GetBrush("DaisyDisabledForegroundBrush")), color, button.Brand.ToString());
            }
            foreach (var button in buttons) button.IsEnabled = true;
            await Flush(panel);
            SetContrast(false);
            await Flush(panel);
            foreach (var button in buttons)
            {
                CollectionAssert.AreEqual(originalColors[button], GlyphColors(button), button.Brand.ToString());
                Assert.AreEqual(originalTextColors[button], ColorOf(Part<TextBlock>(button, "_labelTextBlock").Foreground), button.Brand.ToString());
                Assert.IsTrue(button.UseBrandColors);
            }
        }
        finally
        {
            RuntimeTestHelpers.GetHostPanel().Children.Clear();
            await Flush(panel);
            SetContrast(original);
        }
    }

    [TestMethod]
    public async Task When_FloatingInputsDisable_BothLabelPositionsKeepOpaqueDisabledColors()
    {
        var original = DaisySystemAppearance.HighContrast;
        var input = new DaisyInput { LabelPosition = DaisyLabelPosition.Floating, Label = "Name", Text = "Value" };
        var password = new DaisyPasswordBox { LabelPosition = DaisyLabelPosition.Floating, Label = "Password", Password = "Value" };
        var panel = new StackPanel { Children = { input, password } };
        try
        {
            SetContrast(true);
            await Load(panel);
            input.IsEnabled = password.IsEnabled = false;
            foreach (var text in new[] { "Value", string.Empty })
            {
                input.Text = password.Password = text;
                await Flush(panel);
                foreach (var control in new Control[] { input, password })
                {
                    AssertColor("DaisyDisabledForegroundBrush", Part<TextBlock>(control, "_floatingLabelText").Foreground);
                    Assert.AreEqual(1d, Part<Border>(control, "_floatingLabelBorder").Opacity);
                    if (text.Length > 0) AssertColor("DaisyDisabledForegroundBrush", Part<Border>(control, "_floatingLabelBorder").BorderBrush);
                }
            }
            input.IsEnabled = password.IsEnabled = true;
            await Flush(panel);
            AssertColor("DaisyBaseContentBrush", Part<TextBlock>(input, "_floatingLabelText").Foreground);
            AssertColor("DaisyBaseContentBrush", Part<TextBlock>(password, "_floatingLabelText").Foreground);
        }
        finally
        {
            RuntimeTestHelpers.GetHostPanel().Children.Clear();
            await Flush(panel);
            SetContrast(original);
        }
    }

    [TestMethod]
    public async Task When_SwatchAndModifierContrastChanges_BoundariesRefreshAndRestore()
    {
        var original = DaisySystemAppearance.HighContrast;
        var swatches = new DaisyTabColorGridMenuItem { Swatches = DaisyTabColorGridMenuItem.CreateDefaultSwatches() };
        var modifiers = new DaisyModifierKeys();
        var key = new DaisyKbd { Text = "Shift", KbdStyle = DaisyKbdStyle.Ghost };
        var panel = new StackPanel { Children = { swatches, modifiers, key } };
        try
        {
            SetContrast(false);
            await Load(panel);
            var grid = Part<Grid>(swatches, "_swatchGrid");
            var emptySwatch = grid.Children.OfType<Button>().Select(button => (Ellipse)button.Content).Single(ellipse => ellipse.Stroke != null);
            var originalStroke = ColorOf(emptySwatch.Stroke);
            SetContrast(true);
            await Flush(panel);
            AssertColor("DaisyBorderBrush", emptySwatch.Stroke);
            AssertColor("DaisyBorderBrush", Part<Border>(modifiers, "_separator").Background);
            Assert.AreEqual((byte)0, ((SolidColorBrush)key.BorderBrush).Color.A);
            AssertColor("DaisyBaseContentBrush", key.Foreground);
            foreach (var name in new[] { "PART_OuterRim", "PART_FacePolish", "PART_InnerBezel" })
                Assert.AreEqual(Visibility.Collapsed, Descendants(key).OfType<Border>().Single(border => border.Name == name).Visibility);
            key.KbdStyle = DaisyKbdStyle.Flat;
            AssertColor("DaisyBorderBrush", key.BorderBrush);
            swatches.IsEnabled = false;
            await Flush(panel);
            AssertColor("DaisyDisabledForegroundBrush", emptySwatch.Stroke);
            SetContrast(false);
            await Flush(panel);
            Assert.AreEqual(originalStroke, ColorOf(emptySwatch.Stroke));
            foreach (var name in new[] { "PART_OuterRim", "PART_FacePolish", "PART_InnerBezel" })
                Assert.AreEqual(Visibility.Visible, Descendants(key).OfType<Border>().Single(border => border.Name == name).Visibility);
        }
        finally
        {
            RuntimeTestHelpers.GetHostPanel().Children.Clear();
            await Flush(panel);
            SetContrast(original);
        }
    }

    private static Windows.UI.Color[] GlyphColors(DaisyLoginButton button) => Part<List<Path>>(button, "_iconPaths")
        .Select(path => ColorOf(path.StrokeThickness > 0 ? path.Stroke : path.Fill)).ToArray();

    private static Windows.UI.Color ColorOf(Brush brush) => ((SolidColorBrush)brush).Color;

    private static void AssertColor(string role, Brush brush) => Assert.AreEqual(ColorOf(DaisyResourceLookup.GetBrush(role)), ColorOf(brush), role);

    private static T Part<T>(object control, string name) where T : class
    {
        for (var type = control.GetType(); type != null; type = type.BaseType)
            if (type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(control) is T value) return value;
        throw new AssertFailedException($"Missing {control.GetType().Name}.{name}.");
    }

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
        if (element is Panel panel)
            foreach (var child in panel.Children.OfType<FrameworkElement>())
                await RuntimeTestHelpers.EnsureLoadedAsync(child).WaitAsync(TimeSpan.FromSeconds(5));
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
