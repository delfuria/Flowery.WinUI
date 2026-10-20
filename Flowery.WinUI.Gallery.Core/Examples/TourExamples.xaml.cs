using System.Threading;
using System.Threading.Tasks;
using Flowery.WinUI.Gallery.Localization;

namespace Flowery.WinUI.Gallery.Examples;

public sealed partial class TourExamples : ScrollableExamplePage
{
    private readonly DaisyTourStep[] _standardSteps;

    public TourExamples()
    {
        InitializeComponent();
        _standardSteps = Tour.Steps.ToArray();
        BindText(_standardSteps[0], DaisyTourStep.TitleProperty, "Gallery_Tour_WelcomeTitle");
        BindText(_standardSteps[0], DaisyTourStep.DescriptionProperty, "Gallery_Tour_WelcomeDescription");
        BindText(_standardSteps[1], DaisyTourStep.TitleProperty, "Gallery_Tour_ScrollTitle");
        BindText(_standardSteps[1], DaisyTourStep.DescriptionProperty, "Gallery_Tour_ScrollDescription");
        BindText(_standardSteps[2], DaisyTourStep.TitleProperty, "Gallery_Tour_DeferredTitle");
        BindText(_standardSteps[2], DaisyTourStep.DescriptionProperty, "Gallery_Tour_DeferredDescription");
        _standardSteps[0].PrepareAsync = PrepareOverviewAsync;
        _standardSteps[1].PrepareAsync = PrepareOverviewAsync;
        _standardSteps[2].PrepareAsync = PrepareDetailsAsync;
    }

    public GalleryLocalization Localization => GalleryLocalization.Instance;
    protected override ScrollViewer? ScrollViewer => MainScrollViewer;

    private async void StartTour_Click(object sender, RoutedEventArgs e)
    {
        Tour.Steps.Clear();
        foreach (var step in _standardSteps)
            Tour.Steps.Add(step);
        await Tour.StartAsync();
    }

    private async void Arrows_Click(object sender, RoutedEventArgs e)
    {
        var presets = new[]
        {
            DaisyTourAnnotationPreset.ThinArrow,
            DaisyTourAnnotationPreset.ThickArrow,
            DaisyTourAnnotationPreset.BlockArrow,
            DaisyTourAnnotationPreset.CurvedArrow,
            DaisyTourAnnotationPreset.TwoEndedArrow
        };
        Tour.Steps.Clear();
        foreach (var preset in presets)
        {
            var annotation = new DaisyTourAnnotation { Preset = preset };
            if (preset == DaisyTourAnnotationPreset.BlockArrow)
            {
                annotation.Stroke = new SolidColorBrush(Colors.OrangeRed);
                annotation.Fill = new SolidColorBrush(Colors.Gold);
            }
            if (preset == DaisyTourAnnotationPreset.CurvedArrow)
            {
                annotation.StartHead = DaisyTourArrowHead.Circle;
                annotation.EndHead = DaisyTourArrowHead.Open;
            }
            AddAnnotationStep(preset.ToString(), annotation, connectTargets: true);
        }
        await Tour.StartAsync();
    }

    private async void Lines_Click(object sender, RoutedEventArgs e)
    {
        Tour.Steps.Clear();
        var styles = new[]
        {
            DaisyTourStrokeStyle.Solid,
            DaisyTourStrokeStyle.Dotted,
            DaisyTourStrokeStyle.Dashed,
            DaisyTourStrokeStyle.DashDot,
            DaisyTourStrokeStyle.Custom
        };
        foreach (var style in styles)
        {
            var annotation = new DaisyTourAnnotation
            {
                Kind = DaisyTourAnnotationKind.Line,
                StrokeStyle = style,
                Path = style switch
                {
                    DaisyTourStrokeStyle.Dashed => DaisyTourAnnotationPath.Elbow,
                    DaisyTourStrokeStyle.Dotted => DaisyTourAnnotationPath.Curved,
                    _ => DaisyTourAnnotationPath.Straight
                },
                StartHead = DaisyTourArrowHead.None,
                EndHead = DaisyTourArrowHead.None,
                StrokeThickness = 4,
                DashArray = style == DaisyTourStrokeStyle.Custom ? new DaisyTourDashCollection { 6, 2, 1, 2, 1, 2 } : null,
                DashOffset = 1
            };
            AddAnnotationStep(style.ToString(), annotation, connectTargets: true);
        }
        await Tour.StartAsync();
    }

