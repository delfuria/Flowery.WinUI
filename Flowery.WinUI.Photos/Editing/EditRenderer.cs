using System.IO;
using System.Threading.Tasks;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.Geometry;
using Windows.Graphics.Imaging;

namespace Flowery.WinUI.Photos.Editing
{
    /// <summary>
    /// Builds the Win2D effect graph for an <see cref="EditState"/>. All edits are GPU accelerated
    /// and resolution independent (normalized geometry), so the same state renders the live preview
    /// from a downscaled bitmap and the final export from the full resolution bitmap.
    /// </summary>
    public static class EditRenderer
    {
        public const int PreviewMaxEdge = 2560;

        public static Size GetTransformedSize(Size source, EditState state) =>
            state.Rotation % 2 == 1 ? new Size(source.Height, source.Width) : source;

        public static Size GetOutputSize(Size source, EditState state)
        {
            var t = GetTransformedSize(source, state);
            return new Size(Math.Max(1, Math.Round(t.Width * state.Crop.Width)), Math.Max(1, Math.Round(t.Height * state.Crop.Height)));
        }

        /// <summary>
        /// Rotation, flip, straighten (with fill zoom) and optionally crop. The result starts at (0,0).
        /// </summary>
        public static ICanvasImage BuildGeometry(ICanvasImage source, Size sourceSize, EditState state, bool applyCrop, CanvasImageInterpolation interpolation)
        {
            if (!state.HasGeometry)
                return source;

            var t = GetTransformedSize(sourceSize, state);
            var tw = (float)t.Width;
            var th = (float)t.Height;

            var angle = (float)((state.Rotation % 4) * Math.PI / 2 + state.Straighten * Math.PI / 180);
            var theta = Math.Abs(state.Straighten) * Math.PI / 180;
            var fill = (float)(Math.Cos(theta) + Math.Max(tw / th, th / tw) * Math.Sin(theta));

            var matrix = Matrix3x2.CreateTranslation(-(float)sourceSize.Width / 2, -(float)sourceSize.Height / 2)
                * Matrix3x2.CreateScale(state.FlipH ? -1 : 1, state.FlipV ? -1 : 1)
                * Matrix3x2.CreateRotation(angle)
                * Matrix3x2.CreateScale(fill)
                * Matrix3x2.CreateTranslation(tw / 2, th / 2);

            var bounds = new Rect(0, 0, tw, th);
            if (applyCrop && state.Crop != EditState.FullCrop)
            {
                var cx = (float)(state.Crop.X * tw);
                var cy = (float)(state.Crop.Y * th);
                matrix *= Matrix3x2.CreateTranslation(-cx, -cy);
                bounds = new Rect(0, 0, Math.Max(1, state.Crop.Width * tw), Math.Max(1, state.Crop.Height * th));
            }

            var transform = new Transform2DEffect
            {
                Source = source,
                TransformMatrix = matrix,
                InterpolationMode = interpolation,
                BorderMode = EffectBorderMode.Hard
            };

            return new CropEffect
            {
                Source = transform,
                SourceRectangle = bounds,
                BorderMode = EffectBorderMode.Hard
            };
        }

