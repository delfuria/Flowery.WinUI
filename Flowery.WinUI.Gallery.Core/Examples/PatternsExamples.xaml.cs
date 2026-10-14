using Microsoft.UI.Xaml.Controls;

namespace Flowery.WinUI.Gallery.Examples
{
    public sealed partial class PatternsExamples : ScrollableExamplePage
    {
        public PatternsExamples()
        {
            InitializeComponent();
        }

        protected override ScrollViewer? ScrollViewer => MainScrollViewer;
    }
}
