using Flowery.WinUI.Photos.Services;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using static Flowery.WinUI.Photos.Views.UiHelpers;

namespace Flowery.WinUI.Photos.Views
{
    /// <summary>
    /// "View" options (like Paint's View menu): zoom, rulers, gridlines, status bar, auto-hiding toolbar,
    /// full screen and a navigator thumbnail (mini map) for panning zoomed photos.
    /// </summary>
    public sealed partial class ViewerView
    {
        private const float RulerSize = 22;
        private const float NavigatorMaxEdge = 170;
        private Point? _lastPointer;
        private bool _draggingNavigator;

        private bool RulersVisible => PhotosSettings.ShowRulers && !_slideshow;

        private float RulerInset => RulersVisible ? RulerSize : 0;

        private void ViewFlyout_Opening(object sender, object e) => BuildViewPanel();

        private void BuildViewPanel()
        {
            ViewPanel.Children.Clear();

            ViewPanel.Children.Add(Caption("Photos_View_Zoom", new Thickness(0)));
            var zoom = new Grid { ColumnSpacing = 6 };
            for (var i = 0; i < 4; i++)
                zoom.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            (string? Icon, Symbol? Symbol, string Text, Action Action)[] zoomButtons =
            [
                (null, Symbol.ZoomOut, L("Photos_ZoomOut"), () => StepZoom(-1)),
                ("PhotosIconFitScreen", null, L("Photos_ZoomFit"), ZoomToFit),
                (null, null, "100 %", () => SetZoomPercent(100)),
                (null, Symbol.ZoomIn, L("Photos_ZoomIn"), () => StepZoom(1))
            ];
            for (var i = 0; i < zoomButtons.Length; i++)
            {
                var (icon, symbol, text, action) = zoomButtons[i];
                var button = new DaisyButton
                {
                    Size = DaisySize.Small,
                    Variant = DaisyButtonVariant.Primary,
                    HorizontalAlignment = HorizontalAlignment.Stretch
                };
                if (symbol is { } s)
                    button.IconSymbol = s;
                else if (icon != null)
                    button.IconData = Icon(icon);
                else
                    button.Content = text;
                Tip(button, text);
                button.Click += (_, _) => action();
                Grid.SetColumn(button, i);
                zoom.Children.Add(button);
            }
            ViewPanel.Children.Add(zoom);

            ViewPanel.Children.Add(Caption("Photos_View_Show"));
            ViewPanel.Children.Add(ToggleRow("Photos_View_Rulers", PhotosSettings.ShowRulers, v =>
            {
                PhotosSettings.ShowRulers = v;
                PhotosSettings.SaveOptions();
                ClampPan();
                UpdateZoomUi();
                Canvas.Invalidate();
            }));
            ViewPanel.Children.Add(ToggleRow("Photos_View_Gridlines", PhotosSettings.ShowGrid, v =>
            {
                PhotosSettings.ShowGrid = v;
                PhotosSettings.SaveOptions();
                Canvas.Invalidate();
            }));
            ViewPanel.Children.Add(ToggleRow("Photos_View_StatusBar", PhotosSettings.ShowStatusBar, v =>
            {
                PhotosSettings.ShowStatusBar = v;
                PhotosSettings.SaveOptions();
                ApplyChromeLayout();
            }));
            ViewPanel.Children.Add(ToggleRow("Photos_View_Navigator", PhotosSettings.ShowNavigator, v =>
            {
                PhotosSettings.ShowNavigator = v;
                PhotosSettings.SaveOptions();
                Canvas.Invalidate();
            }));

            ViewPanel.Children.Add(Caption("Photos_View_Toolbar"));
            ViewPanel.Children.Add(ToggleRow("Photos_View_AutoHideToolbar", PhotosSettings.AutoHideToolbar, v =>
            {
                PhotosSettings.AutoHideToolbar = v;
                PhotosSettings.SaveOptions();
                ApplyChromeLayout();
            }));

            var fullScreen = new DaisyButton
            {
                Content = L(_immersive ? "Photos_ExitFullScreen" : "Photos_FullScreen"),
                IconSymbol = _immersive ? Symbol.BackToWindow : Symbol.FullScreen,
                Variant = DaisyButtonVariant.Primary,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(0, 4, 0, 0)
            };
            fullScreen.Click += (_, _) =>
            {
                ViewFlyout.Hide();
                ToggleImmersive();
            };
            ViewPanel.Children.Add(fullScreen);
        }

        #region Overlay drawing

        private static Color ThemeColor(string key, Color fallback) =>
            DaisyResourceLookup.GetBrush(key) is SolidColorBrush b ? b.Color : fallback;

