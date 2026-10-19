using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Flowery.Controls;
using Flowery.Services;
using Flowery.Theming;
using Flowery.WinUI.Gallery.Examples;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Flowery.WinUI.RuntimeTests.Controls;

[TestClass]
public class Given_HighContrastGallery
{
    [TestMethod]
    public async Task When_PlaceholderReloads_LabelsResolveCurrentTranslations()
    {
        var originalResolver = Flowery.Localization.FloweryLocalization.CustomResolver;
        var category = new SidebarCategory { Name = "Category" };
        var item = new SidebarItem { Id = "sample", Name = "Item" };
        category.Items.Add(item);
        var page = new CategoryPlaceholderView(category);
        try
        {
            Flowery.Localization.FloweryLocalization.CustomResolver = static key => "First " + key;
            await Load(page);
            Assert.AreEqual("First Category", Part<TextBlock>(page, "_header").Text);
            RuntimeTestHelpers.GetHostPanel().Children.Clear();
            await Flush(page);
            Flowery.Localization.FloweryLocalization.CustomResolver = static key => "Second " + key;
            await Load(page);
            Assert.AreEqual("Second Category", Part<TextBlock>(page, "_header").Text);
            Assert.IsTrue(Descendants(page).OfType<TextBlock>().Any(text => text.Text == "Second Item"));
        }
        finally
        {
            RuntimeTestHelpers.GetHostPanel().Children.Clear();
            await Flush(page);
            Flowery.Localization.FloweryLocalization.CustomResolver = originalResolver;
        }
    }

    [TestMethod]
    public async Task When_ProductFormContrastChanges_EnteredValuesAndLabelOpacityArePreserved()
    {
        var originalContrast = DaisySystemAppearance.HighContrast;
        var originalTheme = DaisyThemeManager.CurrentThemeName;
        var page = new ProductThemesExamples();
        try
        {
            SetContrast(false);
            await Load(page);
            var container = (StackPanel)page.FindName("FormContainer");
            var form = container.Children.Single();
            var input = Descendants(form).OfType<DaisyInput>().First();
            input.Text = "Retain this value";
            var labels = Descendants(form).OfType<TextBlock>()
                .Where(label => DaisyHighContrast.GetContentOpacity(label) < 1).ToArray();
            Assert.IsTrue(labels.Length > 0);
            foreach (var contrast in new[] { true, false })
            {
                SetContrast(contrast);
                await Flush(page);
                Assert.AreSame(form, container.Children.Single());
                Assert.AreEqual("Retain this value", input.Text);
                foreach (var label in labels)
                    Assert.AreEqual(contrast ? 1 : DaisyHighContrast.GetContentOpacity(label), label.Opacity, 0.000001);
            }
        }
        finally
        {
            RuntimeTestHelpers.GetHostPanel().Children.Clear();
            await Flush(page);
            SetContrast(originalContrast);
            if (originalTheme is not null) DaisyThemeManager.ApplyTheme(originalTheme);
        }
    }

    [TestMethod]
    public async Task When_StylingPageLoadsInContrast_CustomExamplesUseSemanticChrome()
    {
        var original = DaisySystemAppearance.HighContrast;
        Grid? scope = null;
        try
        {
            SetContrast(true);
            var page = new StylingExamples();
            scope = ContrastScope(page);
            await Load(scope);
            var elements = Descendants(page).ToArray();
            var caption = elements.OfType<TextBlock>().Single(text => text.Text == "Custom Styled Button");
            Assert.AreEqual(1d, caption.Opacity);
            AssertColor("DaisyBaseContentBrush", caption.Foreground);
            var button = elements.OfType<DaisyButton>().Single(control => Equals(control.Content, "Custom Colors"));
            AssertColor("DaisyBase100Brush", button.Background);
            AssertColor("DaisyBaseContentBrush", button.Foreground);
            Assert.IsNull(DaisyResourceLookup.TryGetControlBrush(button, "DaisyButton", "PrimaryBackground"));
            var input = elements.OfType<DaisyInput>().Single(control => control.Watermark == "Custom border...");
            AssertColor("DaisyBase100Brush", Part<Border>(input, "_inputBorder").Background);
            AssertColor("DaisyBorderBrush", Part<Border>(input, "_inputBorder").BorderBrush);
            var tabs = (DaisyTabs)page.FindName("StyledTabs");
            Assert.HasCount(3, tabs.Items);
            Assert.IsTrue(Descendants(tabs).OfType<Button>().Any(static control => control.Tag is int));
            var styledCardText = elements.OfType<TextBlock>().Single(text => text.Text == "Dark blue card");
            AssertColor("DaisyBaseContentBrush", styledCardText.Foreground);
        }
        finally
        {
            RuntimeTestHelpers.GetHostPanel().Children.Clear();
            if (scope is not null) await Flush(scope);
            SetContrast(original);
        }
    }

