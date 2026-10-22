using System.Linq;
using System.Threading.Tasks;
using Flowery.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Flowery.WinUI.RuntimeTests.Controls
{
    [TestClass]
    public class Given_TextAreaSizing
    {
        [TestMethod]
        public async Task When_TextAreasShareMinRows_TheInputBorderTakesTheRowHeightForEveryVariant()
        {
            // The text area raises MinHeight after the input applied its sizing on load; the visible border
            // used to keep the default 80 unless something else (a late placeholder binding) applied it again.
            var panel = new StackPanel { Width = 300 };
            var bordered = new DaisyTextArea { MinRows = 3 };
            var primary = new DaisyTextArea { Variant = DaisyInputVariant.Primary, MinRows = 3 };
            panel.Children.Add(bordered);
            panel.Children.Add(primary);
            RuntimeTestHelpers.AttachToHost(panel);
            await RuntimeTestHelpers.EnsureLoadedAsync(panel);
            await Task.Delay(100);

            Assert.IsTrue(bordered.MinHeight > 80, $"Three rows need more than the default 80 (MinHeight {bordered.MinHeight}).");
            Assert.AreEqual(bordered.MinHeight, GetInputBorder(bordered).MinHeight, 0.01, "Bordered input border");
            Assert.AreEqual(primary.MinHeight, GetInputBorder(primary).MinHeight, 0.01, "Primary input border");
            Assert.AreEqual(GetInputBorder(bordered).ActualHeight, GetInputBorder(primary).ActualHeight, 0.01, "Primary vs. Bordered height");

            bordered.MinRows = 6;
            await Task.Delay(100);
            Assert.AreEqual(bordered.MinHeight, GetInputBorder(bordered).MinHeight, 0.01, "Input border after a MinRows change");
        }

        // The input border sits behind the text box, so it is the sized border outside the text box template.
        private static Border GetInputBorder(DaisyTextArea area)
        {
            var textBoxBorders = Descendants<Border>(Descendants<TextBox>(area).Single()).ToHashSet();
            return Descendants<Border>(area).Single(b => b.MinHeight > 0 && !textBoxBorders.Contains(b));
        }

        private static System.Collections.Generic.IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
        {
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                if (child is T match)
                {
                    yield return match;
                }

                foreach (var descendant in Descendants<T>(child))
                {
                    yield return descendant;
                }
            }
        }
    }
}