    private async void Bubbles_Click(object sender, RoutedEventArgs e)
    {
        Tour.Steps.Clear();
        AddBubbleSteps(DaisyTourAnnotationKind.Cloud, DaisyTourAnnotationTail.Thought);
        AddBubbleSteps(DaisyTourAnnotationKind.SpeechBubble, DaisyTourAnnotationTail.Speech);
        await Tour.StartAsync();
    }

    private void AddBubbleSteps(DaisyTourAnnotationKind kind, DaisyTourAnnotationTail tail)
    {
        AddBubbleStep(kind, DaisyTourAnnotationTail.None, hasText: false);
        AddBubbleStep(kind, tail, hasText: false);
        AddBubbleStep(kind, DaisyTourAnnotationTail.None, hasText: true);
        AddBubbleStep(kind, tail, hasText: true);
    }

    private void AddBubbleStep(DaisyTourAnnotationKind kind, DaisyTourAnnotationTail tail, bool hasText)
    {
        var annotation = new DaisyTourAnnotation
        {
            Kind = kind,
            Tail = tail,
            Width = hasText ? 230 : 90,
            Height = hasText ? 130 : 70,
            MaxWidth = 240,
            CornerRadius = tail == DaisyTourAnnotationTail.None ? 0 : 20
        };
        if (hasText)
            BindText(annotation, DaisyTourAnnotation.TextProperty, "Gallery_Tour_BubbleText");
        AddAnnotationStep($"{kind} / {tail} / {(hasText ? "Text" : "No text")}", annotation);
    }

    private async void Custom_Click(object sender, RoutedEventArgs e)
    {
        Tour.Steps.Clear();
        var annotation = new DaisyTourAnnotation
        {
            Kind = DaisyTourAnnotationKind.Custom,
            Geometry = new EllipseGeometry { Center = new Point(60, 35), RadiusX = 58, RadiusY = 33 },
            Width = 120,
            Height = 70,
            StrokeThickness = 5,
            Stroke = new SolidColorBrush(Colors.OrangeRed),
            Fill = new SolidColorBrush(Colors.Transparent)
        };
        AddAnnotationStep("Custom", annotation);
        BindText(Tour.Steps[0], DaisyTourStep.DescriptionProperty, "Gallery_Tour_CustomDescription");
        await Tour.StartAsync();
    }

    private void AddAnnotationStep(string title, DaisyTourAnnotation annotation, bool connectTargets = false)
    {
        var step = new DaisyTourStep
        {
            Id = $"annotation-{Tour.Steps.Count}",
            Title = title,
            Target = PrimaryTarget,
            PrepareAsync = PrepareOverviewAsync,
            AnnotationDefaults = new DaisyTourAnnotationStyle { Opacity = 0.95 }
        };
        BindText(step, DaisyTourStep.DescriptionProperty, "Gallery_Tour_AnnotationDescription");
        annotation.Id = $"shape-{Tour.Steps.Count}";
        annotation.Target = connectTargets ? RelatedTarget : PrimaryTarget;
        annotation.Source = connectTargets ? PrimaryTarget : null;
        step.Annotations.Add(annotation);
        Tour.Steps.Add(step);
    }

    private Task PrepareOverviewAsync(DaisyTourStep step, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        DemoTabs.SelectedItem = OverviewTab;
        return Task.CompletedTask;
    }

    private Task PrepareDetailsAsync(DaisyTourStep step, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (DetailsTab.Content is not DaisyButton)
        {
            var target = new DaisyButton
            {
                Margin = new Thickness(16),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Variant = DaisyButtonVariant.Primary
            };
            BindText(target, ContentControl.ContentProperty, "Gallery_Tour_Target");
            DetailsTab.Content = target;
        }
        DemoTabs.SelectedItem = DetailsTab;
        step.Target = (DaisyButton)DetailsTab.Content;
        return Task.CompletedTask;
    }

    private void Tour_Closed(object? sender, DaisyTourClosedEventArgs e)
    {
        OutcomeText.Text = string.Format(CultureInfo.CurrentCulture,
            GalleryLocalization.GetString("Gallery_Tour_Result"), e.Reason, e.StepId ?? "-", e.AnnotationId ?? "-");
    }

    private void BindText(DependencyObject target, DependencyProperty property, string key)
    {
        BindingOperations.SetBinding(target, property, new Binding
        {
            Source = Localization,
            Path = new PropertyPath($"[{key}]"),
            Mode = BindingMode.OneWay
        });
    }
}