        /// <summary>Picks a "nice" step (1, 2, 5 × 10^n) of at least <paramref name="minimum"/>.</summary>
        private static double NiceStep(double minimum)
        {
            if (minimum <= 0 || double.IsNaN(minimum) || double.IsInfinity(minimum))
                return 100;
            var magnitude = Math.Pow(10, Math.Floor(Math.Log10(minimum)));
            foreach (var factor in new[] { 1.0, 2.0, 5.0, 10.0 })
            {
                if (factor * magnitude >= minimum)
                    return factor * magnitude;
            }
            return 10 * magnitude;
        }

        /// <summary>DIPs per full-resolution pixel for the current transform.</summary>
        private double ScreenPerFullPixel(ViewTransform t) =>
            _full == null ? t.Scale : t.Scale * t.Source.SizeInPixels.Width / _full.SizeInPixels.Width;

        private void DrawGridlines(CanvasDrawingSession ds, ViewTransform t)
        {
            var r = t.ScreenRect;
            var ppx = ScreenPerFullPixel(t);
            var step = NiceStep(48 / ppx);
            var light = Color.FromArgb(70, 255, 255, 255);
            var dark = Color.FromArgb(55, 0, 0, 0);
            var left = Math.Max(r.X, RulerInset);
            var top = Math.Max(r.Y, RulerInset);
            var right = Math.Min(r.Right, Canvas.ActualWidth);
            var bottom = Math.Min(r.Bottom, Canvas.ActualHeight);

            for (var v = step; v * ppx < r.Width; v += step)
            {
                var x = (float)(r.X + v * ppx);
                if (x < left || x > right)
                    continue;
                ds.DrawLine(x + 0.5f, (float)top, x + 0.5f, (float)bottom, dark, 1);
                ds.DrawLine(x, (float)top, x, (float)bottom, light, 1);
            }
            for (var v = step; v * ppx < r.Height; v += step)
            {
                var y = (float)(r.Y + v * ppx);
                if (y < top || y > bottom)
                    continue;
                ds.DrawLine((float)left, y + 0.5f, (float)right, y + 0.5f, dark, 1);
                ds.DrawLine((float)left, y, (float)right, y, light, 1);
            }
        }

        private void DrawRulers(CanvasDrawingSession ds, ViewTransform t)
        {
            var width = (float)Canvas.ActualWidth;
            var height = (float)Canvas.ActualHeight;
            var background = ThemeColor("DaisyBase200Brush", Color.FromArgb(255, 30, 30, 30));
            var content = ThemeColor("DaisyBaseContentBrush", Color.FromArgb(255, 230, 230, 230));
            var tick = Color.FromArgb(150, content.R, content.G, content.B);
            var accent = ThemeColor("DaisyPrimaryBrush", Color.FromArgb(255, 99, 102, 241));

            ds.FillRectangle(0, 0, width, RulerSize, background);
            ds.FillRectangle(0, 0, RulerSize, height, background);
            ds.DrawLine(RulerSize, RulerSize - 0.5f, width, RulerSize - 0.5f, tick, 1);
            ds.DrawLine(RulerSize - 0.5f, RulerSize, RulerSize - 0.5f, height, tick, 1);

            var r = t.ScreenRect;
            var ppx = ScreenPerFullPixel(t);
            var major = NiceStep(70 / ppx);
            var minor = major / 5;
            using var format = new CanvasTextFormat { FontSize = 9.5f, WordWrapping = CanvasWordWrapping.NoWrap };

            // Horizontal ruler
            var startX = Math.Floor((RulerSize - r.X) / ppx / minor) * minor;
            for (var v = startX; r.X + v * ppx < width; v += minor)
            {
                var x = (float)(r.X + v * ppx);
                if (x < RulerSize)
                    continue;
                var isMajor = Math.Abs(v / major - Math.Round(v / major)) < 1e-6;
                ds.DrawLine(x, RulerSize - (isMajor ? 10 : 4), x, RulerSize, tick, 1);
                if (isMajor)
                    ds.DrawText(((long)Math.Round(v)).ToString(CultureInfo.InvariantCulture), x + 2, 1, content, format);
            }

            // Vertical ruler (labels rotated)
            var startY = Math.Floor((RulerSize - r.Y) / ppx / minor) * minor;
            for (var v = startY; r.Y + v * ppx < height; v += minor)
            {
                var y = (float)(r.Y + v * ppx);
                if (y < RulerSize)
                    continue;
                var isMajor = Math.Abs(v / major - Math.Round(v / major)) < 1e-6;
                ds.DrawLine(RulerSize - (isMajor ? 10 : 4), y, RulerSize, y, tick, 1);
                if (isMajor)
                {
                    var previous = ds.Transform;
                    ds.Transform = Matrix3x2.CreateRotation((float)(-Math.PI / 2), new Vector2(1, y - 2));
                    ds.DrawText(((long)Math.Round(v)).ToString(CultureInfo.InvariantCulture), 1, y - 2, content, format);
                    ds.Transform = previous;
                }
            }

            // Pointer position markers
            if (_lastPointer is { } p)
            {
                if (p.X >= RulerSize)
                    ds.DrawLine((float)p.X, 0, (float)p.X, RulerSize, accent, 1.5f);
                if (p.Y >= RulerSize)
                    ds.DrawLine(0, (float)p.Y, RulerSize, (float)p.Y, accent, 1.5f);
            }

            ds.FillRectangle(0, 0, RulerSize, RulerSize, background);
        }

