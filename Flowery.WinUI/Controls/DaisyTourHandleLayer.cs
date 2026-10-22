using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace Flowery.Controls;

/// <summary>Pointer and keyboard editing over the same geometry used by the tour presenter.</summary>
internal sealed partial class DaisyTourHandleLayer : Canvas
{
    private enum HandleKind { Source, Target, Curvature, FirstControl, SecondControl, BodyWidth, HeadLength, HeadWidth }
    private readonly DaisyTourEditor _editor;
    private readonly Dictionary<HandleKind, Button> _handles = [];
    private readonly List<Path> _hitPaths = [];
    private readonly Dictionary<HandleKind, Point> _positions = [];
    private HandleKind? _drag;
    private DaisyTourEditGeometry? _dragGeometry;
    private Pointer? _pointer;
    private Point _dragOrigin;
    private Point _dragPointerOrigin;

    internal DaisyTourHandleLayer(DaisyTourEditor editor)
    {
        _editor = editor;
        IsTabStop = false;
        foreach (var kind in Enum.GetValues<HandleKind>())
        {
            var button = new Button { Width = 28, Height = 28, Padding = new Thickness(0), Content = kind is HandleKind.Source or HandleKind.Target ? "●" : "◆", Tag = kind };
            AutomationProperties.SetAutomationId(button, "DaisyTourHandle" + kind);
            button.AddHandler(PointerPressedEvent, new PointerEventHandler(OnPressed), true);
            button.AddHandler(PointerMovedEvent, new PointerEventHandler(OnMoved), true);
            button.AddHandler(PointerReleasedEvent, new PointerEventHandler(OnReleased), true);
            button.AddHandler(PointerCanceledEvent, new PointerEventHandler(OnCanceled), true);
            button.AddHandler(PointerCaptureLostEvent, new PointerEventHandler(OnCaptureLost), true);
            button.KeyDown += OnKeyDown;
            _handles.Add(kind, button);
            Children.Add(button);
        }
    }

    internal void Update()
    {
        var tour = _editor.Tour;
        var step = _editor.SelectedStep;
        if (tour is null || step is null || tour.CurrentStep != step) { HideHandles(); return; }
        while (_hitPaths.Count < tour.AnnotationPresenters.Count)
        {
            var path = new Path { Stroke = new SolidColorBrush(Colors.Transparent), Fill = new SolidColorBrush(Colors.Transparent), StrokeThickness = 16 };
            path.PointerPressed += OnSelect;
            _hitPaths.Add(path);
            Children.Insert(0, path);
        }
        for (var index = 0; index < _hitPaths.Count; index++)
        {
            var path = _hitPaths[index];
            path.Data = null;
            if (index >= tour.AnnotationPresenters.Count) continue;
            var points = tour.AnnotationPresenters[index].Course;
            path.Tag = index;
            path.Width = Width;
            path.Height = Height;
            if (points.Count < 2)
            {
                if (tour.AnnotationPresenters[index].SelectionBounds is { } bounds)
                    path.Data = new RectangleGeometry { Rect = bounds };
                continue;
            }
            var figure = new PathFigure { StartPoint = points[0], IsFilled = false };
            for (var point = 1; point < points.Count; point++) figure.Segments.Add(new LineSegment { Point = points[point] });
            var hitGeometry = new PathGeometry();
            hitGeometry.Figures.Add(figure);
            path.Data = hitGeometry;
        }
        var selected = _editor.SelectedAnnotationIndex;
        if (selected < 0 || selected >= tour.AnnotationPresenters.Count || tour.AnnotationPresenters[selected].EditGeometry is not { } geometryForEdit)
        { HideHandles(); return; }
        var geometry = geometryForEdit;
        var along = Unit(geometry.Source, geometry.Target);
        var normal = new Point(-along.Y, along.X);
        var below = normal.Y < 0 || (normal.Y == 0 && normal.X < 0) ? new Point(-normal.X, -normal.Y) : normal;
        // Keep the 28-DIP handle just below the rendered midpoint, including on diagonal paths.
        var clearance = 6 + 14 * (Math.Abs(below.X) + Math.Abs(below.Y)) + tour.AnnotationPresenters[selected].CourseThickness / 2;
        var curve = Add(geometry.BodyCenter, below, clearance);
        var tipDirection = geometry.HeadDirection;
        var tipNormal = new Point(-tipDirection.Y, tipDirection.X);
        var headBase = Add(geometry.HeadTip, tipDirection, -geometry.HeadLength);
        SetHandle(HandleKind.Source, geometryForEdit.Source, true);
        SetHandle(HandleKind.Target, geometryForEdit.Target, true);
        SetHandle(HandleKind.Curvature, curve, geometry.Path == DaisyTourAnnotationPath.Curved && !geometry.HasRouteLimit && Distance(geometry.Source, geometry.Target) > 0.001);
        SetHandle(HandleKind.FirstControl, geometry.FirstControl, geometry.Path == DaisyTourAnnotationPath.Bezier && !geometry.HasRouteLimit);
        SetHandle(HandleKind.SecondControl, geometry.SecondControl, geometry.Path == DaisyTourAnnotationPath.Bezier && !geometry.HasRouteLimit);
        SetHandle(HandleKind.BodyWidth, Add(geometry.BodyCenter, normal, geometry.BodyWidth / 2), geometry.IsBlock);
        SetHandle(HandleKind.HeadLength, headBase, geometry.HasEndHead);
        SetHandle(HandleKind.HeadWidth, Add(headBase, tipNormal, geometry.HeadWidth / 2), geometry.HasEndHead);
    }

