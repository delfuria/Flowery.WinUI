#if WINDOWS
using System.Numerics;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Windows.UI;

namespace Flowery.Controls
{
    /// <summary>
    /// Owns the native Composition shadow used when the owner cannot host XAML shadow layers.
    /// </summary>
    public sealed partial class DaisyNeumorphicHelper
    {
        /// <summary>
        /// When true, matches the DropShadow mask to the owner's rounded corners.
        /// </summary>
        public static bool UseRoundedShadowMask { get; set; } = true;

        private SpriteVisual? _darkShadowVisual;
        private DropShadow? _darkDropShadow;
        private ShapeVisual? _darkMaskVisual;
        private CompositionRoundedRectangleGeometry? _darkMaskGeometry;
        private CompositionSpriteShape? _darkMaskShape;
        private CompositionColorBrush? _darkMaskFill;
        private CompositionVisualSurface? _darkMaskSurface;
        private CompositionBrush? _darkMaskBrush;
        private bool _windowsShadowsAttached;

        partial void OnApplyDirectElevation(DaisyNeumorphicMode mode, double intensity, double elevation, ref bool handled)
        {
            if (!_owner.IsLoaded)
                return;

            try
            {
                ApplyWindowsDropShadows(mode, intensity, elevation);
                handled = true;
            }
            catch
            {
                DisposeWindowsShadowVisuals();
                handled = false;
            }
        }

        partial void OnDisposeWindowsShadows()
        {
            DisposeWindowsShadowVisuals();
        }

        private void ApplyWindowsDropShadows(DaisyNeumorphicMode mode, double intensity, double elevation)
        {
            _owner.SizeChanged -= OnOwnerSizeChangedWindows;
            _owner.SizeChanged += OnOwnerSizeChangedWindows;
            float width = (float)_owner.ActualWidth;
            float height = (float)_owner.ActualHeight;
            if (width <= 0 || height <= 0)
                return;

            var compositor = ElementCompositionPreview.GetElementVisual(_owner).Compositor;
            var finalElevation = GetDirectElevation(mode, intensity, elevation);
            var darkBaseColor = DaisyNeumorphic.GetDarkShadowColor(_owner)
                ?? DaisyBaseContentControl.GlobalNeumorphicDarkShadowColor;
            byte darkAlpha = (byte)Math.Clamp(160 * intensity, 60, 220);

            _darkShadowVisual ??= compositor.CreateSpriteVisual();
            _darkDropShadow ??= compositor.CreateDropShadow();
            _darkShadowVisual.Shadow = _darkDropShadow;
            _darkDropShadow.BlurRadius = (float)(finalElevation * 1.5);
            float offset = (float)(finalElevation * 0.6);
            _darkDropShadow.Offset = new Vector3(offset, offset, 0);
            _darkDropShadow.Color = Color.FromArgb(darkAlpha, darkBaseColor.R, darkBaseColor.G, darkBaseColor.B);
            _darkShadowVisual.Size = new Vector2(width, height);

            float cornerRadius = UseRoundedShadowMask ? GetOwnerCornerRadius() : 0;
            if (cornerRadius > 0)
            {
                ApplyRoundedMask(compositor, width, height, cornerRadius);
            }
            else
            {
                if (_darkMaskSurface is not null)
                    DisposeWindowsMask();
                _darkMaskBrush ??= compositor.CreateColorBrush(Microsoft.UI.Colors.White);
            }
            _darkDropShadow.Mask = _darkMaskBrush;

            if (!_windowsShadowsAttached)
            {
                ElementCompositionPreview.SetElementChildVisual(_owner, _darkShadowVisual);
                _windowsShadowsAttached = true;
            }

        }

        private float GetOwnerCornerRadius()
        {
            return _owner switch
            {
                Control control => (float)control.CornerRadius.TopLeft,
                Border border => (float)border.CornerRadius.TopLeft,
                Grid grid => (float)grid.CornerRadius.TopLeft,
                _ => 0
            };
        }

