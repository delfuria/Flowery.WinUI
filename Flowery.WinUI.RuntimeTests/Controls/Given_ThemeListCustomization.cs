using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Flowery.Controls;
using Flowery.Theming;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Flowery.WinUI.RuntimeTests.Controls
{
    [TestClass]
    public class Given_ThemeListCustomization
    {
        [TestMethod]
        public async Task When_ThemeHidden_RemovedFromListsUntilShownAgain()
        {
            var dropdown = await LoadDropdownAsync();
            var raised = 0;
            EventHandler handler = (_, _) => raised++;
            DaisyThemeManager.AvailableThemesChanged += handler;
            try
            {
                Assert.IsTrue(DaisyThemeManager.HideTheme("Dracula"));
                Assert.IsFalse(DaisyThemeManager.HideTheme("dracula"), "Hiding twice must report no change.");
                Assert.IsFalse(DaisyThemeManager.IsThemeVisible("Dracula"));
                Assert.IsFalse(DaisyThemeManager.AvailableThemes.Any(t => t.Name == "Dracula"));
                Assert.IsNull(Find(dropdown, "Dracula"));
                Assert.IsNotNull(DaisyThemeManager.GetThemeInfo("Dracula"), "Hiding must not unregister the theme.");

                Assert.IsTrue(DaisyThemeManager.ShowTheme("Dracula"));
                Assert.IsNotNull(Find(dropdown, "Dracula"));
                Assert.AreEqual(2, raised);
            }
            finally
            {
                DaisyThemeManager.AvailableThemesChanged -= handler;
                DaisyThemeManager.ShowTheme("Dracula");
            }
        }

        [TestMethod]
        public async Task When_PreferredThemesSet_TheyComeFirstWithOneSeparator()
        {
            var previous = DaisyThemeManager.PreferredThemes;
            var dropdown = await LoadDropdownAsync();
            try
            {
                DaisyThemeManager.PreferredThemes = new[] { "Nord", "Dim" };

                CollectionAssert.AreEqual(new[] { "Dim", "Nord" },
                    DaisyThemeManager.AvailableThemes.Take(2).Select(t => t.Name).ToArray());

                var items = Items(dropdown);
                CollectionAssert.AreEqual(new[] { "Dim", "Nord" }, items.Take(2).Select(t => t.Name).ToArray());
                Assert.IsTrue(items[2].HasSeparatorBefore, "The first non-preferred theme starts the second group.");
                Assert.AreEqual(1, items.Count(t => t.HasSeparatorBefore));
            }
            finally
            {
                DaisyThemeManager.PreferredThemes = previous;
            }

            Assert.IsFalse(Items(dropdown).Any(t => t.HasSeparatorBefore), "No separator without a preferred group.");
        }

        [TestMethod]
        public async Task When_ThemeAddedGlobally_ProductThemeShowsDisplayNameWithoutApplying()
        {
            var product = UnregisteredProductTheme();
            var current = DaisyThemeManager.CurrentThemeName;
            var dropdown = await LoadDropdownAsync();
            try
            {
                DaisyThemeManager.AddTheme(product.ToUpperInvariant(), "My product", preferred: true);

                Assert.IsNotNull(DaisyThemeManager.GetThemeInfo(product), "AddTheme registers product themes.");
                Assert.AreEqual(current, DaisyThemeManager.CurrentThemeName, "AddTheme must not apply a theme.");
                var first = Items(dropdown)[0];
                Assert.AreEqual(product, first.Name, "The original name is kept, matched case-insensitively.");
                Assert.AreEqual("My product", first.DisplayName);
            }
            finally
            {
                DaisyThemeManager.RemoveThemeOverride(product);
            }

            var restored = Find(dropdown, product);
            Assert.IsNotNull(restored, "The registered product theme stays in the list.");
            Assert.AreEqual(product, restored.DisplayName);
        }

        [TestMethod]
        public async Task When_ThemeAddedLocally_OnlyThatDropdownChanges()
        {
            var first = new DaisyThemeDropdown();
            var second = new DaisyThemeDropdown();
            var host = RuntimeTestHelpers.GetHostPanel();
            host.Children.Clear();
            var panel = new StackPanel();
            panel.Children.Add(first);
            panel.Children.Add(second);
            host.Children.Add(panel);
            await RuntimeTestHelpers.EnsureLoadedAsync(first);
            await RuntimeTestHelpers.EnsureLoadedAsync(second);

            first.AddTheme("Dark", "Night mode", preferred: true);

            Assert.AreEqual("Night mode", Items(first)[0].DisplayName);
            Assert.AreEqual("Dark", Find(second, "Dark")!.DisplayName);
            Assert.IsFalse(Items(second).Any(t => t.HasSeparatorBefore));

            Assert.IsTrue(first.RemoveThemeOverride("dark"));
            Assert.IsFalse(first.RemoveThemeOverride("Dark"));
            Assert.AreEqual("Dark", Find(first, "Dark")!.DisplayName);
        }

        [TestMethod]
        public async Task When_AddedProductThemeSelected_ItIsRegisteredAndApplied()
        {
            var product = UnregisteredProductTheme();
            var previous = DaisyThemeManager.CurrentThemeName ?? "Dark";
            var dropdown = await LoadDropdownAsync();
            try
            {
                dropdown.AddTheme(product, "Selected product", preferred: true);
                Assert.IsNull(DaisyThemeManager.GetThemeInfo(product), "A local AddTheme does not register the theme.");

                dropdown.SelectedItem = Items(dropdown)[0];
                for (var attempt = 0; attempt < 50 && DaisyThemeManager.CurrentThemeName != product; attempt++)
                    await Task.Delay(50);

                Assert.AreEqual(product, DaisyThemeManager.CurrentThemeName);
                Assert.IsNotNull(DaisyThemeManager.GetThemeInfo(product));
            }
            finally
            {
                DaisyThemeManager.ApplyTheme(previous);
            }
        }

        [TestMethod]
        public void When_NamesInvalid_Throws()
        {
            Assert.ThrowsExactly<ArgumentException>(() => DaisyThemeManager.AddTheme("NoSuchTheme", "x", false));
            Assert.ThrowsExactly<ArgumentException>(() => DaisyThemeManager.AddTheme("Dark", " ", false));
            Assert.ThrowsExactly<ArgumentException>(() => DaisyThemeManager.ExcludedThemes = new[] { "Dark", " " });
            Assert.ThrowsExactly<ArgumentNullException>(() => DaisyThemeManager.PreferredThemes = null!);
            Assert.IsFalse(DaisyThemeManager.RemoveThemeOverride("Dark"));
        }

        private static async Task<DaisyThemeDropdown> LoadDropdownAsync()
        {
            var dropdown = new DaisyThemeDropdown();
            RuntimeTestHelpers.AttachToHost(dropdown);
            await RuntimeTestHelpers.EnsureLoadedAsync(dropdown);
            return dropdown;
        }

        private static List<ThemePreviewInfo> Items(DaisyThemeDropdown dropdown) =>
            (dropdown.ItemsSource as IEnumerable<ThemePreviewInfo>)?.ToList() ?? [];

        private static ThemePreviewInfo? Find(DaisyThemeDropdown dropdown, string name) =>
            Items(dropdown).FirstOrDefault(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase));

        private static string UnregisteredProductTheme() =>
            ProductPaletteFactory.GetAll().First(p => DaisyThemeManager.GetThemeInfo(p.Name) == null).Name;
    }
}
