using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Flowery.Controls;

/// <summary>Retained XAML vectors in the tour overlay's DIP coordinate space.</summary>
internal sealed partial class DaisyTourAnnotationPresenter : Canvas
{
    private readonly Path _body = CreatePath();
    private readonly Path _tail = CreatePath();
    private readonly Path _startHead = CreatePath();
    private readonly Path _endHead = CreatePath();
    private readonly ContentPresenter _content = new();
    private readonly TextBlock _text = new() { TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis };
    private readonly Border _contentHost;
    private readonly Path[] _shapes;

    internal DaisyTourAnnotationPresenter()
    {
        IsHitTestVisible = false;
        _shapes = [_body, _tail, _startHead, _endHead];
        _contentHost = new Border { Child = _content, Padding = new Thickness(16) };
        AutomationProperties.SetAccessibilityView(_contentHost, AccessibilityView.Raw);
        AutomationProperties.SetAccessibilityView(_content, AccessibilityView.Raw);
        AutomationProperties.SetAccessibilityView(_text, AccessibilityView.Raw);
        Children.Add(_tail);
        Children.Add(_body);
        Children.Add(_startHead);
        Children.Add(_endHead);
        Children.Add(_contentHost);
    }

    internal void Update(DaisyTourAnnotation annotation, Rect sourceBounds, Rect targetBounds,
        Brush defaultStroke, Brush defaultFill, Brush defaultForeground,
        DaisyTourAnnotationStyle? hostDefaults = null, DaisyTourAnnotationStyle? stepDefaults = null, double scale = 1,
        FlowDirection textFlowDirection = FlowDirection.LeftToRight)
    {
        _contentHost.FlowDirection = textFlowDirection;
        var style = new EffectiveStyle(annotation, stepDefaults, hostDefaults, scale);
        var kind = style.Kind;
        var stroke = style.Get(DaisyTourAnnotationStyle.StrokeProperty, defaultStroke);
        var fill = style.Get(DaisyTourAnnotationStyle.FillProperty, defaultFill);
        var thickness = Math.Max(0, style.Dimension(DaisyTourAnnotationStyle.StrokeThicknessProperty,
            style.Preset == DaisyTourAnnotationPreset.ThickArrow ? 7 : 3));
        var foreground = style.Get(DaisyTourAnnotationStyle.ForegroundProperty, defaultForeground);
        Opacity = Math.Clamp(style.Number(DaisyTourAnnotationStyle.OpacityProperty, 1), 0, 1);
        if (DaisySystemAppearance.HighContrast)
        {
            stroke = defaultStroke;
            fill = defaultFill;
            foreground = defaultForeground;
            thickness = Math.Max(1, thickness);
            Opacity = 1;
        }

        foreach (var shape in _shapes)
        {
            shape.Data = null;
            shape.RenderTransform = null;
            shape.Stretch = Stretch.None;
            shape.Width = double.NaN;
            shape.Height = double.NaN;
            SetLeft(shape, 0);
            SetTop(shape, 0);
            shape.Stroke = stroke;
            shape.Fill = null;
            shape.StrokeThickness = thickness;
            shape.StrokeLineJoin = PenLineJoin.Round;
            shape.StrokeStartLineCap = PenLineCap.Round;
            shape.StrokeEndLineCap = PenLineCap.Round;
            shape.StrokeDashArray = null;
        }
        ApplyDashes(_body, style);
        _contentHost.Visibility = Visibility.Collapsed;
        var source = Offset(Anchor(sourceBounds, Center(targetBounds), annotation.SourceAnchor),
            style.Number(DaisyTourAnnotationStyle.SourceOffsetXProperty, 0), style.Number(DaisyTourAnnotationStyle.SourceOffsetYProperty, 0));
        var target = Offset(Anchor(targetBounds, Center(sourceBounds), annotation.TargetAnchor),
            style.Number(DaisyTourAnnotationStyle.TargetOffsetXProperty, 0), style.Number(DaisyTourAnnotationStyle.TargetOffsetYProperty, 0));

        if (kind is DaisyTourAnnotationKind.Arrow or DaisyTourAnnotationKind.Line)
        {
            var midpoint = DrawConnector(style, kind, source, target, fill);
            ArrangeContent(annotation, style, midpoint, foreground);
            _contentHost.Background = fill;
            return;
        }

        DrawSurface(annotation, style, kind, sourceBounds, target, fill, foreground);
    }

