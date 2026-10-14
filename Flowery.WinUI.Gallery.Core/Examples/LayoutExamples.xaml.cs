using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Flowery.WinUI.Gallery.Examples
{
    public sealed partial class LayoutExamples : ScrollableExamplePage
    {
        public LayoutExamples()
        {
            InitializeComponent();
            LoadDeferredSections();
#if WINDOWS
            if (DemoDrawer != null)
            {
                DemoDrawer.LightDismissOverlayMode = LightDismissOverlayMode.Off;
            }
#endif
        }

        protected override ScrollViewer? ScrollViewer => MainScrollViewer;

        private void LoadDeferredSections()
        {
            TryLoadSection(nameof(SectionDrawer), "drawer");
            TryLoadSection(nameof(SectionHero), "hero");
            TryLoadSection(nameof(SectionIndicator), "indicator");
            TryLoadSection(nameof(SectionJoin), "join");
            TryLoadSection(nameof(SectionMask), "mask");
            TryLoadSection(nameof(SectionMockup), "mockup");
            TryLoadSection(nameof(SectionStack), "stack");
        }

        private void TryLoadSection(string elementName, string sectionId)
        {
            try
            {
                var element = FindName(elementName);
                if (element == null)
                {
                    GalleryDiagnostics.Log($"{DateTimeOffset.Now:O} [LayoutExamples] Section '{sectionId}' returned null.");
                }
            }
            catch (Exception ex)
            {
                var message = $"{DateTimeOffset.Now:O} [LayoutExamples] Section '{sectionId}' failed: {ex}";
                GalleryDiagnostics.Log(message);
            }
        }

        private void DrawerToggle_Click(object sender, RoutedEventArgs e)
        {
            _ = sender;
            _ = e;

            DemoDrawer?.Toggle();
        }

        private void DrawerClose_Click(object sender, RoutedEventArgs e)
        {
            _ = sender;
            _ = e;

            DemoDrawer?.Close();
        }
    }
}
