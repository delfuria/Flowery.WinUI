using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace Flowery.Controls;

/// <summary>Common appearances that can be overridden property by property.</summary>
public enum DaisyTourAnnotationPreset { Default, ThinArrow, ThickArrow, BlockArrow, CurvedArrow, TwoEndedArrow, DottedLine, DashedLine, DashDotLine, Cloud, SpeechBubble }
public enum DaisyTourAnnotationKind { Default, Arrow, Line, Cloud, SpeechBubble, Custom }
public enum DaisyTourAnnotationPath { Default, Straight, Curved, Elbow, SCurve, Bezier, Custom }
public enum DaisyTourArrowHead { Default, None, Open, Triangle, Diamond, Circle }
public enum DaisyTourArrowStyle { Default, Line, Block }
public enum DaisyTourStrokeStyle { Default, Solid, Dotted, Dashed, DashDot, Custom }
public enum DaisyTourAnnotationTail { Default, None, Speech, Thought }
public enum DaisyTourAnchor { Auto, Top, Bottom, Left, Right, Center }
/// <summary>The side of a route limit that a connector's course must stay on.</summary>
public enum DaisyTourRouteSide { None, Above, Below, LeftOf, RightOf }

/// <summary>A live dash pattern whose values are multiples of stroke thickness.</summary>
public sealed class DaisyTourDashCollection : ObservableCollection<double>
{
}