    internal static DaisyTourAnnotationKind ResolveKind(DaisyTourAnnotation annotation,
        DaisyTourAnnotationStyle? hostDefaults = null, DaisyTourAnnotationStyle? stepDefaults = null)
        => new EffectiveStyle(annotation, stepDefaults, hostDefaults).Kind;

    internal Size MeasureSurface(DaisyTourAnnotation annotation, Brush defaultForeground,
        DaisyTourAnnotationStyle? hostDefaults = null, DaisyTourAnnotationStyle? stepDefaults = null,
        double scale = 1, FlowDirection textFlowDirection = FlowDirection.LeftToRight)
    {
        _contentHost.FlowDirection = textFlowDirection;
        var style = new EffectiveStyle(annotation, stepDefaults, hostDefaults, scale);
        var foreground = DaisySystemAppearance.HighContrast ? defaultForeground
            : style.Get(DaisyTourAnnotationStyle.ForegroundProperty, defaultForeground);
        var rect = ArrangeContent(annotation, style, new Point(), foreground);
        return new Size(rect.Width, rect.Height);
    }

    private Point DrawConnector(EffectiveStyle style, DaisyTourAnnotationKind kind, Point source, Point target, Brush fill)
    {
        var path = style.Get(DaisyTourAnnotationStyle.PathProperty,
            style.Preset == DaisyTourAnnotationPreset.CurvedArrow ? DaisyTourAnnotationPath.Curved : DaisyTourAnnotationPath.Straight);
        var curvature = style.Number(DaisyTourAnnotationStyle.CurvatureProperty, 0.25);
        var points = ConnectorPoints(source, target, path, curvature);
        var block = style.Get(DaisyTourAnnotationStyle.ArrowStyleProperty,
            style.Preset == DaisyTourAnnotationPreset.BlockArrow ? DaisyTourArrowStyle.Block : DaisyTourArrowStyle.Line) == DaisyTourArrowStyle.Block;
        var start = style.Get(DaisyTourAnnotationStyle.StartHeadProperty,
            style.Preset == DaisyTourAnnotationPreset.TwoEndedArrow ? DaisyTourArrowHead.Triangle : DaisyTourArrowHead.None);
        var end = style.Get(DaisyTourAnnotationStyle.EndHeadProperty,
            kind == DaisyTourAnnotationKind.Line ? DaisyTourArrowHead.None : DaisyTourArrowHead.Triangle);
        var length = Math.Clamp(style.Dimension(DaisyTourAnnotationStyle.HeadLengthProperty, 22), 0, PolylineLength(points) / 3);
        var width = Math.Max(0, style.Dimension(DaisyTourAnnotationStyle.HeadWidthProperty, 18));
        _body.Data = block
            ? BlockGeometry(TrimEndpoints(points, start == DaisyTourArrowHead.None ? 0 : length, end == DaisyTourArrowHead.None ? 0 : length),
                Math.Max(1, style.Dimension(DaisyTourAnnotationStyle.BodyWidthProperty, 12)))
            : ConnectorGeometry(source, target, path, curvature, points);
        _body.Fill = block ? fill : null;
        DrawHead(_startHead, start, points[0], points[1], length, width, fill);
        DrawHead(_endHead, end, points[^1], points[^2], length, width, fill);
        return PolylineMidpoint(points);
    }

    private void DrawSurface(DaisyTourAnnotation annotation, EffectiveStyle style, DaisyTourAnnotationKind kind,
        Rect sourceBounds, Point target, Brush fill, Brush foreground)
    {
        var rect = ArrangeContent(annotation, style, Center(sourceBounds), foreground);
        _contentHost.Background = null;
        var radius = Math.Clamp(style.Dimension(DaisyTourAnnotationStyle.CornerRadiusProperty, 16), 0, Math.Min(rect.Width, rect.Height) / 2);
        _body.Fill = fill;
        _body.Data = kind switch
        {
            DaisyTourAnnotationKind.Cloud => CloudGeometry(rect),
            DaisyTourAnnotationKind.Custom => annotation.Geometry,
            _ => RoundedRectangle(rect, radius)
        };
        if (kind == DaisyTourAnnotationKind.Custom && annotation.Geometry is not null)
        {
            _body.Stretch = Stretch.Fill;
            _body.Width = rect.Width;
            _body.Height = rect.Height;
            SetLeft(_body, rect.X);
            SetTop(_body, rect.Y);
        }

        var tail = style.Get(DaisyTourAnnotationStyle.TailProperty, DaisyTourAnnotationTail.None);
        DrawTail(tail, rect, target, style, fill);
    }