    [TestMethod]
    public async Task When_PatternsPageLoadsInContrast_DecorationHidesAndCaptionsStayReadable()
    {
        var original = DaisySystemAppearance.HighContrast;
        Grid? scope = null;
        try
        {
            SetContrast(true);
            var page = new PatternsExamples();
            scope = ContrastScope(page);
            await Load(scope);
            var cards = Descendants(page).OfType<DaisyPatternedCard>().ToArray();
            Assert.HasCount(16, cards);
            foreach (var card in cards)
            {
                await RuntimeTestHelpers.EnsureLoadedAsync(card).WaitAsync(TimeSpan.FromSeconds(5));
                await Flush(card);
                Assert.IsTrue(card.IsLoaded);
                Assert.AreEqual(Visibility.Collapsed, Part<Canvas>(card, "_patternLayer").Visibility);
                Assert.AreEqual(Visibility.Collapsed, Part<Canvas>(card, "_ornamentLayer").Visibility);
                AssertColor("DaisyBase100Brush", Part<Border>(card, "_solidBackground").Background);
                AssertColor("DaisyBaseContentBrush", card.Foreground);
            }
            var caption = Descendants(page).OfType<TextBlock>().Single(text => text.Text == "Accent markers");
            Assert.AreEqual(1d, caption.Opacity);
            AssertColor("DaisyBaseContentBrush", caption.Foreground);
        }
        finally
        {
            RuntimeTestHelpers.GetHostPanel().Children.Clear();
            if (scope is not null) await Flush(scope);
            SetContrast(original);
        }
    }

    [TestMethod]
    public void When_GalleryResourceRolesAreInspected_ContrastCaptionsAreOpaqueAndNormalValuesRemain()
    {
        var resources = GalleryResources();
        var defaults = (ResourceDictionary)resources.ThemeDictionaries["Default"];
        var contrast = (ResourceDictionary)resources.ThemeDictionaries["HighContrast"];
        Assert.IsFalse(resources.ThemeDictionaries.ContainsKey("Light"));
        Assert.IsFalse(resources.ThemeDictionaries.ContainsKey("Dark"));
        var directKeys = resources.Select(static resource => resource.Key).ToHashSet();
        foreach (var resource in contrast)
        {
            Assert.IsTrue(defaults.ContainsKey(resource.Key), resource.Key.ToString());
            Assert.IsFalse(directKeys.Contains(resource.Key), resource.Key.ToString());
        }
        foreach (var normal in new[] { 0.4, 0.5, 0.6, 0.7, 0.8, 0.85, 0.9 })
        {
            var key = "GalleryOpacity" + (int)Math.Round(normal * 100);
            Assert.AreEqual((double)(float)normal, (double)defaults[key], key);
            Assert.AreEqual(1d, (double)contrast[key], key);
        }
        Assert.AreEqual(0d, (double)contrast["GalleryHeaderOrbOpacity"]);
        Assert.AreEqual(0d, (double)contrast["GalleryHeaderSmallOrbOpacity"]);
        Assert.IsInstanceOfType<LinearGradientBrush>(defaults["GalleryHeaderBackgroundBrush"]);
        Assert.IsInstanceOfType<SolidColorBrush>(contrast["GalleryHeaderBackgroundBrush"]);
        Assert.IsInstanceOfType<LinearGradientBrush>(defaults["GalleryHoverFrontBackdropBrush"]);
        Assert.IsInstanceOfType<SolidColorBrush>(contrast["GalleryHoverFrontBackdropBrush"]);
    }

    private static ResourceDictionary GalleryResources() => new()
    {
        Source = new Uri("ms-appx:///Flowery.WinUI.Gallery.Core/GalleryResources.xaml")
    };

    private static Grid ContrastScope(FrameworkElement page)
    {
        var scope = new Grid { Children = { page } };
        // The flag exercises library policy. The dictionary exercises authored XAML resources
        // without changing the user's Windows contrast design from a unit test.
        var resources = GalleryResources();
        var contrast = (ResourceDictionary)resources.ThemeDictionaries["HighContrast"];
        Assert.IsTrue(resources.ThemeDictionaries.Remove("HighContrast"));
        scope.Resources.MergedDictionaries.Add(contrast);
        return scope;
    }

    private static void AssertColor(string role, Brush actual) => Assert.AreEqual(
        ((SolidColorBrush)DaisyResourceLookup.GetBrush(role)).Color, ((SolidColorBrush)actual).Color, role);

    private static T Part<T>(object control, string field) where T : class
    {
        for (var type = control.GetType(); type is not null; type = type.BaseType)
            if (type.GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(control) is T part)
                return part;
        throw new AssertFailedException($"Missing {control.GetType().Name}.{field}.");
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
