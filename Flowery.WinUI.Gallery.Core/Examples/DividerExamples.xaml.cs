using Microsoft.UI.Xaml.Controls;

namespace Flowery.WinUI.Gallery.Examples
{
    public sealed partial class DividerExamples : ScrollableExamplePage
    {
        public DividerExamples()
        {
            InitializeComponent();
        }

        protected override ScrollViewer? ScrollViewer => MainScrollViewer;
    }
}