    private Rect ArrangeContent(DaisyTourAnnotation annotation, EffectiveStyle style, Point anchor, Brush foreground)
    {
        var hasContent = annotation.Template is not null || annotation.Content is not null || !string.IsNullOrWhiteSpace(annotation.Text);
        var maxWidth = Math.Max(1, style.Dimension(DaisyTourAnnotationStyle.MaxWidthProperty, 280));
        var maxHeight = Math.Max(1, style.Dimension(DaisyTourAnnotationStyle.MaxHeightProperty, 180));
        if (double.IsFinite(Width)) maxWidth = Math.Min(maxWidth, Math.Max(1, Width - 24));
        if (double.IsFinite(Height)) maxHeight = Math.Min(maxHeight, Math.Max(1, Height - 24));
        _text.Text = annotation.Text;
        _text.MaxLines = 0;
        _text.FontSize = Math.Max(1, style.Dimension(DaisyTourAnnotationStyle.FontSizeProperty, 16));
        _text.Foreground = foreground;
        _content.ContentTemplate = annotation.Template;
        _content.Content = annotation.Content ?? (annotation.Template is null ? _text : annotation);
        _contentHost.MaxWidth = maxWidth;
        _contentHost.MaxHeight = maxHeight;
        _contentHost.Width = double.NaN;
        _contentHost.Height = double.NaN;
        _contentHost.Padding = new Thickness(16 * style.Scale);
        _contentHost.Visibility = hasContent ? Visibility.Visible : Visibility.Collapsed;
        if (hasContent)
            _contentHost.Measure(new Size(maxWidth, maxHeight));

        var width = Math.Clamp(style.Number(DaisyTourAnnotationStyle.WidthProperty,
            hasContent ? Math.Max(80 * style.Scale, _contentHost.DesiredSize.Width) : 100 * style.Scale), 1, maxWidth);
        var height = Math.Clamp(style.Number(DaisyTourAnnotationStyle.HeightProperty,
            hasContent ? Math.Max(60 * style.Scale, _contentHost.DesiredSize.Height) : 64 * style.Scale), 1, maxHeight);
        _text.MaxLines = Math.Max(1, (int)((height - 32 * style.Scale) / (_text.FontSize * 1.4)));
        _contentHost.Clip = new RectangleGeometry { Rect = new Rect(0, 0, width, height) };
        var center = Offset(anchor, style.Number(DaisyTourAnnotationStyle.OffsetXProperty, 0), style.Number(DaisyTourAnnotationStyle.OffsetYProperty, 0));
        var rect = new Rect(center.X - width / 2, center.Y - height / 2, width, height);
        _contentHost.Width = width;
        _contentHost.Height = height;
        SetLeft(_contentHost, rect.X);
        SetTop(_contentHost, rect.Y);
        AutomationProperties.SetName(_contentHost, annotation.Text);
        return rect;
    }

    private void DrawTail(DaisyTourAnnotationTail tail, Rect rect, Point target, EffectiveStyle style, Brush fill)
    {
        if (tail == DaisyTourAnnotationTail.None)
            return;
        var start = Anchor(rect, target, DaisyTourAnchor.Auto);
        var direction = Unit(start, target);
        var distance = Distance(start, target);
        var length = Math.Min(distance, Math.Max(0, style.Dimension(DaisyTourAnnotationStyle.TailLengthProperty, distance)));
        var end = Offset(start, direction.X * length, direction.Y * length);
        var width = Math.Max(0, style.Dimension(DaisyTourAnnotationStyle.TailWidthProperty, 18));
        _tail.Fill = fill;
        if (tail == DaisyTourAnnotationTail.Speech)
        {
            _tail.Data = Polygon([
                Offset(start, -direction.Y * width / 2, direction.X * width / 2),
                end, Offset(start, direction.Y * width / 2, -direction.X * width / 2)]);
            return;
        }
        var geometry = new GeometryGroup();
        for (var index = 0; index < 3; index++)
        {
            var fraction = (index + 1) / 4d;
            var radius = width / 2 * (1 - fraction);
            geometry.Children.Add(new EllipseGeometry
            {
                Center = Offset(start, direction.X * length * fraction, direction.Y * length * fraction),
                RadiusX = radius, RadiusY = radius
            });
        }
        _tail.Data = geometry;
    }

