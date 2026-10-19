using System;
using System.Numerics;
using Flowery.Controls;
using Flowery.Helpers;
using Flowery.Theming;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Controls;
using Windows.UI;

namespace Flowery.WinUI.Gallery.Examples
{
    public enum DaisyButtonTechMode
    {
        None,
        Elevation,
        DropShadow,
        ThemeShadow
    }

    public sealed partial class DaisyButtonTechDemo : DaisyButton
    {
        public static bool EnableDebug { get; set; } = true;

        public static readonly DependencyProperty TechModeProperty =
            DependencyProperty.Register(
                nameof(TechMode),
                typeof(DaisyButtonTechMode),
                typeof(DaisyButtonTechDemo),
                new PropertyMetadata(DaisyButtonTechMode.None, OnTechModeChanged));

        private Border? _buttonBorder;
        private Canvas? _themeShadowReceiver;
        private SpriteVisual? _dropShadowVisual;
        private DropShadow? _dropShadow;
        private ShapeVisual? _dropShadowMaskVisual;
        private CompositionRoundedRectangleGeometry? _dropShadowMaskGeometry;
        private CompositionSpriteShape? _dropShadowMaskShape;
        private CompositionColorBrush? _dropShadowMaskFill;
        private CompositionVisualSurface? _dropShadowMaskSurface;
        private CompositionSurfaceBrush? _dropShadowMaskBrush;
        private ThemeShadow? _themeShadow;
        private bool _pendingApplyTech;

        public DaisyButtonTechMode TechMode
        {
            get => (DaisyButtonTechMode)GetValue(TechModeProperty);
            set => SetValue(TechModeProperty, value);
        }

        public DaisyButtonTechDemo()
        {
            DefaultStyleKey = typeof(DaisyButton);
            NeumorphicEnabled = false;
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            SizeChanged += OnSizeChanged;
        }

        protected override void OnApplyTemplate()
        {
            ClearTech();
            base.OnApplyTemplate();
            _buttonBorder = GetTemplateChild("ButtonBorder") as Border;
            _themeShadowReceiver = GetTemplateChild("PART_NeumorphicHost") as Canvas;
            _buttonBorder ??= FindChildByType<Border>(this);
            ApplyTech();
        }

        private static T? FindChildByType<T>(DependencyObject parent) where T : DependencyObject
        {
            var count = VisualTreeHelper.GetChildrenCount(parent);
            for (var i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild)
                    return typedChild;
                if (FindChildByType<T>(child) is { } result)
                    return result;
            }
            return default;
        }

        protected override bool ShouldSyncTemplateResources() => false;