        private void ApplyRoundedMask(Compositor compositor, float width, float height, float cornerRadius)
        {
            if (_darkMaskSurface is null)
            {
                DisposeWindowsMask();
                _darkMaskVisual = compositor.CreateShapeVisual();
                _darkMaskGeometry = compositor.CreateRoundedRectangleGeometry();
                _darkMaskShape = compositor.CreateSpriteShape(_darkMaskGeometry);
                _darkMaskFill = compositor.CreateColorBrush(Microsoft.UI.Colors.White);
                _darkMaskShape.FillBrush = _darkMaskFill;
                _darkMaskVisual.Shapes.Add(_darkMaskShape);
                _darkMaskSurface = compositor.CreateVisualSurface();
                _darkMaskSurface.SourceVisual = _darkMaskVisual;
                _darkMaskBrush = compositor.CreateSurfaceBrush(_darkMaskSurface);
            }

            UpdateMaskSize(width, height, cornerRadius);
        }

        private void UpdateMaskSize(float width, float height, float cornerRadius)
        {
            var size = new Vector2(width, height);
            if (_darkMaskVisual is not null)
                _darkMaskVisual.Size = size;
            if (_darkMaskGeometry is not null)
            {
                _darkMaskGeometry.Size = size;
                _darkMaskGeometry.CornerRadius = new Vector2(cornerRadius);
            }
            if (_darkMaskSurface is not null)
                _darkMaskSurface.SourceSize = size;
        }

        private void OnOwnerSizeChangedWindows(object sender, SizeChangedEventArgs e)
        {
            float width = (float)e.NewSize.Width;
            float height = (float)e.NewSize.Height;
            if (width <= 0 || height <= 0)
                return;

            if (_darkShadowVisual is null && _owner.IsLoaded && _lastEnabled &&
                _currentMode is DaisyNeumorphicMode.Raised or DaisyNeumorphicMode.Flat)
            {
                ApplyWindowsDropShadows(_currentMode, _lastIntensity, _lastElevation);
                return;
            }
            if (_darkShadowVisual is not null)
                _darkShadowVisual.Size = new Vector2(width, height);
            UpdateMaskSize(width, height, GetOwnerCornerRadius());
        }

        private void DisposeWindowsMask()
        {
            if (_darkDropShadow is not null && ReferenceEquals(_darkDropShadow.Mask, _darkMaskBrush))
                _darkDropShadow.Mask = null;
            if (_darkMaskBrush is CompositionSurfaceBrush surfaceBrush)
                surfaceBrush.Surface = null;
            if (_darkMaskSurface is not null)
                _darkMaskSurface.SourceVisual = null;
            _darkMaskVisual?.Shapes.Clear();

            _darkMaskBrush?.Dispose();
            _darkMaskSurface?.Dispose();
            _darkMaskShape?.Dispose();
            _darkMaskGeometry?.Dispose();
            _darkMaskFill?.Dispose();
            _darkMaskVisual?.Dispose();
            _darkMaskBrush = null;
            _darkMaskSurface = null;
            _darkMaskShape = null;
            _darkMaskGeometry = null;
            _darkMaskFill = null;
            _darkMaskVisual = null;
        }

        private void DisposeWindowsShadowVisuals()
        {
            _owner.SizeChanged -= OnOwnerSizeChangedWindows;
            if (_windowsShadowsAttached &&
                ReferenceEquals(ElementCompositionPreview.GetElementChildVisual(_owner), _darkShadowVisual))
            {
                ElementCompositionPreview.SetElementChildVisual(_owner, null);
            }
            _windowsShadowsAttached = false;

            DisposeWindowsMask();
            if (_darkShadowVisual is not null)
                _darkShadowVisual.Shadow = null;
            _darkDropShadow?.Dispose();
            _darkShadowVisual?.Dispose();
            _darkDropShadow = null;
            _darkShadowVisual = null;
        }
    }
}
#endif
