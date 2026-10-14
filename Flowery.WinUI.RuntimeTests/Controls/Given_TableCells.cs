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
    public class Given_TableCells
    {
        [TestMethod]
        public async Task When_CellsHaveStringOrElementContent_TextIsRendered()
        {
            var cells = new StackPanel { Orientation = Orientation.Horizontal };
            cells.Children.Add(new DaisyTableCell { Content = "String cell" });
            cells.Children.Add(new DaisyTableCell { Content = new TextBlock { Text = "Element cell" } });
            cells.Children.Add(new DaisyTableCell { Content = "Fixed cell", ColumnWidth = 120 });
            var row = new DaisyTableRow { Content = cells };
            var panel = new StackPanel { Width = 600 };
            panel.Children.Add(row);

            RuntimeTestHelpers.AttachToHost(panel);
            await RuntimeTestHelpers.EnsureLoadedAsync(row);
            await Task.Delay(200);
            panel.UpdateLayout();

            foreach (var text in new[] { "String cell", "Element cell", "Fixed cell" })
            {
                var block = Descendants<TextBlock>(row).FirstOrDefault(t => t.Text == text);
                Assert.IsNotNull(block, $"'{text}' is not in the visual tree.");
                Assert.IsTrue(block.ActualWidth > 0, $"'{text}' has no width.");
            }
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