    private static void ApplyDashes(Path shape, EffectiveStyle style)
    {
        var customPattern = style.Get<DaisyTourDashCollection?>(DaisyTourAnnotationStyle.DashArrayProperty, null);
        var mode = style.Get(DaisyTourAnnotationStyle.StrokeStyleProperty, customPattern is not null ? DaisyTourStrokeStyle.Custom : style.Preset switch
        {
            DaisyTourAnnotationPreset.DottedLine => DaisyTourStrokeStyle.Dotted,
            DaisyTourAnnotationPreset.DashedLine => DaisyTourStrokeStyle.Dashed,
            DaisyTourAnnotationPreset.DashDotLine => DaisyTourStrokeStyle.DashDot,
            _ => DaisyTourStrokeStyle.Solid
        });
        IEnumerable<double>? pattern = mode switch
        {
            DaisyTourStrokeStyle.Custom => customPattern,
            DaisyTourStrokeStyle.Dotted => [0.1, 2],
            DaisyTourStrokeStyle.Dashed => [4, 2],
            DaisyTourStrokeStyle.DashDot => [4, 2, 0.1, 2],
            _ => null
        };
        if (pattern is null)
            return;
        var dashes = new DoubleCollection();
        foreach (var dash in pattern)
        {
            if (double.IsFinite(dash) && dash >= 0)
                dashes.Add(dash);
        }
        if (dashes.Any(static dash => dash > 0))
            shape.StrokeDashArray = dashes;
        shape.StrokeDashOffset = style.Number(DaisyTourAnnotationStyle.DashOffsetProperty, 0);
    }

    private static Path CreatePath()
    {
        var path = new Path { IsHitTestVisible = false };
        AutomationProperties.SetAccessibilityView(path, AccessibilityView.Raw);
        return path;
    }

    internal static Point Anchor(Rect rect, Point toward, DaisyTourAnchor anchor)
    {
        var center = Center(rect);
        if (anchor != DaisyTourAnchor.Auto)
        {
            return anchor switch
            {
                DaisyTourAnchor.Top => new Point(center.X, rect.Top),
                DaisyTourAnchor.Bottom => new Point(center.X, rect.Bottom),
                DaisyTourAnchor.Left => new Point(rect.Left, center.Y),
                DaisyTourAnchor.Right => new Point(rect.Right, center.Y),
                _ => center
            };
        }
        var dx = toward.X - center.X;
        var dy = toward.Y - center.Y;
        if (Math.Abs(dx) + Math.Abs(dy) < 0.001)
            return center;
        var scaleX = Math.Abs(dx) > 0.001 ? rect.Width / 2 / Math.Abs(dx) : double.PositiveInfinity;
        var scaleY = Math.Abs(dy) > 0.001 ? rect.Height / 2 / Math.Abs(dy) : double.PositiveInfinity;
        var scale = Math.Min(scaleX, scaleY);
        return Offset(center, dx * scale, dy * scale);
    }

    private static List<Point> ConnectorPoints(Point start, Point end, DaisyTourAnnotationPath path, double curvature)
    {
        if (path == DaisyTourAnnotationPath.Elbow)
        {
            if (Math.Abs(end.X - start.X) >= Math.Abs(end.Y - start.Y))
                return [start, new Point((start.X + end.X) / 2, start.Y), new Point((start.X + end.X) / 2, end.Y), end];
            return [start, new Point(start.X, (start.Y + end.Y) / 2), new Point(end.X, (start.Y + end.Y) / 2), end];
        }
        if (path != DaisyTourAnnotationPath.Curved)
            return [start, end];

        var dx = end.X - start.X;
        var dy = end.Y - start.Y;
        var control = new Point((start.X + end.X) / 2 - dy * curvature, (start.Y + end.Y) / 2 + dx * curvature);
        var points = new List<Point>(25);
        for (var index = 0; index <= 24; index++)
        {
            var t = index / 24d;
            var u = 1 - t;
            points.Add(new Point(u * u * start.X + 2 * u * t * control.X + t * t * end.X,
                u * u * start.Y + 2 * u * t * control.Y + t * t * end.Y));
        }
        return points;
    }