        private static void OnTechModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyButtonTechDemo { IsLoaded: true } control)
                control.ApplyTech();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            DaisyThemeManager.ThemeChanged += OnGalleryThemeChanged;
            ApplyTech();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            DaisyThemeManager.ThemeChanged -= OnGalleryThemeChanged;
            ClearTech();
        }

        private void OnGalleryThemeChanged(object? sender, string themeName) => ApplyTech();

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!IsLoaded || ActualWidth <= 0 || ActualHeight <= 0)
                return;

            if (_pendingApplyTech)
                ApplyTech();
            else
                UpdateDropShadowSize();
        }

        private void OnLayoutUpdated(object? sender, object e)
        {
            if (IsLoaded && ActualWidth > 0 && ActualHeight > 0 && _pendingApplyTech)
                ApplyTech();
        }

        private void ApplyTech()
        {
            if (DaisyResourceLookup.IsHighContrast)
            {
                ClearTech();
                return;
            }

            if (!IsLoaded || _buttonBorder is null || ActualWidth <= 0 || ActualHeight <= 0)
            {
                _pendingApplyTech = true;
                if (IsLoaded)
                {
                    LayoutUpdated -= OnLayoutUpdated;
                    LayoutUpdated += OnLayoutUpdated;
                }
                return;
            }

            ClearTech();
            _buttonBorder.CornerRadius = CornerRadius;
            switch (TechMode)
            {
                case DaisyButtonTechMode.Elevation:
                    _themeShadow = new ThemeShadow();
                    _buttonBorder.Shadow = _themeShadow;
                    _buttonBorder.Translation = new Vector3(0, 0, 6);
                    break;
                case DaisyButtonTechMode.DropShadow:
                    ApplyDropShadow();
                    break;
                case DaisyButtonTechMode.ThemeShadow:
                    ApplyThemeShadow();
                    break;
            }
        }

        private void ClearTech()
        {
            _pendingApplyTech = false;
            LayoutUpdated -= OnLayoutUpdated;
            if (_buttonBorder is not null)
            {
                if (_themeShadow is not null && ReferenceEquals(_buttonBorder.Shadow, _themeShadow))
                {
                    _buttonBorder.Shadow = null;
                    _buttonBorder.Translation = new Vector3(_buttonBorder.Translation.X, _buttonBorder.Translation.Y, 0);
                }
                if (_dropShadowVisual is not null &&
                    ReferenceEquals(ElementCompositionPreview.GetElementChildVisual(_buttonBorder), _dropShadowVisual))
                {
                    ElementCompositionPreview.SetElementChildVisual(_buttonBorder, null);
                }
            }

            _themeShadow?.Receivers.Clear();
            _themeShadow = null;
            if (_dropShadowVisual is not null)
                _dropShadowVisual.Shadow = null;
            if (_dropShadow is not null)
                _dropShadow.Mask = null;
            if (_dropShadowMaskBrush is not null)
                _dropShadowMaskBrush.Surface = null;
            if (_dropShadowMaskSurface is not null)
                _dropShadowMaskSurface.SourceVisual = null;
            _dropShadowMaskVisual?.Shapes.Clear();

            _dropShadow?.Dispose();
            _dropShadowVisual?.Dispose();
            _dropShadowMaskBrush?.Dispose();
            _dropShadowMaskSurface?.Dispose();
            _dropShadowMaskShape?.Dispose();
            _dropShadowMaskGeometry?.Dispose();
            _dropShadowMaskFill?.Dispose();
            _dropShadowMaskVisual?.Dispose();
            _dropShadow = null;
            _dropShadowVisual = null;
            _dropShadowMaskBrush = null;
            _dropShadowMaskSurface = null;
            _dropShadowMaskShape = null;
            _dropShadowMaskGeometry = null;
            _dropShadowMaskFill = null;
            _dropShadowMaskVisual = null;
        }

        private static void Log(string message)
        {
            if (EnableDebug)
                FloweryDiagnostics.Log($"[DaisyButtonTechDemo] {message}");
        }

        private void ApplyDropShadow()
        {
            if (_buttonBorder is null)
                return;

            try
            {
                var compositor = ElementCompositionPreview.GetElementVisual(_buttonBorder).Compositor;
                _dropShadowVisual = compositor.CreateSpriteVisual();
                _dropShadow = compositor.CreateDropShadow();
                _dropShadow.BlurRadius = 12f;
                _dropShadow.Offset = new Vector3(3f, 3f, 0f);
                _dropShadow.Color = Color.FromArgb(140, 0, 0, 0);
                _dropShadowVisual.Shadow = _dropShadow;

                _dropShadowMaskVisual = compositor.CreateShapeVisual();
                _dropShadowMaskGeometry = compositor.CreateRoundedRectangleGeometry();
                _dropShadowMaskShape = compositor.CreateSpriteShape(_dropShadowMaskGeometry);
                _dropShadowMaskFill = compositor.CreateColorBrush(Microsoft.UI.Colors.White);
                _dropShadowMaskShape.FillBrush = _dropShadowMaskFill;
                _dropShadowMaskVisual.Shapes.Add(_dropShadowMaskShape);
                _dropShadowMaskSurface = compositor.CreateVisualSurface();
                _dropShadowMaskSurface.SourceVisual = _dropShadowMaskVisual;
                _dropShadowMaskBrush = compositor.CreateSurfaceBrush(_dropShadowMaskSurface);
                _dropShadow.Mask = _dropShadowMaskBrush;
                UpdateDropShadowSize();
                ElementCompositionPreview.SetElementChildVisual(_buttonBorder, _dropShadowVisual);
            }
            catch (Exception ex)
            {
                ClearTech();
                Log($"ApplyDropShadow Failed: {ex.Message}");
            }
        }

        private void UpdateDropShadowSize()
        {
            var size = new Vector2((float)ActualWidth, (float)ActualHeight);
            if (_dropShadowVisual is not null)
                _dropShadowVisual.Size = size;
            if (_dropShadowMaskVisual is not null)
                _dropShadowMaskVisual.Size = size;
            if (_dropShadowMaskGeometry is not null)
            {
                _dropShadowMaskGeometry.Size = size;
                _dropShadowMaskGeometry.CornerRadius = new Vector2((float)CornerRadius.TopLeft);
            }
            if (_dropShadowMaskSurface is not null)
                _dropShadowMaskSurface.SourceSize = size;
        }

        private void ApplyThemeShadow()
        {
            if (_buttonBorder is null || _themeShadowReceiver is null)
                return;

            try
            {
                _themeShadow = new ThemeShadow();
                _themeShadow.Receivers.Add(_themeShadowReceiver);
                _buttonBorder.Shadow = _themeShadow;
                _buttonBorder.Translation = new Vector3(0, 0, 6f);
            }
            catch (Exception ex)
            {
                ClearTech();
                Log($"ApplyThemeShadow Failed: {ex.Message}");
            }
        }
    }
}
