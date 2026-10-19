using System.Collections.Specialized;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml.Media.Imaging;
using SkiaSharp;
using Flowery.Composition;

namespace Flowery.Controls;

/// <summary>
/// Renders outer shadows with Windows Composition and inset shadows with alpha masks.
/// Shadow measurements use XAML pixels; inset bitmaps follow the current rasterization scale.
/// </summary>
[TemplatePart(Name = "PART_OuterShadows", Type = typeof(Canvas))]
[TemplatePart(Name = "PART_InnerShadows", Type = typeof(Image))]
public sealed partial class DaisyShadowContainer : ContentControl
{
    public static readonly DependencyProperty ShadowsProperty = DependencyProperty.Register(
        nameof(Shadows), typeof(DaisyShadowCollection), typeof(DaisyShadowContainer),
        new PropertyMetadata(null, OnShadowsChanged));

    private readonly List<DaisyShadow> _observedShadows = [];
    private readonly List<OuterShadowVisual> _outerShadows = [];
    private DaisyShadowCollection? _observedCollection;
    private Canvas? _outerHost;
    private Image? _innerImage;
    private CompositionVisualScope? _compositionScope;
    private CompositionElementTracker? _layoutTracker;
    private bool _updateQueued;

    public DaisyShadowContainer()
    {
        DefaultStyleKey = typeof(DaisyShadowContainer);
        Shadows = new DaisyShadowCollection();
        IsTabStop = false;
        HorizontalContentAlignment = HorizontalAlignment.Stretch;
        VerticalContentAlignment = VerticalAlignment.Stretch;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        RegisterPropertyChangedCallback(CornerRadiusProperty, OnSurfaceChanged);
        RegisterPropertyChangedCallback(BackgroundProperty, OnSurfaceChanged);
    }

