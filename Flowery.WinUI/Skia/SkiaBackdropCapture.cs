using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Flowery.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using SkiaSharp;
using Windows.Storage.Streams;

namespace Flowery.Skia
{
    /// <summary>
    /// Captures backdrop content behind a UI element and processes it with SkiaSharp.
    /// Provides the bridge between WinUI rendering and Skia image processing.
    /// </summary>
    public static class SkiaBackdropCapture
    {
        private static readonly SemaphoreSlim CaptureGate = new(1, 1);

        /// <summary>
        /// Captures the area behind an element and applies glass effect.
        /// </summary>
        /// <param name="element">The element to capture behind.</param>
        /// <param name="blurRadius">Blur amount (0-100, will be converted to sigma).</param>
        /// <param name="saturation">Saturation level (0.0 = grayscale, 1.0 = normal).</param>
        /// <param name="tintColor">Tint overlay color.</param>
        /// <param name="tintOpacity">Tint opacity (0.0 - 1.0).</param>
        /// <param name="cornerRadius">Corner radius for the glass effect.</param>
        /// <returns>An ImageSource containing the glass effect, or null if capture failed.</returns>
        public static async Task<ImageSource?> CaptureAndApplyGlassEffectAsync(
            FrameworkElement element,
            double blurRadius,
            double saturation,
            Windows.UI.Color tintColor,
            double tintOpacity,
            double cornerRadius = 0)
        {
            try
            {
                (SKBitmap Bitmap, double ScaleX, double ScaleY)? snapshot;
                Windows.Foundation.Point topLeft;
                double width;
                double height;
                await CaptureGate.WaitAsync();
                try
                {
                    if (!element.IsLoaded || element.ActualWidth <= 0 || element.ActualHeight <= 0)
                        return null;
                    var backgroundSource = FindBackgroundSource(element);
                    if (backgroundSource == null)
                        return null;
                    width = element.ActualWidth;
                    height = element.ActualHeight;
                    topLeft = element.TransformToVisual(backgroundSource).TransformPoint(new Windows.Foundation.Point(0, 0));

                    // All glass surfaces share the unfiltered scene. Serializing this phase
                    // prevents concurrent captures from restoring each other's overlays.
                    var overlays = new List<HiddenElement>();
                    try
                    {
                        HideGlassOverlays(backgroundSource, overlays);
                        if (!overlays.Exists(hidden => ReferenceEquals(hidden.Element, element)))
                            overlays.Add(new HiddenElement(element));
                        snapshot = await CaptureSnapshotAsync(backgroundSource);
                    }
                    finally
                    {
                        foreach (var overlay in overlays)
                            overlay.Restore();
                    }
                }
                finally
                {
                    CaptureGate.Release();
                }

                if (snapshot is not { } captured)
                    return null;
                using var bitmap = captured.Bitmap;
                int targetX = (int)Math.Round(topLeft.X * captured.ScaleX);
                int targetY = (int)Math.Round(topLeft.Y * captured.ScaleY);
                int targetWidth = (int)Math.Round(width * captured.ScaleX);
                int targetHeight = (int)Math.Round(height * captured.ScaleY);
                if (targetWidth <= 0 || targetHeight <= 0)
                    return null;

                float sigma = (float)(Math.Max(0, blurRadius) / 10 * captured.ScaleX);
                int padding = (int)Math.Ceiling(sigma * 3);
                var captureRect = new SKRectI(
                    Math.Max(0, targetX - padding), Math.Max(0, targetY - padding),
                    Math.Min(bitmap.Width, targetX + targetWidth + padding),
                    Math.Min(bitmap.Height, targetY + targetHeight + padding));
                if (captureRect.Width <= 0 || captureRect.Height <= 0)
                    return null;

                using var paddedBitmap = new SKBitmap();
                if (!bitmap.ExtractSubset(paddedBitmap, captureRect))
                    return null;
                var glassBytes = SkiaImageProcessor.ApplyGlassEffect(
                    paddedBitmap, sigma, (float)saturation,
                    new SKColor(tintColor.R, tintColor.G, tintColor.B),
                    (float)(Math.Clamp(tintOpacity, 0, 1) * tintColor.A / 255), 0);
                if (glassBytes == null)
                    return null;

                using var processed = SKBitmap.Decode(glassBytes);
                if (processed == null)
                    return null;
                using var surface = SKSurface.Create(new SKImageInfo(targetWidth, targetHeight));
                if (surface == null)
                    return null;
                var canvas = surface.Canvas;
                canvas.Clear(SKColors.Transparent);
                using var clip = new SKRoundRect(new SKRect(0, 0, targetWidth, targetHeight),
                    (float)(Math.Max(0, cornerRadius) * captured.ScaleX),
                    (float)(Math.Max(0, cornerRadius) * captured.ScaleY));
                canvas.ClipRoundRect(clip, SKClipOperation.Intersect, true);
                canvas.DrawBitmap(processed, captureRect.Left - targetX, captureRect.Top - targetY);
                using var image = surface.Snapshot();
                using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
                return encoded == null ? null : await BytesToImageSourceAsync(encoded.ToArray());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SkiaBackdropCapture] Error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Captures a region of a UI element as an SKBitmap.
        /// </summary>
        public static async Task<SKBitmap?> CaptureElementAsync(
            FrameworkElement element,
            double x,
            double y,
            double width,
            double height)
        {
            try
            {
                var snapshot = await CaptureSnapshotAsync(element);
                if (snapshot is not { } captured)
                    return null;
                using var fullBitmap = captured.Bitmap;
                var cropRect = new SKRectI(
                    Math.Max(0, (int)Math.Round(x * captured.ScaleX)),
                    Math.Max(0, (int)Math.Round(y * captured.ScaleY)),
                    Math.Min(fullBitmap.Width, (int)Math.Round((x + width) * captured.ScaleX)),
                    Math.Min(fullBitmap.Height, (int)Math.Round((y + height) * captured.ScaleY)));
                if (cropRect.Width <= 0 || cropRect.Height <= 0)
                    return null;
                var croppedBitmap = new SKBitmap();
                if (fullBitmap.ExtractSubset(croppedBitmap, cropRect))
                    return croppedBitmap;
                croppedBitmap.Dispose();
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SkiaBackdropCapture] CaptureElement error: {ex.Message}");
                return null;
            }
        }