/// <summary>
/// Appearance defaults shared by a tour, a step, or an individual annotation.
/// Null brushes, Default enums, and NaN measurements inherit the containing defaults.
/// Measurements are device-independent pixels, except Curvature and dash values.
/// </summary>
public partial class DaisyTourAnnotationStyle : DependencyObject
{
    public static readonly DependencyProperty PresetProperty = DependencyProperty.Register(
        nameof(Preset), typeof(DaisyTourAnnotationPreset), typeof(DaisyTourAnnotationStyle), new PropertyMetadata(DaisyTourAnnotationPreset.Default, OnChanged));
    public static readonly DependencyProperty KindProperty = DependencyProperty.Register(
        nameof(Kind), typeof(DaisyTourAnnotationKind), typeof(DaisyTourAnnotationStyle), new PropertyMetadata(DaisyTourAnnotationKind.Default, OnChanged));
    public static readonly DependencyProperty PathProperty = DependencyProperty.Register(
        nameof(Path), typeof(DaisyTourAnnotationPath), typeof(DaisyTourAnnotationStyle), new PropertyMetadata(DaisyTourAnnotationPath.Default, OnChanged));
    public static readonly DependencyProperty StartHeadProperty = DependencyProperty.Register(
        nameof(StartHead), typeof(DaisyTourArrowHead), typeof(DaisyTourAnnotationStyle), new PropertyMetadata(DaisyTourArrowHead.Default, OnChanged));
    public static readonly DependencyProperty EndHeadProperty = DependencyProperty.Register(
        nameof(EndHead), typeof(DaisyTourArrowHead), typeof(DaisyTourAnnotationStyle), new PropertyMetadata(DaisyTourArrowHead.Default, OnChanged));
    public static readonly DependencyProperty ArrowStyleProperty = DependencyProperty.Register(
        nameof(ArrowStyle), typeof(DaisyTourArrowStyle), typeof(DaisyTourAnnotationStyle), new PropertyMetadata(DaisyTourArrowStyle.Default, OnChanged));
    public static readonly DependencyProperty StrokeStyleProperty = DependencyProperty.Register(
        nameof(StrokeStyle), typeof(DaisyTourStrokeStyle), typeof(DaisyTourAnnotationStyle), new PropertyMetadata(DaisyTourStrokeStyle.Default, OnChanged));
    public static readonly DependencyProperty TailProperty = DependencyProperty.Register(
        nameof(Tail), typeof(DaisyTourAnnotationTail), typeof(DaisyTourAnnotationStyle), new PropertyMetadata(DaisyTourAnnotationTail.Default, OnChanged));
    public static readonly DependencyProperty StrokeProperty = DependencyProperty.Register(
        nameof(Stroke), typeof(Brush), typeof(DaisyTourAnnotationStyle), new PropertyMetadata(null, OnChanged));
    public static readonly DependencyProperty FillProperty = DependencyProperty.Register(
        nameof(Fill), typeof(Brush), typeof(DaisyTourAnnotationStyle), new PropertyMetadata(null, OnChanged));
    public static readonly DependencyProperty ForegroundProperty = DependencyProperty.Register(
        nameof(Foreground), typeof(Brush), typeof(DaisyTourAnnotationStyle), new PropertyMetadata(null, OnChanged));
    public static readonly DependencyProperty DashArrayProperty = DependencyProperty.Register(
        nameof(DashArray), typeof(DaisyTourDashCollection), typeof(DaisyTourAnnotationStyle), new PropertyMetadata(null, OnChanged));
    public static readonly DependencyProperty StrokeThicknessProperty = DependencyProperty.Register(
        nameof(StrokeThickness), typeof(double), typeof(DaisyTourAnnotationStyle), new PropertyMetadata(double.NaN, OnChanged));
    public static readonly DependencyProperty DashOffsetProperty = DependencyProperty.Register(
        nameof(DashOffset), typeof(double), typeof(DaisyTourAnnotationStyle), new PropertyMetadata(double.NaN, OnChanged));
    public static readonly DependencyProperty HeadLengthProperty = DependencyProperty.Register(
        nameof(HeadLength), typeof(double), typeof(DaisyTourAnnotationStyle), new PropertyMetadata(double.NaN, OnChanged));
    public static readonly DependencyProperty HeadWidthProperty = DependencyProperty.Register(
        nameof(HeadWidth), typeof(double), typeof(DaisyTourAnnotationStyle), new PropertyMetadata(double.NaN, OnChanged));
    public static readonly DependencyProperty BodyWidthProperty = DependencyProperty.Register(
        nameof(BodyWidth), typeof(double), typeof(DaisyTourAnnotationStyle), new PropertyMetadata(double.NaN, OnChanged));
    public static readonly DependencyProperty CurvatureProperty = DependencyProperty.Register(
        nameof(Curvature), typeof(double), typeof(DaisyTourAnnotationStyle), new PropertyMetadata(double.NaN, OnChanged));
    public static readonly DependencyProperty ControlPoint1XProperty = DependencyProperty.Register(
        nameof(ControlPoint1X), typeof(double), typeof(DaisyTourAnnotationStyle), new PropertyMetadata(double.NaN, OnChanged));
    public static readonly DependencyProperty ControlPoint1YProperty = DependencyProperty.Register(
        nameof(ControlPoint1Y), typeof(double), typeof(DaisyTourAnnotationStyle), new PropertyMetadata(double.NaN, OnChanged));
    public static readonly DependencyProperty ControlPoint2XProperty = DependencyProperty.Register(
        nameof(ControlPoint2X), typeof(double), typeof(DaisyTourAnnotationStyle), new PropertyMetadata(double.NaN, OnChanged));
    public static readonly DependencyProperty ControlPoint2YProperty = DependencyProperty.Register(
        nameof(ControlPoint2Y), typeof(double), typeof(DaisyTourAnnotationStyle), new PropertyMetadata(double.NaN, OnChanged));
    public static readonly DependencyProperty SourceOffsetXProperty = DependencyProperty.Register(
        nameof(SourceOffsetX), typeof(double), typeof(DaisyTourAnnotationStyle), new PropertyMetadata(double.NaN, OnChanged));
    public static readonly DependencyProperty SourceOffsetYProperty = DependencyProperty.Register(
        nameof(SourceOffsetY), typeof(double), typeof(DaisyTourAnnotationStyle), new PropertyMetadata(double.NaN, OnChanged));
    public static readonly DependencyProperty TargetOffsetXProperty = DependencyProperty.Register(
        nameof(TargetOffsetX), typeof(double), typeof(DaisyTourAnnotationStyle), new PropertyMetadata(double.NaN, OnChanged));
    public static readonly DependencyProperty TargetOffsetYProperty = DependencyProperty.Register(
        nameof(TargetOffsetY), typeof(double), typeof(DaisyTourAnnotationStyle), new PropertyMetadata(double.NaN, OnChanged));
    public static readonly DependencyProperty OffsetXProperty = DependencyProperty.Register(
        nameof(OffsetX), typeof(double), typeof(DaisyTourAnnotationStyle), new PropertyMetadata(double.NaN, OnChanged));
    public static readonly DependencyProperty OffsetYProperty = DependencyProperty.Register(
        nameof(OffsetY), typeof(double), typeof(DaisyTourAnnotationStyle), new PropertyMetadata(double.NaN, OnChanged));
    public static readonly DependencyProperty WidthProperty = DependencyProperty.Register(
        nameof(Width), typeof(double), typeof(DaisyTourAnnotationStyle), new PropertyMetadata(double.NaN, OnChanged));
    public static readonly DependencyProperty HeightProperty = DependencyProperty.Register(
        nameof(Height), typeof(double), typeof(DaisyTourAnnotationStyle), new PropertyMetadata(double.NaN, OnChanged));
    public static readonly DependencyProperty MaxWidthProperty = DependencyProperty.Register(
        nameof(MaxWidth), typeof(double), typeof(DaisyTourAnnotationStyle), new PropertyMetadata(double.NaN, OnChanged));
    public static readonly DependencyProperty MaxHeightProperty = DependencyProperty.Register(
        nameof(MaxHeight), typeof(double), typeof(DaisyTourAnnotationStyle), new PropertyMetadata(double.NaN, OnChanged));
    public static readonly DependencyProperty FontSizeProperty = DependencyProperty.Register(
        nameof(FontSize), typeof(double), typeof(DaisyTourAnnotationStyle), new PropertyMetadata(double.NaN, OnChanged));
    public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.Register(
        nameof(CornerRadius), typeof(double), typeof(DaisyTourAnnotationStyle), new PropertyMetadata(double.NaN, OnChanged));
    public static readonly DependencyProperty TailWidthProperty = DependencyProperty.Register(
        nameof(TailWidth), typeof(double), typeof(DaisyTourAnnotationStyle), new PropertyMetadata(double.NaN, OnChanged));
    public static readonly DependencyProperty TailLengthProperty = DependencyProperty.Register(
        nameof(TailLength), typeof(double), typeof(DaisyTourAnnotationStyle), new PropertyMetadata(double.NaN, OnChanged));
    public static readonly DependencyProperty OpacityProperty = DependencyProperty.Register(
        nameof(Opacity), typeof(double), typeof(DaisyTourAnnotationStyle), new PropertyMetadata(double.NaN, OnChanged));