    public DaisyShadowCollection Shadows
    {
        get => (DaisyShadowCollection)GetValue(ShadowsProperty);
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            SetValue(ShadowsProperty, value);
        }
    }

    protected override void OnApplyTemplate()
    {
        ReleaseVisuals();
        base.OnApplyTemplate();
        _outerHost = GetTemplateChild("PART_OuterShadows") as Canvas;
        _innerImage = GetTemplateChild("PART_InnerShadows") as Image;
        ObserveLayout();
        QueueUpdate();
    }

    private static void OnShadowsChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var owner = (DaisyShadowContainer)sender;
        owner.ObserveShadows();
        owner.QueueUpdate();
    }

    private void OnLoaded(object sender, RoutedEventArgs args)
    {
        ObserveShadows();
        ObserveLayout();
        QueueUpdate();
    }

    private void OnUnloaded(object sender, RoutedEventArgs args)
    {
        StopObservingShadows();
        _layoutTracker?.Dispose();
        _layoutTracker = null;
        ReleaseVisuals();
    }

    private void ObserveLayout()
    {
        _layoutTracker?.Dispose();
        _layoutTracker = IsLoaded && _outerHost is not null
            ? new CompositionElementTracker(this, _outerHost, OnLayoutChanged)
            : null;
    }

    private void OnLayoutChanged(CompositionElementBounds? bounds) => UpdateShadows();
    private void OnSurfaceChanged(DependencyObject sender, DependencyProperty property) => QueueUpdate();
    private void OnShadowChanged(object? sender, EventArgs args) => QueueUpdate();

    private void ObserveShadows()
    {
        StopObservingShadows();
        if (!IsLoaded)
            return;
        _observedCollection = Shadows;
        _observedCollection.CollectionChanged += OnCollectionChanged;
        foreach (var shadow in _observedCollection)
        {
            shadow.Changed += OnShadowChanged;
            _observedShadows.Add(shadow);
        }
    }

    private void StopObservingShadows()
    {
        if (_observedCollection != null)
            _observedCollection.CollectionChanged -= OnCollectionChanged;
        _observedCollection = null;
        foreach (var shadow in _observedShadows)
            shadow.Changed -= OnShadowChanged;
        _observedShadows.Clear();
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs args)
    {
        ObserveShadows();
        QueueUpdate();
    }

    private void QueueUpdate()
    {
        if (!IsLoaded || _updateQueued)
            return;
        _updateQueued = true;
        if (!DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, UpdateShadows))
            _updateQueued = false;
    }

    private void UpdateShadows()
    {
        _updateQueued = false;
        if (!IsLoaded || _outerHost == null || _innerImage == null)
            return;
        var width = (float)ActualWidth;
        var height = (float)ActualHeight;
        if (width <= 0 || height <= 0 || Shadows.Count == 0)
        {
            ReleaseVisuals();
            return;
        }

        var descriptions = Shadows.Where(static shadow => !shadow.IsInner && shadow.Opacity > 0 && shadow.Color.A > 0).ToArray();
        if (_compositionScope is { IsAttached: false } || _outerShadows.Count != descriptions.Length)
            ReleaseVisuals();

        _compositionScope ??= CompositionVisualScope.TryAttach(_outerHost);
        if (_compositionScope is { } scope)
        {
            scope.Root.Size = new Vector2(width, height);
            while (_outerShadows.Count < descriptions.Length)
                _outerShadows.Add(new OuterShadowVisual(scope));
            for (var index = 0; index < descriptions.Length; index++)
                _outerShadows[index].Update(descriptions[index], width, height, CornerRadius);
        }

        _innerImage.Source = null;
        RenderInsetShadows(width, height);
    }

    private sealed class OuterShadowVisual
    {
        private readonly ContainerVisual _mask;
        private readonly CompositionVectorVisual[] _corners = new CompositionVectorVisual[4];
        private readonly InsetClip[] _clips = new InsetClip[4];
        private readonly CompositionVisualSurface _surface;
        private readonly DropShadow _shadow;
        private readonly SpriteVisual _visual;

        internal OuterShadowVisual(CompositionVisualScope scope)
        {
            var compositor = scope.Compositor;
            _mask = scope.Track(compositor.CreateContainerVisual());
            for (var corner = 0; corner < _corners.Length; corner++)
            {
                var vector = new CompositionVectorVisual(scope, compositor.CreateRoundedRectangleGeometry(), Colors.White);
                _corners[corner] = vector;
                _clips[corner] = scope.Track(compositor.CreateInsetClip());
                vector.Visual.Clip = _clips[corner];
                _mask.Children.InsertAtTop(vector.Visual);
            }
            _surface = scope.Track(compositor.CreateVisualSurface());
            _surface.SourceVisual = _mask;
            _shadow = scope.Track(compositor.CreateDropShadow());
            _shadow.Mask = scope.Track(compositor.CreateSurfaceBrush(_surface));
            _visual = scope.Track(compositor.CreateSpriteVisual());
            _visual.Shadow = _shadow;
            scope.Root.Children.InsertAtTop(_visual);
        }

        internal void Update(DaisyShadow description, float width, float height, CornerRadius corners)
        {
            var spread = (float)Math.Max(description.Spread, -Math.Min(width, height) / 2 + 0.01);
            var size = new Vector2(width + 2 * spread, height + 2 * spread);
            _mask.Size = size;
            Span<double> radii = [corners.TopLeft, corners.TopRight, corners.BottomRight, corners.BottomLeft];
            for (var corner = 0; corner < _corners.Length; corner++)
            {
                var vector = _corners[corner];
                vector.SetSize(size);
                var geometry = (CompositionRoundedRectangleGeometry)vector.Geometry;
                geometry.Size = size;
                geometry.CornerRadius = new Vector2((float)Math.Clamp(radii[corner] + spread, 0, Math.Min(size.X, size.Y) / 2));
                var clip = _clips[corner];
                var right = corner is 1 or 2;
                var bottom = corner is 2 or 3;
                clip.LeftInset = right ? size.X / 2 : 0;
                clip.TopInset = bottom ? size.Y / 2 : 0;
                clip.RightInset = right ? 0 : size.X / 2;
                clip.BottomInset = bottom ? 0 : size.Y / 2;
            }
            _surface.SourceSize = size;
            _shadow.BlurRadius = (float)Math.Max(0, description.BlurRadius);
            _shadow.Color = Color.FromArgb(255, description.Color.R, description.Color.G, description.Color.B);
            _shadow.Opacity = (float)(Math.Clamp(description.Opacity, 0, 1) * description.Color.A / 255);
            _shadow.Offset = new Vector3((float)description.OffsetX, (float)description.OffsetY, 0);
            _visual.Size = size;
            _visual.Offset = new Vector3(-spread, -spread, 0);
        }
    }

    private void RenderInsetShadows(float width, float height)
    {
        var innerShadows = Shadows.Where(static shadow => shadow.IsInner && shadow.Opacity > 0 && shadow.Color.A > 0).ToArray();
        if (innerShadows.Length == 0 || _innerImage == null)
            return;
        var scale = (float)(XamlRoot?.RasterizationScale ?? 1);
        var pixelWidth = Math.Max(1, (int)Math.Ceiling(width * scale));
        var pixelHeight = Math.Max(1, (int)Math.Ceiling(height * scale));
        using var bitmap = new SKBitmap(new SKImageInfo(pixelWidth, pixelHeight, SKColorType.Bgra8888, SKAlphaType.Premul));
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);
        canvas.Scale(scale);
        using var clip = CreateRoundedRect(new SKRect(0, 0, width, height), 0);
        canvas.ClipRoundRect(clip, SKClipOperation.Intersect, antialias: true);

        foreach (var shadow in innerShadows)
        {
            var blur = (float)Math.Max(0, shadow.BlurRadius);
            var extent = blur * 3 + (float)Math.Max(Math.Abs(shadow.OffsetX), Math.Abs(shadow.OffsetY)) + (float)Math.Abs(shadow.Spread) + 1;
            using var inverse = new SKPath { FillType = SKPathFillType.EvenOdd };
            inverse.AddRect(new SKRect(-extent, -extent, width + extent, height + extent));
            var spread = (float)Math.Min(shadow.Spread, Math.Min(width, height) / 2 - 0.01);
            using var hole = CreateRoundedRect(new SKRect(
                spread + (float)shadow.OffsetX, spread + (float)shadow.OffsetY,
                width - spread + (float)shadow.OffsetX, height - spread + (float)shadow.OffsetY), -spread);
            inverse.AddRoundRect(hole);
            using var filter = SKImageFilter.CreateBlur(blur / 2, blur / 2, SKShaderTileMode.Decal);
            using var paint = new SKPaint
            {
                IsAntialias = true,
                Color = new SKColor(shadow.Color.R, shadow.Color.G, shadow.Color.B,
                    (byte)Math.Clamp(shadow.Color.A * shadow.Opacity, 0, 255)),
                ImageFilter = filter
            };
            canvas.DrawPath(inverse, paint);
        }
        canvas.Flush();
        var image = new WriteableBitmap(pixelWidth, pixelHeight);
        using (var stream = image.PixelBuffer.AsStream())
            stream.Write(bitmap.Bytes);
        image.Invalidate();
        _innerImage.Source = image;
    }

    private SKRoundRect CreateRoundedRect(SKRect bounds, float spread)
    {
        var result = new SKRoundRect();
        var maximumRadius = Math.Max(0, Math.Min(bounds.Width, bounds.Height) / 2);
        var corners = new[] { CornerRadius.TopLeft, CornerRadius.TopRight, CornerRadius.BottomRight, CornerRadius.BottomLeft };
        var radii = new SKPoint[corners.Length];
        for (var index = 0; index < corners.Length; index++)
        {
            var radius = (float)Math.Clamp(corners[index] + spread, 0, maximumRadius);
            radii[index] = new SKPoint(radius, radius);
        }
        result.SetRectRadii(bounds, radii);
        return result;
    }

    private void ReleaseVisuals()
    {
        _compositionScope?.Dispose();
        _compositionScope = null;
        _outerShadows.Clear();
        if (_innerImage != null)
            _innerImage.Source = null;
    }
}