    private void SetHandle(HandleKind kind, Point point, bool visible)
    {
        var button = _handles[kind];
        button.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        _positions[kind] = point;
        SetLeft(button, point.X - button.Width / 2);
        SetTop(button, point.Y - button.Height / 2);
        button.Background = DaisyResourceLookup.GetBrush("DaisyBase100Brush");
        button.Foreground = DaisyResourceLookup.GetBrush("DaisyPrimaryBrush");
        button.BorderBrush = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush");
        var name = FloweryLocalization.GetStringInternal("TourEditor_Handle_" + kind);
        AutomationProperties.SetName(button, name);
        ToolTipService.SetToolTip(button, name);
    }

    private void OnSelect(object sender, PointerRoutedEventArgs args)
    {
        if (sender is Path { Tag: int index }) { _editor.SelectAnnotation(index); args.Handled = true; }
    }

    private void OnPressed(object sender, PointerRoutedEventArgs args)
    {
        if (sender is not Button { Tag: HandleKind kind } button || !args.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        if (GetGeometry() is not { } geometry) return;
        _drag = kind;
        _dragGeometry = geometry;
        _pointer = args.Pointer;
        _dragOrigin = _positions[kind];
        _dragPointerOrigin = args.GetCurrentPoint(this).Position;
        _editor.BeginGesture();
        button.CapturePointer(args.Pointer);
        button.Focus(FocusState.Pointer);
        args.Handled = true;
    }

    private void OnMoved(object sender, PointerRoutedEventArgs args)
    {
        if (_drag is not { } kind || _dragGeometry is not { } geometry) return;
        ApplyPoint(kind, DragPoint(args), geometry, _dragOrigin);
        args.Handled = true;
    }

    private void OnReleased(object sender, PointerRoutedEventArgs args)
    {
        if (_drag is not { } kind || _dragGeometry is not { } geometry) return;
        ApplyPoint(kind, DragPoint(args), geometry, _dragOrigin);
        _editor.EndGesture();
        ReleaseGesture();
        args.Handled = true;
    }

    private void OnCanceled(object sender, PointerRoutedEventArgs args)
    {
        if (_drag is null) return;
        _editor.CancelGesture();
        ReleaseGesture();
        args.Handled = true;
    }

    private void OnCaptureLost(object sender, PointerRoutedEventArgs args)
    {
        if (_drag is not { } kind || _dragGeometry is not { } geometry) return;
        // Button can release capture before PointerReleased reaches handled-events-too listeners.
        // The final point can be newer than the last delivered PointerMoved event.
        ApplyPoint(kind, DragPoint(args), geometry, _dragOrigin);
        _drag = null;
        _dragGeometry = null;
        _pointer = null;
        _editor.EndGesture();
    }

    private void OnKeyDown(object sender, KeyRoutedEventArgs args)
    {
        if (sender is not Button { Tag: HandleKind kind } || GetGeometry() is not { } geometry) return;
        if (args.Key is not (VirtualKey.Left or VirtualKey.Right or VirtualKey.Up or VirtualKey.Down)) return;
        var step = (InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift) & Windows.UI.Core.CoreVirtualKeyStates.Down) != 0 ? 10d : 1d;
        var point = _positions[kind];
        point.X += args.Key == VirtualKey.Left ? -step : args.Key == VirtualKey.Right ? step : 0;
        point.Y += args.Key == VirtualKey.Up ? -step : args.Key == VirtualKey.Down ? step : 0;
        _editor.BeginGesture();
        ApplyPoint(kind, point, geometry, _positions[kind]);
        _editor.EndGesture();
        args.Handled = true;
    }

    private Point DragPoint(PointerRoutedEventArgs args)
    {
        var point = args.GetCurrentPoint(this).Position;
        return new Point(_dragOrigin.X + point.X - _dragPointerOrigin.X, _dragOrigin.Y + point.Y - _dragPointerOrigin.Y);
    }

