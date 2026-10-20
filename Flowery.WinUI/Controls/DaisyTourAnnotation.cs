using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace Flowery.Controls;

/// <summary>A live annotation anchored to the current bounds of one or two controls.</summary>
public sealed partial class DaisyTourAnnotation : DaisyTourAnnotationStyle
{
    public static readonly DependencyProperty IdProperty = DependencyProperty.Register(
        nameof(Id), typeof(string), typeof(DaisyTourAnnotation), new PropertyMetadata(string.Empty, OnAnnotationChanged));
    public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
        nameof(Source), typeof(FrameworkElement), typeof(DaisyTourAnnotation), new PropertyMetadata(null, OnAnnotationChanged));
    public static readonly DependencyProperty TargetProperty = DependencyProperty.Register(
        nameof(Target), typeof(FrameworkElement), typeof(DaisyTourAnnotation), new PropertyMetadata(null, OnAnnotationChanged));
    public static readonly DependencyProperty SourceAnchorProperty = DependencyProperty.Register(
        nameof(SourceAnchor), typeof(DaisyTourAnchor), typeof(DaisyTourAnnotation), new PropertyMetadata(DaisyTourAnchor.Auto, OnAnnotationChanged));
    public static readonly DependencyProperty TargetAnchorProperty = DependencyProperty.Register(
        nameof(TargetAnchor), typeof(DaisyTourAnchor), typeof(DaisyTourAnnotation), new PropertyMetadata(DaisyTourAnchor.Auto, OnAnnotationChanged));
    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text), typeof(string), typeof(DaisyTourAnnotation), new PropertyMetadata(string.Empty, OnAnnotationChanged));
    public static readonly DependencyProperty ContentProperty = DependencyProperty.Register(
        nameof(Content), typeof(object), typeof(DaisyTourAnnotation), new PropertyMetadata(null, OnAnnotationChanged));
    public static readonly DependencyProperty GeometryProperty = DependencyProperty.Register(
        nameof(Geometry), typeof(Geometry), typeof(DaisyTourAnnotation), new PropertyMetadata(null, OnAnnotationChanged));
    public static readonly DependencyProperty TemplateProperty = DependencyProperty.Register(
        nameof(Template), typeof(DataTemplate), typeof(DaisyTourAnnotation), new PropertyMetadata(null, OnAnnotationChanged));

    public static readonly DependencyProperty RouteLimitProperty = DependencyProperty.Register(
        nameof(RouteLimit), typeof(FrameworkElement), typeof(DaisyTourAnnotation), new PropertyMetadata(null, OnAnnotationChanged));
    public static readonly DependencyProperty RouteLimitCoordinateProperty = DependencyProperty.Register(
        nameof(RouteLimitCoordinate), typeof(double), typeof(DaisyTourAnnotation), new PropertyMetadata(double.NaN, OnAnnotationChanged));
    public static readonly DependencyProperty RouteLimitSideProperty = DependencyProperty.Register(
        nameof(RouteLimitSide), typeof(DaisyTourRouteSide), typeof(DaisyTourAnnotation), new PropertyMetadata(DaisyTourRouteSide.None, OnAnnotationChanged));
    public static readonly DependencyProperty RouteLimitMarginProperty = DependencyProperty.Register(
        nameof(RouteLimitMargin), typeof(double), typeof(DaisyTourAnnotation), new PropertyMetadata(12d, OnAnnotationChanged));

    public string Id { get => (string)GetValue(IdProperty); set => SetValue(IdProperty, value); }
    /// <summary>A control whose edge, plus RouteLimitMargin, limits the connector's course.</summary>
    public FrameworkElement? RouteLimit { get => (FrameworkElement?)GetValue(RouteLimitProperty); set => SetValue(RouteLimitProperty, value); }
    /// <summary>A fixed window coordinate in DIP that limits the course when RouteLimit is not set:
    /// y for Above and Below, x for LeftOf and RightOf.</summary>
    public double RouteLimitCoordinate { get => (double)GetValue(RouteLimitCoordinateProperty); set => SetValue(RouteLimitCoordinateProperty, value); }
    /// <summary>The side of the limit that the course between both ends must stay on.</summary>
    public DaisyTourRouteSide RouteLimitSide { get => (DaisyTourRouteSide)GetValue(RouteLimitSideProperty); set => SetValue(RouteLimitSideProperty, value); }
    public double RouteLimitMargin { get => (double)GetValue(RouteLimitMarginProperty); set => SetValue(RouteLimitMarginProperty, value); }
    public FrameworkElement? Source { get => (FrameworkElement?)GetValue(SourceProperty); set => SetValue(SourceProperty, value); }
    public FrameworkElement? Target { get => (FrameworkElement?)GetValue(TargetProperty); set => SetValue(TargetProperty, value); }
    public DaisyTourAnchor SourceAnchor { get => (DaisyTourAnchor)GetValue(SourceAnchorProperty); set => SetValue(SourceAnchorProperty, value); }
    public DaisyTourAnchor TargetAnchor { get => (DaisyTourAnchor)GetValue(TargetAnchorProperty); set => SetValue(TargetAnchorProperty, value); }
    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }
    public object? Content { get => (object?)GetValue(ContentProperty); set => SetValue(ContentProperty, value); }
    public Geometry? Geometry { get => (Geometry?)GetValue(GeometryProperty); set => SetValue(GeometryProperty, value); }
    public DataTemplate? Template { get => (DataTemplate?)GetValue(TemplateProperty); set => SetValue(TemplateProperty, value); }

    private static void OnAnnotationChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        => ((DaisyTourAnnotation)sender).NotifyChanged();
}