    private static Geometry BlockGeometry(IReadOnlyList<Point> points, double width)
    {
        var left = new List<Point>(points.Count);
        var right = new List<Point>(points.Count);
        for (var index = 0; index < points.Count; index++)
        {
            var direction = Unit(points[Math.Max(0, index - 1)], points[Math.Min(points.Count - 1, index + 1)]);
            left.Add(Offset(points[index], -direction.Y * width / 2, direction.X * width / 2));
            right.Add(Offset(points[index], direction.Y * width / 2, -direction.X * width / 2));
        }
        right.Reverse();
        left.AddRange(right);
        return Polygon(left);
    }

    private static double PolylineLength(IReadOnlyList<Point> points)
    {
        var length = 0d;
        for (var index = 1; index < points.Count; index++)
            length += Distance(points[index - 1], points[index]);
        return length;
    }

    private static Point PolylineMidpoint(IReadOnlyList<Point> points)
    {
        var remaining = PolylineLength(points) / 2;
        for (var index = 1; index < points.Count; index++)
        {
            var segment = Distance(points[index - 1], points[index]);
            if (remaining <= segment)
            {
                var direction = Unit(points[index - 1], points[index]);
                return Offset(points[index - 1], direction.X * remaining, direction.Y * remaining);
            }
            remaining -= segment;
        }
        return points[^1];
    }

    private static List<Point> TrimEndpoints(IReadOnlyList<Point> points, double start, double end)
    {
        var result = points.ToList();
        TrimStart(result, start);
        result.Reverse();
        TrimStart(result, end);
        result.Reverse();
        return result;
    }

    private static void TrimStart(List<Point> points, double distance)
    {
        while (points.Count > 2 && Distance(points[0], points[1]) < distance)
        {
            distance -= Distance(points[0], points[1]);
            points.RemoveAt(0);
        }
        var direction = Unit(points[0], points[1]);
        points[0] = Offset(points[0], direction.X * distance, direction.Y * distance);
    }

    private static Geometry ConnectorGeometry(Point start, Point end, DaisyTourAnnotationPath path, double curvature, IReadOnlyList<Point> points)
    {
        if (path != DaisyTourAnnotationPath.Curved)
            return LineGeometry(points);
        var figure = new PathFigure { StartPoint = start, IsFilled = false };
        figure.Segments.Add(new QuadraticBezierSegment
        {
            Point1 = new Point((start.X + end.X) / 2 - (end.Y - start.Y) * curvature,
                (start.Y + end.Y) / 2 + (end.X - start.X) * curvature),
            Point2 = end
        });
        var geometry = new PathGeometry();
        geometry.Figures.Add(figure);
        return geometry;
    }

    private static void DrawHead(Path shape, DaisyTourArrowHead head, Point tip, Point neighbor, double length, double width, Brush fill)
    {
        if (head == DaisyTourArrowHead.None)
            return;
        var direction = Unit(tip, neighbor);
        var baseCenter = Offset(tip, direction.X * length, direction.Y * length);
        var left = Offset(baseCenter, -direction.Y * width / 2, direction.X * width / 2);
        var right = Offset(baseCenter, direction.Y * width / 2, -direction.X * width / 2);
        shape.Fill = head == DaisyTourArrowHead.Open ? null : fill;
        shape.Data = head switch
        {
            DaisyTourArrowHead.Open => LineGeometry([left, tip, right]),
            DaisyTourArrowHead.Diamond => Polygon([tip, left, Offset(tip, direction.X * length * 2, direction.Y * length * 2), right]),
            DaisyTourArrowHead.Circle => new EllipseGeometry { Center = baseCenter, RadiusX = width / 2, RadiusY = width / 2 },
            _ => Polygon([tip, left, right])
        };
    }

    private static Geometry CloudGeometry(Rect rect)
    {
        // A smooth, closed scalloped contour; text stays inside its central area.
        var points = new List<Point>(97);
        var center = Center(rect);
        for (var index = 0; index <= 96; index++)
        {
            var angle = index * Math.PI * 2 / 96;
            var scallop = 0.9 + 0.1 * Math.Cos(angle * 8);
            points.Add(new Point(center.X + rect.Width / 2 * Math.Cos(angle) * scallop,
                center.Y + rect.Height / 2 * Math.Sin(angle) * scallop));
        }
        return Polygon(points);
    }