        /// <summary>
        /// Navigator thumbnail rect (bottom right) and the viewport inside it, in screen coordinates.
        /// </summary>
        private (Rect Thumb, Rect Viewport)? GetNavigatorRects(ViewTransform t)
        {
            if (!PhotosSettings.ShowNavigator || IsCropMode || _slideshow)
                return null;
            var aspect = t.ImageSize.Width / t.ImageSize.Height;
            var w = aspect >= 1 ? NavigatorMaxEdge : NavigatorMaxEdge * aspect;
            var h = aspect >= 1 ? NavigatorMaxEdge / aspect : NavigatorMaxEdge;
            var margin = 14;
            var thumb = new Rect(Canvas.ActualWidth - w - margin, Canvas.ActualHeight - h - margin, w, h);

            var r = t.ScreenRect;
            var vx = (Math.Max(RulerInset, 0) - r.X) / r.Width;
            var vy = (Math.Max(RulerInset, 0) - r.Y) / r.Height;
            var vw = (Canvas.ActualWidth - RulerInset) / r.Width;
            var vh = (Canvas.ActualHeight - RulerInset) / r.Height;
            var x0 = Math.Clamp(vx, 0, 1);
            var y0 = Math.Clamp(vy, 0, 1);
            var x1 = Math.Clamp(vx + vw, 0, 1);
            var y1 = Math.Clamp(vy + vh, 0, 1);
            var viewport = new Rect(thumb.X + x0 * w, thumb.Y + y0 * h, Math.Max(2, (x1 - x0) * w), Math.Max(2, (y1 - y0) * h));
            return (thumb, viewport);
        }

        private void DrawNavigator(CanvasDrawingSession ds, ViewTransform t, ICanvasImage image)
        {
            if (GetNavigatorRects(t) is not { } rects)
                return;
            var (thumb, viewport) = rects;
            var background = ThemeColor("DaisyBase200Brush", Color.FromArgb(255, 30, 30, 30));
            var accent = ThemeColor("DaisyPrimaryBrush", Color.FromArgb(255, 99, 102, 241));

            var frame = new Rect(thumb.X - 4, thumb.Y - 4, thumb.Width + 8, thumb.Height + 8);
            ds.FillRoundedRectangle(frame, 8, 8, Color.FromArgb(235, background.R, background.G, background.B));
            ds.DrawImage(image, thumb, new Rect(0, 0, t.ImageSize.Width, t.ImageSize.Height), 1f, CanvasImageInterpolation.Linear);

            // Dim everything outside the visible viewport.
            var shade = Color.FromArgb(110, 0, 0, 0);
            ds.FillRectangle(new Rect(thumb.X, thumb.Y, thumb.Width, Math.Max(0, viewport.Y - thumb.Y)), shade);
            ds.FillRectangle(new Rect(thumb.X, viewport.Bottom, thumb.Width, Math.Max(0, thumb.Bottom - viewport.Bottom)), shade);
            ds.FillRectangle(new Rect(thumb.X, viewport.Y, Math.Max(0, viewport.X - thumb.X), viewport.Height), shade);
            ds.FillRectangle(new Rect(viewport.Right, viewport.Y, Math.Max(0, thumb.Right - viewport.Right), viewport.Height), shade);
            ds.DrawRectangle(viewport, accent, 2);
        }

        private bool IsInNavigator(ViewTransform t, Point p) =>
            GetNavigatorRects(t) is { } rects && rects.Thumb.Contains(p);

        /// <summary>Centers the view on the image point under a navigator position.</summary>
        private void PanFromNavigator(ViewTransform t, Point p)
        {
            if (GetNavigatorRects(t) is not { } rects)
                return;
            if (_zoom == null)
                _zoom = GetEffectiveZoom();
            var nx = Math.Clamp((p.X - rects.Thumb.X) / rects.Thumb.Width, 0, 1);
            var ny = Math.Clamp((p.Y - rects.Thumb.Y) / rects.Thumb.Height, 0, 1);
            var r = t.ScreenRect;
            // Desired: image point (nx, ny) at the center of the view area; pan shifts the image center.
            _pan = new Vector2((float)((0.5 - nx) * r.Width), (float)((0.5 - ny) * r.Height));
            ClampPan();
            UpdateZoomUi();
            Canvas.Invalidate();
        }

        #endregion
    }
}