        /// <summary>
        /// Tone, color, detail, filter and effects.
        /// </summary>
        public static ICanvasImage BuildAdjustments(ICanvasImage input, EditState s, Size size, bool includeFilter = true)
        {
            var image = input;

            if (s.Exposure != 0)
            {
                image = new ExposureEffect { Source = image, Exposure = (float)(s.Exposure / 100 * 1.5) };
            }

            if (s.Highlights != 0 || s.Shadows != 0 || s.Clarity != 0)
            {
                image = new HighlightsAndShadowsEffect
                {
                    Source = image,
                    Highlights = (float)Math.Clamp(s.Highlights / 100, -1, 1),
                    Shadows = (float)Math.Clamp(s.Shadows / 100, -1, 1),
                    Clarity = (float)Math.Clamp(s.Clarity / 100, -1, 1)
                };
            }

            if (s.Contrast != 0)
            {
                image = new ContrastEffect { Source = image, Contrast = (float)Math.Clamp(s.Contrast / 100 * 0.75, -1, 1) };
            }

            if (s.Warmth != 0 || s.Tint != 0)
            {
                image = new TemperatureAndTintEffect
                {
                    Source = image,
                    Temperature = (float)Math.Clamp(s.Warmth / 100 * 0.8, -1, 1),
                    Tint = (float)Math.Clamp(s.Tint / 100 * 0.8, -1, 1)
                };
            }

            // Brightness, saturation, fade and the filter look are folded into one color matrix.
            var matrix = ColorMatrix.Identity;
            var useMatrix = false;
            if (s.Brightness != 0)
            {
                matrix = matrix.Then(ColorMatrix.Brightness((float)(s.Brightness / 100 * 0.25)));
                useMatrix = true;
            }
            if (s.Saturation != 0)
            {
                matrix = matrix.Then(ColorMatrix.Saturation((float)(1 + s.Saturation / 100)));
                useMatrix = true;
            }
            if (includeFilter && s.Filter != FilterPresets.NoneId && s.FilterIntensity > 0)
            {
                var preset = FilterPresets.Get(s.Filter);
                matrix = matrix.Then(ColorMatrix.Identity.Lerp(preset.Matrix, (float)Math.Clamp(s.FilterIntensity / 100, 0, 1)));
                useMatrix = true;
            }
            if (s.Fade > 0)
            {
                matrix = matrix.Then(ColorMatrix.Fade((float)(s.Fade / 100)));
                useMatrix = true;
            }
            if (useMatrix)
            {
                image = new ColorMatrixEffect { Source = image, ColorMatrix = matrix.ToMatrix5x4(), ClampOutput = true };
            }

            if (s.Sharpness > 0)
            {
                image = new SharpenEffect { Source = image, Amount = (float)(s.Sharpness / 100 * 6), Threshold = 0 };
            }
            else if (s.Sharpness < 0)
            {
                var amount = (float)(-s.Sharpness / 100 * Math.Max(size.Width, size.Height) / 400);
                image = new GaussianBlurEffect { Source = image, BlurAmount = amount, BorderMode = EffectBorderMode.Hard, Optimization = EffectOptimization.Balanced };
            }

            if (s.Vignette != 0)
            {
                var strength = (float)Math.Clamp(Math.Abs(s.Vignette) / 100, 0, 1);
                image = new VignetteEffect
                {
                    Source = image,
                    Color = s.Vignette > 0 ? Color.FromArgb(255, 0, 0, 0) : Color.FromArgb(255, 255, 255, 255),
                    Amount = 0.15f + strength * 0.65f,
                    Curve = 0.35f + (1 - strength) * 0.4f
                };
            }

            return image;
        }

        /// <summary>
        /// Draws markup strokes. <paramref name="imageSize"/> is the size of the drawn image in the current
        /// drawing space; <paramref name="visible"/> is the normalized region of the transformed image that is drawn.
        /// </summary>
        public static void DrawStrokes(CanvasDrawingSession ds, IEnumerable<MarkupStroke> strokes, Size transformedSize, Rect visible, Size imageSize)
        {
            var scaleX = imageSize.Width / Math.Max(visible.Width, 1e-6);
            var scaleY = imageSize.Height / Math.Max(visible.Height, 1e-6);
            var refEdge = Math.Max(transformedSize.Width, transformedSize.Height) * (imageSize.Width / Math.Max(1, transformedSize.Width * visible.Width));

            using var style = new CanvasStrokeStyle
            {
                StartCap = CanvasCapStyle.Round,
                EndCap = CanvasCapStyle.Round,
                LineJoin = CanvasLineJoin.Round
            };

            foreach (var stroke in strokes)
            {
                if (stroke.Points.Length == 0)
                    continue;

                var width = (float)(stroke.Width * refEdge);
                var color = stroke.Highlighter ? Color.FromArgb(Math.Min((byte)110, stroke.Color.A), stroke.Color.R, stroke.Color.G, stroke.Color.B) : stroke.Color;

                Vector2 Map(Vector2 p) => new((float)((p.X - visible.X) * scaleX), (float)((p.Y - visible.Y) * scaleY));

                if (stroke.Points.Length == 1)
                {
                    ds.FillCircle(Map(stroke.Points[0]), width / 2, color);
                    continue;
                }

                using var builder = new CanvasPathBuilder(ds);
                builder.BeginFigure(Map(stroke.Points[0]));
                for (var i = 1; i < stroke.Points.Length; i++)
                {
                    // Quadratic smoothing through midpoints.
                    var p0 = Map(stroke.Points[i - 1]);
                    var p1 = Map(stroke.Points[i]);
                    builder.AddQuadraticBezier(p0, (p0 + p1) / 2);
                }
                builder.AddLine(Map(stroke.Points[^1]));
                builder.EndFigure(CanvasFigureLoop.Open);
                using var geometry = CanvasGeometry.CreatePath(builder);
                ds.DrawGeometry(geometry, color, Math.Max(1, width), style);
            }
        }

