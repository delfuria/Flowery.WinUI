using Flowery.WinUI.Photos.Services;
using Microsoft.UI.Dispatching;

namespace Flowery.WinUI.Photos.Views
{
    /// <summary>
    /// Viewer chrome layout: immersive full screen (taskbar and app title bar hidden), an auto-hiding
    /// toolbar and an optional status bar. Overlay bars float above the photo and fade out until the
    /// pointer approaches the top or bottom edge.
    /// </summary>
    public sealed partial class ViewerView
    {
        private const double ChromeRevealBand = 96;
        private bool _immersive;
        private bool _immersiveBeforeSlideshow;
        private DispatcherQueueTimer? _chromeHideTimer;
        private bool _chromeInitialized;

        public bool IsImmersive => _immersive;

        private bool ToolbarIsOverlay => _immersive || PhotosSettings.AutoHideToolbar;

        private bool StatusBarIsOverlay => _immersive;

        public void ToggleImmersive() => SetImmersive(!_immersive);

        public void SetImmersive(bool immersive)
        {
            if (_shell == null || _slideshow)
                return;

            _immersive = immersive;
            _shell.SetFullScreen(immersive);
            _shell.SetTitleBarVisible(!immersive);
            CanvasHost.Margin = immersive ? new Thickness(0) : new Thickness(8, 0, 8, 0);
            CanvasHost.CornerRadius = immersive ? new CornerRadius(0) : new CornerRadius(12);
            ApplyChromeLayout();
            UpdateFullScreenButton();
            ZoomToFit();
        }

        /// <summary>
        /// Places toolbar/status bar either in their own rows or as overlays above the photo.
        /// </summary>
        private void ApplyChromeLayout()
        {
            EnsureChromeInitialized();
            if (_slideshow)
                return;

            StatusBar.Visibility = PhotosSettings.ShowStatusBar ? Visibility.Visible : Visibility.Collapsed;

            var startRow = ToolbarIsOverlay ? 0 : 1;
            var endRow = StatusBarIsOverlay ? 2 : 1;
            Grid.SetRow(MainArea, startRow);
            Grid.SetRowSpan(MainArea, endRow - startRow + 1);

            Toolbar.Background = ToolbarIsOverlay ? CreateChromeBrush() : null;
            StatusBar.Background = StatusBarIsOverlay ? CreateChromeBrush() : null;

            var toolbarHeight = Toolbar.ActualHeight > 0 ? Toolbar.ActualHeight : 52;
            var statusHeight = StatusBar.Visibility == Visibility.Visible && StatusBar.ActualHeight > 0 ? StatusBar.ActualHeight : 0;
            // The edit panel stays usable between overlay bars.
            EditPanel.Margin = new Thickness(0, ToolbarIsOverlay ? toolbarHeight : 0, 8, StatusBarIsOverlay ? statusHeight + 4 : 0);

            if (ToolbarIsOverlay || StatusBarIsOverlay)
            {
                RevealChrome();
            }
            else
            {
                UiTimers.Cancel(_chromeHideTimer);
                _chromeHideTimer = null;
                SetChromeVisible(true);
            }
            Canvas.Invalidate();
        }

        private static Brush CreateChromeBrush()
        {
            var color = DaisyResourceLookup.GetBrush("DaisyBase200Brush") is SolidColorBrush b ? b.Color : Color.FromArgb(255, 30, 30, 30);
            return new SolidColorBrush(Color.FromArgb(225, color.R, color.G, color.B));
        }

        private void EnsureChromeInitialized()
        {
            if (_chromeInitialized)
                return;
            _chromeInitialized = true;
            Microsoft.UI.Xaml.Controls.Canvas.SetZIndex(Toolbar, 10);
            Microsoft.UI.Xaml.Controls.Canvas.SetZIndex(StatusBar, 10);
            Toolbar.OpacityTransition = new ScalarTransition { Duration = TimeSpan.FromMilliseconds(180) };
            StatusBar.OpacityTransition = new ScalarTransition { Duration = TimeSpan.FromMilliseconds(180) };
            RootGrid.PointerMoved += RootGrid_PointerMoved;
            Toolbar.SizeChanged += (_, _) =>
            {
                if (ToolbarIsOverlay)
                    EditPanel.Margin = new Thickness(0, Toolbar.ActualHeight, 8, EditPanel.Margin.Bottom);
            };
        }

        private void SetChromeVisible(bool visible)
        {
            var toolbarVisible = visible || !ToolbarIsOverlay;
            var statusVisible = visible || !StatusBarIsOverlay;
            Toolbar.Opacity = toolbarVisible ? 1 : 0;
            Toolbar.IsHitTestVisible = toolbarVisible;
            StatusBar.Opacity = statusVisible ? 1 : 0;
            StatusBar.IsHitTestVisible = statusVisible;
        }

        private void RevealChrome()
        {
            SetChromeVisible(true);
            UiTimers.Cancel(_chromeHideTimer);
            _chromeHideTimer = UiTimers.RunOnce(DispatcherQueue, TimeSpan.FromSeconds(2.5), HideChromeIfIdle);
        }

        private void HideChromeIfIdle()
        {
            _chromeHideTimer = null;
            if (!ToolbarIsOverlay && !StatusBarIsOverlay)
                return;
            // Keep the bars while one of their menus or flyouts is open.
            if (MoreMenu.IsOpen || SaveMenu.IsOpen || ZoomMenu.IsOpen || ViewFlyout.IsOpen)
            {
                RevealChrome();
                return;
            }
            SetChromeVisible(false);
        }

        private void RootGrid_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (!ToolbarIsOverlay && !StatusBarIsOverlay)
                return;
            var y = e.GetCurrentPoint(RootGrid).Position.Y;
            if ((ToolbarIsOverlay && y <= ChromeRevealBand) || (StatusBarIsOverlay && y >= RootGrid.ActualHeight - ChromeRevealBand))
                RevealChrome();
        }
    }
}