        private static async Task<(SKBitmap Bitmap, double ScaleX, double ScaleY)?> CaptureSnapshotAsync(FrameworkElement element)
        {
            double width = element.ActualWidth;
            double height = element.ActualHeight;
            if (width <= 0 || height <= 0)
                return null;
            var renderTarget = new RenderTargetBitmap();
            // RenderAsync applies the XamlRoot rasterization scale itself.
            await renderTarget.RenderAsync(element, (int)Math.Ceiling(width), (int)Math.Ceiling(height));
            int pixelWidth = renderTarget.PixelWidth;
            int pixelHeight = renderTarget.PixelHeight;
            if (pixelWidth <= 0 || pixelHeight <= 0)
                return null;
            var pixels = (await renderTarget.GetPixelsAsync()).ToArray();
            if (pixels.Length != (long)pixelWidth * pixelHeight * 4)
                return null;
            var bitmap = SkiaImageProcessor.FromPixelData(pixels, pixelWidth, pixelHeight);
            return bitmap == null ? null : (bitmap, (double)pixelWidth / width, (double)pixelHeight / height);
        }

        private static void HideGlassOverlays(DependencyObject parent, List<HiddenElement> overlays)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is DaisyGlass glass)
                    overlays.Add(new HiddenElement(glass));
                else if (child is DaisyCard card && (card.IsGlass || card.CardStyle == DaisyCardStyle.Glass))
                    overlays.Add(new HiddenElement(card));
                else
                    HideGlassOverlays(child, overlays);
            }
        }

        private sealed class HiddenElement
        {
            public FrameworkElement Element { get; }
            private readonly object _localOpacity;
            private readonly Binding? _opacityBinding;

            public HiddenElement(FrameworkElement element)
            {
                Element = element;
                _localOpacity = element.ReadLocalValue(UIElement.OpacityProperty);
                _opacityBinding = element.GetBindingExpression(UIElement.OpacityProperty)?.ParentBinding;
                element.Opacity = 0;
            }

            public void Restore()
            {
                // Do not overwrite a new value supplied while RenderAsync yielded.
                if (Element.ReadLocalValue(UIElement.OpacityProperty) is not double current || current != 0)
                    return;
                if (_opacityBinding != null)
                    Element.SetBinding(UIElement.OpacityProperty, _opacityBinding);
                else if (_localOpacity == DependencyProperty.UnsetValue)
                    Element.ClearValue(UIElement.OpacityProperty);
                else
                    Element.SetValue(UIElement.OpacityProperty, _localOpacity);
            }
        }

        /// <summary>
        /// Finds the appropriate background source element (parent with background).
        /// </summary>
        private static FrameworkElement? FindBackgroundSource(FrameworkElement element)
        {
            DependencyObject? current = VisualTreeHelper.GetParent(element);

            while (current != null)
            {
                // Stop at Page level (top of visual tree)
                if (current is Page)
                    break;

                // Check for background on Border or Panel
                if (current is Border { Background: not null } border)
                    return border;

                if (current is Panel { Background: not null } panel)
                    return panel;

                // Stop if we've reached the root (no more parents)
                var parent = VisualTreeHelper.GetParent(current);
                if (parent == null)
                    break;

                current = parent;
            }

            // Fallback: immediate parent
            return VisualTreeHelper.GetParent(element) as FrameworkElement;
        }

        /// <summary>
        /// Converts PNG bytes to a WinUI ImageSource.
        /// </summary>
        private static async Task<ImageSource?> BytesToImageSourceAsync(byte[] pngBytes)
        {
            try
            {
                using var stream = new InMemoryRandomAccessStream();
                await stream.WriteAsync(pngBytes.AsBuffer());
                stream.Seek(0);

                var bitmapImage = new BitmapImage();
                await bitmapImage.SetSourceAsync(stream);

                return bitmapImage;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SkiaBackdropCapture] BytesToImageSource error: {ex.Message}");
                return null;
            }
        }
    }
}