    private static PathGeometry RoundedRectangle(Rect rect, double radius)
    {
        var figure = new PathFigure { StartPoint = new Point(rect.Left + radius, rect.Top), IsClosed = true, IsFilled = true };
        figure.Segments.Add(new LineSegment { Point = new Point(rect.Right - radius, rect.Top) });
        figure.Segments.Add(new QuadraticBezierSegment { Point1 = new Point(rect.Right, rect.Top), Point2 = new Point(rect.Right, rect.Top + radius) });
        figure.Segments.Add(new LineSegment { Point = new Point(rect.Right, rect.Bottom - radius) });
        figure.Segments.Add(new QuadraticBezierSegment { Point1 = new Point(rect.Right, rect.Bottom), Point2 = new Point(rect.Right - radius, rect.Bottom) });
        figure.Segments.Add(new LineSegment { Point = new Point(rect.Left + radius, rect.Bottom) });
        figure.Segments.Add(new QuadraticBezierSegment { Point1 = new Point(rect.Left, rect.Bottom), Point2 = new Point(rect.Left, rect.Bottom - radius) });
        figure.Segments.Add(new LineSegment { Point = new Point(rect.Left, rect.Top + radius) });
        figure.Segments.Add(new QuadraticBezierSegment { Point1 = new Point(rect.Left, rect.Top), Point2 = new Point(rect.Left + radius, rect.Top) });
        var geometry = new PathGeometry();
        geometry.Figures.Add(figure);
        return geometry;
    }

    private static PathGeometry LineGeometry(IReadOnlyList<Point> points) => FigureGeometry(points, false);
    private static PathGeometry Polygon(IReadOnlyList<Point> points) => FigureGeometry(points, true);

    private static PathGeometry FigureGeometry(IReadOnlyList<Point> points, bool closed)
    {
        var figure = new PathFigure { StartPoint = points[0], IsClosed = closed, IsFilled = closed };
        for (var index = 1; index < points.Count; index++)
            figure.Segments.Add(new LineSegment { Point = points[index] });
        var geometry = new PathGeometry();
        geometry.Figures.Add(figure);
        return geometry;
    }

    private static Point Center(Rect rect) => new(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);
    private static Point Offset(Point point, double x, double y) => new(point.X + x, point.Y + y);
    private static double Distance(Point start, Point end) => Math.Sqrt(Math.Pow(end.X - start.X, 2) + Math.Pow(end.Y - start.Y, 2));
    private static Point Unit(Point start, Point end)
    {
        var length = Distance(start, end);
        return length > 0.001 ? new Point((end.X - start.X) / length, (end.Y - start.Y) / length) : new Point(1, 0);
    }

    private sealed class EffectiveStyle(DaisyTourAnnotation annotation, DaisyTourAnnotationStyle? step, DaisyTourAnnotationStyle? host, double scale = 1)
    {
        private readonly DaisyTourAnnotationStyle?[] _styles = [annotation, step, host];
        internal double Scale { get; } = double.IsFinite(scale) && scale > 0 ? scale : 1;
        internal DaisyTourAnnotationPreset Preset => Get(DaisyTourAnnotationStyle.PresetProperty, DaisyTourAnnotationPreset.ThinArrow);
        internal DaisyTourAnnotationKind Kind => Get(DaisyTourAnnotationStyle.KindProperty, Preset switch
        {
            DaisyTourAnnotationPreset.Cloud => DaisyTourAnnotationKind.Cloud,
            DaisyTourAnnotationPreset.SpeechBubble => DaisyTourAnnotationKind.SpeechBubble,
            DaisyTourAnnotationPreset.DottedLine or DaisyTourAnnotationPreset.DashedLine or DaisyTourAnnotationPreset.DashDotLine => DaisyTourAnnotationKind.Line,
            _ => DaisyTourAnnotationKind.Arrow
        });

        internal T Get<T>(DependencyProperty property, T fallback)
        {
            foreach (var style in _styles)
            {
                var value = style?.GetValue(property);
                if (value is null || value is double number && !double.IsFinite(number) ||
                    value is Enum enumeration && Convert.ToInt32(enumeration) == 0)
                    continue;
                return (T)value;
            }
            return fallback;
        }

        internal double Number(DependencyProperty property, double fallback) => Get(property, fallback);
        internal double Dimension(DependencyProperty property, double fallback) => Get(property, fallback * Scale);
    }
}