    public DaisyTourAnnotationPreset Preset { get => (DaisyTourAnnotationPreset)GetValue(PresetProperty); set => SetValue(PresetProperty, value); }
    public DaisyTourAnnotationKind Kind { get => (DaisyTourAnnotationKind)GetValue(KindProperty); set => SetValue(KindProperty, value); }
    public DaisyTourAnnotationPath Path { get => (DaisyTourAnnotationPath)GetValue(PathProperty); set => SetValue(PathProperty, value); }
    public DaisyTourArrowHead StartHead { get => (DaisyTourArrowHead)GetValue(StartHeadProperty); set => SetValue(StartHeadProperty, value); }
    public DaisyTourArrowHead EndHead { get => (DaisyTourArrowHead)GetValue(EndHeadProperty); set => SetValue(EndHeadProperty, value); }
    public DaisyTourArrowStyle ArrowStyle { get => (DaisyTourArrowStyle)GetValue(ArrowStyleProperty); set => SetValue(ArrowStyleProperty, value); }
    public DaisyTourStrokeStyle StrokeStyle { get => (DaisyTourStrokeStyle)GetValue(StrokeStyleProperty); set => SetValue(StrokeStyleProperty, value); }
    public DaisyTourAnnotationTail Tail { get => (DaisyTourAnnotationTail)GetValue(TailProperty); set => SetValue(TailProperty, value); }
    public Brush? Stroke { get => (Brush?)GetValue(StrokeProperty); set => SetValue(StrokeProperty, value); }
    public Brush? Fill { get => (Brush?)GetValue(FillProperty); set => SetValue(FillProperty, value); }
    public Brush? Foreground { get => (Brush?)GetValue(ForegroundProperty); set => SetValue(ForegroundProperty, value); }
    public DaisyTourDashCollection? DashArray { get => (DaisyTourDashCollection?)GetValue(DashArrayProperty); set => SetValue(DashArrayProperty, value); }
    public double StrokeThickness { get => (double)GetValue(StrokeThicknessProperty); set => SetValue(StrokeThicknessProperty, value); }
    public double DashOffset { get => (double)GetValue(DashOffsetProperty); set => SetValue(DashOffsetProperty, value); }
    public double HeadLength { get => (double)GetValue(HeadLengthProperty); set => SetValue(HeadLengthProperty, value); }
    public double HeadWidth { get => (double)GetValue(HeadWidthProperty); set => SetValue(HeadWidthProperty, value); }
    public double BodyWidth { get => (double)GetValue(BodyWidthProperty); set => SetValue(BodyWidthProperty, value); }
    public double Curvature { get => (double)GetValue(CurvatureProperty); set => SetValue(CurvatureProperty, value); }
    /// <summary>Bezier control points in connector coordinates: (0,0) is the start, (1,0) the end.
    /// Positive Y bends to the same side as a positive Curvature.</summary>
    public double ControlPoint1X { get => (double)GetValue(ControlPoint1XProperty); set => SetValue(ControlPoint1XProperty, value); }
    public double ControlPoint1Y { get => (double)GetValue(ControlPoint1YProperty); set => SetValue(ControlPoint1YProperty, value); }
    public double ControlPoint2X { get => (double)GetValue(ControlPoint2XProperty); set => SetValue(ControlPoint2XProperty, value); }
    public double ControlPoint2Y { get => (double)GetValue(ControlPoint2YProperty); set => SetValue(ControlPoint2YProperty, value); }
    public double SourceOffsetX { get => (double)GetValue(SourceOffsetXProperty); set => SetValue(SourceOffsetXProperty, value); }
    public double SourceOffsetY { get => (double)GetValue(SourceOffsetYProperty); set => SetValue(SourceOffsetYProperty, value); }
    public double TargetOffsetX { get => (double)GetValue(TargetOffsetXProperty); set => SetValue(TargetOffsetXProperty, value); }
    public double TargetOffsetY { get => (double)GetValue(TargetOffsetYProperty); set => SetValue(TargetOffsetYProperty, value); }
    public double OffsetX { get => (double)GetValue(OffsetXProperty); set => SetValue(OffsetXProperty, value); }
    public double OffsetY { get => (double)GetValue(OffsetYProperty); set => SetValue(OffsetYProperty, value); }
    public double Width { get => (double)GetValue(WidthProperty); set => SetValue(WidthProperty, value); }
    public double Height { get => (double)GetValue(HeightProperty); set => SetValue(HeightProperty, value); }
    public double MaxWidth { get => (double)GetValue(MaxWidthProperty); set => SetValue(MaxWidthProperty, value); }
    public double MaxHeight { get => (double)GetValue(MaxHeightProperty); set => SetValue(MaxHeightProperty, value); }
    public double FontSize { get => (double)GetValue(FontSizeProperty); set => SetValue(FontSizeProperty, value); }
    public double CornerRadius { get => (double)GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }
    public double TailWidth { get => (double)GetValue(TailWidthProperty); set => SetValue(TailWidthProperty, value); }
    public double TailLength { get => (double)GetValue(TailLengthProperty); set => SetValue(TailLengthProperty, value); }
    public double Opacity { get => (double)GetValue(OpacityProperty); set => SetValue(OpacityProperty, value); }

    internal event EventHandler? Changed;

    private static void OnChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var style = (DaisyTourAnnotationStyle)sender;
        if (args.Property == DashArrayProperty)
        {
            if (args.OldValue is DaisyTourDashCollection oldPattern)
                oldPattern.CollectionChanged -= style.OnDashPatternChanged;
            if (args.NewValue is DaisyTourDashCollection newPattern)
                newPattern.CollectionChanged += style.OnDashPatternChanged;
        }
        style.NotifyChanged();
    }

    private void OnDashPatternChanged(object? sender, NotifyCollectionChangedEventArgs args) => NotifyChanged();
    protected void NotifyChanged() => Changed?.Invoke(this, EventArgs.Empty);
}
