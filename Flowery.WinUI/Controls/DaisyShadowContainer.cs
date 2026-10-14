using System.Collections.Specialized;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml.Media.Imaging;
using SkiaSharp;

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
    private readonly List<IDisposable> _compositionResources = [];
    private DaisyShadowCollection? _observedCollection;
    private Canvas? _outerHost;
    private Image? _innerImage;
    private ContainerVisual? _outerVisual;
    private XamlRoot? _observedRoot;
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
        SizeChanged += OnSizeChanged;
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
        if (_observedRoot != null)
            _observedRoot.Changed -= OnRootChanged;
        _observedRoot = XamlRoot;
        if (_observedRoot != null)
            _observedRoot.Changed += OnRootChanged;
        QueueUpdate();
    }

    private void OnUnloaded(object sender, RoutedEventArgs args)
    {
        StopObservingShadows();
        if (_observedRoot != null)
            _observedRoot.Changed -= OnRootChanged;
        _observedRoot = null;
        ReleaseVisuals();
    }

    private void OnRootChanged(XamlRoot sender, XamlRootChangedEventArgs args) => QueueUpdate();
    private void OnSizeChanged(object sender, SizeChangedEventArgs args) => QueueUpdate();
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
        ReleaseVisuals();
        var width = (float)ActualWidth;
        var height = (float)ActualHeight;
        if (width <= 0 || height <= 0 || Shadows.Count == 0)
            return;

        var compositor = ElementCompositionPreview.GetElementVisual(_outerHost).Compositor;
        _outerVisual = Track(compositor.CreateContainerVisual());
        _outerVisual.Size = new Vector2(width, height);
        foreach (var shadow in Shadows)
        {
            if (!shadow.IsInner && shadow.Opacity > 0 && shadow.Color.A > 0)
                AddOuterShadow(compositor, shadow, width, height);
        }
        ElementCompositionPreview.SetElementChildVisual(_outerHost, _outerVisual);
        RenderInsetShadows(width, height);
    }

    private T Track<T>(T value) where T : IDisposable
    {
        _compositionResources.Add(value);
        return value;
    }

    private void AddOuterShadow(Compositor compositor, DaisyShadow description, float width, float height)
    {
        var spread = (float)Math.Max(description.Spread, -Math.Min(width, height) / 2 + 0.01);
        var maskWidth = width + 2 * spread;
        var maskHeight = height + 2 * spread;
        var mask = Track(compositor.CreateContainerVisual());
        mask.Size = new Vector2(maskWidth, maskHeight);
        var radii = new[] { CornerRadius.TopLeft, CornerRadius.TopRight, CornerRadius.BottomRight, CornerRadius.BottomLeft };
        for (var corner = 0; corner < radii.Length; corner++)
        {
            var shapeVisual = Track(compositor.CreateShapeVisual());
            shapeVisual.Size = mask.Size;
            var geometry = Track(compositor.CreateRoundedRectangleGeometry());
            geometry.Size = mask.Size;
            var radius = (float)Math.Clamp(radii[corner] + spread, 0, Math.Min(maskWidth, maskHeight) / 2);
            geometry.CornerRadius = new Vector2(radius);
            var shape = Track(compositor.CreateSpriteShape(geometry));
            shape.FillBrush = Track(compositor.CreateColorBrush(Colors.White));
            shapeVisual.Shapes.Add(shape);
            var right = corner is 1 or 2;
            var bottom = corner is 2 or 3;
            shapeVisual.Clip = Track(compositor.CreateInsetClip(
                right ? maskWidth / 2 : 0,
                bottom ? maskHeight / 2 : 0,
                right ? 0 : maskWidth / 2,
                bottom ? 0 : maskHeight / 2));
            mask.Children.InsertAtTop(shapeVisual);
        }

        var surface = Track(compositor.CreateVisualSurface());
        surface.SourceVisual = mask;
        surface.SourceSize = mask.Size;
        var dropShadow = Track(compositor.CreateDropShadow());
        dropShadow.Mask = Track(compositor.CreateSurfaceBrush(surface));
        dropShadow.BlurRadius = (float)Math.Max(0, description.BlurRadius);
        dropShadow.Color = Color.FromArgb(255, description.Color.R, description.Color.G, description.Color.B);
        dropShadow.Opacity = (float)(Math.Clamp(description.Opacity, 0, 1) * description.Color.A / 255);
        dropShadow.Offset = new Vector3((float)description.OffsetX, (float)description.OffsetY, 0);
        var visual = Track(compositor.CreateSpriteVisual());
        visual.Size = mask.Size;
        visual.Offset = new Vector3(-spread, -spread, 0);
        visual.Shadow = dropShadow;
        _outerVisual!.Children.InsertAtTop(visual);
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
        if (_outerHost != null && _outerVisual != null)
            ElementCompositionPreview.SetElementChildVisual(_outerHost, null);
        _outerVisual = null;
        if (_innerImage != null)
            _innerImage.Source = null;
        for (var index = _compositionResources.Count - 1; index >= 0; index--)
            _compositionResources[index].Dispose();
        _compositionResources.Clear();
    }
}
