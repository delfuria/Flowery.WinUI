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

    public string Id { get => (string)GetValue(IdProperty); set => SetValue(IdProperty, value); }
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