        /// <summary>
        /// Renders the final edited image at full resolution.
        /// </summary>
        public static CanvasRenderTarget RenderFinal(CanvasDevice device, CanvasBitmap source, EditState state)
        {
            var sourceSize = new Size(source.SizeInPixels.Width, source.SizeInPixels.Height);
            var output = GetOutputSize(sourceSize, state);
            var target = new CanvasRenderTarget(device, (float)output.Width, (float)output.Height, 96);

            using var ds = target.CreateDrawingSession();
            ds.Clear(Color.FromArgb(0, 0, 0, 0));
            var geometry = BuildGeometry(source, sourceSize, state, applyCrop: true, CanvasImageInterpolation.HighQualityCubic);
            ds.DrawImage(BuildAdjustments(geometry, state, output));
            if (!state.Strokes.IsEmpty)
            {
                DrawStrokes(ds, state.Strokes, GetTransformedSize(sourceSize, state), state.Crop, output);
            }
            return target;
        }

        public static CanvasBitmapFileFormat GetFormatForPath(string path) =>
            System.IO.Path.GetExtension(path).ToLowerInvariant() switch
            {
                ".png" => CanvasBitmapFileFormat.Png,
                ".bmp" => CanvasBitmapFileFormat.Bmp,
                ".gif" => CanvasBitmapFileFormat.Gif,
                ".tif" or ".tiff" => CanvasBitmapFileFormat.Tiff,
                ".jxr" or ".wdp" => CanvasBitmapFileFormat.JpegXR,
                _ => CanvasBitmapFileFormat.Jpeg
            };

        /// <summary>
        /// True if Win2D can write the format back; otherwise edits are saved as JPEG copies.
        /// </summary>
        public static bool CanWriteFormat(string path) =>
            System.IO.Path.GetExtension(path).ToLowerInvariant() is ".jpg" or ".jpeg" or ".jpe" or ".jfif" or ".png" or ".bmp" or ".gif" or ".tif" or ".tiff" or ".jxr" or ".wdp";

        /// <summary>
        /// Decodes a file with EXIF orientation applied. Bitmaps larger than the GPU limit are downscaled.
        /// </summary>
        public static async Task<CanvasBitmap> LoadOrientedAsync(CanvasDevice device, string path)
        {
            var bytes = await File.ReadAllBytesAsync(path).ConfigureAwait(false);
            using var stream = new Windows.Storage.Streams.InMemoryRandomAccessStream();
            using (var writer = new Windows.Storage.Streams.DataWriter(stream.GetOutputStreamAt(0)))
            {
                writer.WriteBytes(bytes);
                await writer.StoreAsync();
                writer.DetachStream();
            }
            stream.Seek(0);

            var decoder = await BitmapDecoder.CreateAsync(stream);
            var max = Math.Max(1024, device.MaximumBitmapSizeInPixels);
            var transform = new BitmapTransform();
            var longest = Math.Max(decoder.PixelWidth, decoder.PixelHeight);
            if (longest > max)
            {
                var scale = (double)max / longest;
                transform.ScaledWidth = (uint)Math.Max(1, decoder.PixelWidth * scale);
                transform.ScaledHeight = (uint)Math.Max(1, decoder.PixelHeight * scale);
                transform.InterpolationMode = BitmapInterpolationMode.Fant;
            }

            using var software = await decoder.GetSoftwareBitmapAsync(
                BitmapPixelFormat.Bgra8,
                BitmapAlphaMode.Premultiplied,
                transform,
                ExifOrientationMode.RespectExifOrientation,
                ColorManagementMode.ColorManageToSRgb);

            // The pipeline assumes 1 pixel == 1 DIP; files often carry 72/300 dpi metadata.
            software.DpiX = 96;
            software.DpiY = 96;
            return CanvasBitmap.CreateFromSoftwareBitmap(device, software);
        }

        /// <summary>
        /// Creates a downscaled copy for interactive preview, or returns the source if it is small enough.
        /// </summary>
        public static CanvasBitmap CreatePreview(CanvasDevice device, CanvasBitmap source, int maxEdge = PreviewMaxEdge)
        {
            var w = source.SizeInPixels.Width;
            var h = source.SizeInPixels.Height;
            var longest = Math.Max(w, h);
            if (longest <= maxEdge)
                return source;

            var scale = (double)maxEdge / longest;
            var pw = (float)Math.Max(1, Math.Round(w * scale));
            var ph = (float)Math.Max(1, Math.Round(h * scale));
            var target = new CanvasRenderTarget(device, pw, ph, 96);
            using var ds = target.CreateDrawingSession();
            ds.Clear(Color.FromArgb(0, 0, 0, 0));
            ds.DrawImage(source, new Rect(0, 0, pw, ph), new Rect(0, 0, w, h), 1f, CanvasImageInterpolation.HighQualityCubic);
            return target;
        }
    }
}