    private void ApplyPoint(HandleKind kind, Point point, DaisyTourEditGeometry geometry, Point origin)
    {
        var along = Unit(geometry.Source, geometry.Target);
        var normal = new Point(-along.Y, along.X);
        var length = Math.Max(1, Distance(geometry.Source, geometry.Target));
        var delta = new Point(point.X - geometry.Source.X, point.Y - geometry.Source.Y);
        switch (kind)
        {
            case HandleKind.Source:
            case HandleKind.Target:
                var source = kind == HandleKind.Source;
                var bounds = source ? geometry.SourceBounds : geometry.TargetBounds;
                var anchor = NearestAnchor(bounds, point);
                var dock = DaisyTourAnnotationPresenter.Anchor(bounds, point, anchor);
                _editor.SetAnnotationValue(source ? "sourceAnchor" : "targetAnchor", anchor, publish: false);
                _editor.SetAnnotationValue(source ? "sourceOffsetX" : "targetOffsetX", point.X - dock.X, publish: false);
                _editor.SetAnnotationValue(source ? "sourceOffsetY" : "targetOffsetY", point.Y - dock.Y);
                break;
            case HandleKind.Curvature:
                _editor.SetAnnotationValue("curvature", ProjectCurvature(point, geometry, origin));
                break;
            case HandleKind.FirstControl:
            case HandleKind.SecondControl:
                var prefix = kind == HandleKind.FirstControl ? "controlPoint1" : "controlPoint2";
                _editor.SetAnnotationValue(prefix + "X", (delta.X * along.X + delta.Y * along.Y) / length, publish: false);
                _editor.SetAnnotationValue(prefix + "Y", (delta.X * normal.X + delta.Y * normal.Y) / length);
                break;
            case HandleKind.BodyWidth:
                _editor.SetAnnotationValue("bodyWidth", Math.Max(1, ProjectWidth(point, geometry.BodyCenter, normal)));
                break;
            case HandleKind.HeadLength:
                _editor.SetAnnotationValue("headLength", ProjectHeadLength(point, geometry));
                break;
            case HandleKind.HeadWidth:
                var headNormal = new Point(-geometry.HeadDirection.Y, geometry.HeadDirection.X);
                _editor.SetAnnotationValue("headWidth", ProjectWidth(point, geometry.HeadTip, headNormal));
                break;
        }
    }

    private DaisyTourEditGeometry? GetGeometry()
    {
        var index = _editor.SelectedAnnotationIndex;
        var presenters = _editor.Tour?.AnnotationPresenters;
        return presenters is not null && index >= 0 && index < presenters.Count ? presenters[index].EditGeometry : null;
    }

    internal void ReleaseGesture()
    {
        var kind = _drag;
        var pointer = _pointer;
        _drag = null;
        _dragGeometry = null;
        _pointer = null;
        if (kind is { } selected && pointer is not null) _handles[selected].ReleasePointerCapture(pointer);
    }

    internal void Clear()
    {
        ReleaseGesture();
        foreach (var path in _hitPaths) path.Data = null;
        HideHandles();
    }
    private void HideHandles() { foreach (var handle in _handles.Values) handle.Visibility = Visibility.Collapsed; }

    private static Point Add(Point point, Point direction, double scale) => new(point.X + direction.X * scale, point.Y + direction.Y * scale);
    internal static double ProjectWidth(Point point, Point center, Point normal) =>
        Math.Max(0, 2 * Math.Abs((point.X - center.X) * normal.X + (point.Y - center.Y) * normal.Y));
    internal static double ProjectHeadLength(Point point, DaisyTourEditGeometry geometry) =>
        Math.Max(0, (geometry.HeadTip.X - point.X) * geometry.HeadDirection.X + (geometry.HeadTip.Y - point.Y) * geometry.HeadDirection.Y);
    internal static double ProjectCurvature(Point point, DaisyTourEditGeometry geometry, Point origin)
    {
        var length = Distance(geometry.Source, geometry.Target);
        if (length <= 0.001) return geometry.Curvature;
        var along = Unit(geometry.Source, geometry.Target);
        return geometry.Curvature + 2 * (-(point.X - origin.X) * along.Y + (point.Y - origin.Y) * along.X) / length;
    }
    private static double Distance(Point a, Point b) => Math.Sqrt((b.X - a.X) * (b.X - a.X) + (b.Y - a.Y) * (b.Y - a.Y));
    private static Point Unit(Point a, Point b)
    {
        var length = Distance(a, b);
        return length > 0.001 ? new((b.X - a.X) / length, (b.Y - a.Y) / length) : new(1, 0);
    }
    private static DaisyTourAnchor NearestAnchor(Rect bounds, Point point)
    {
        var sides = new[] { Math.Abs(point.X - bounds.Left), Math.Abs(point.X - bounds.Right), Math.Abs(point.Y - bounds.Top), Math.Abs(point.Y - bounds.Bottom) };
        return Array.IndexOf(sides, sides.Min()) switch { 0 => DaisyTourAnchor.Left, 1 => DaisyTourAnchor.Right, 2 => DaisyTourAnchor.Top, _ => DaisyTourAnchor.Bottom };
    }
}
