using System.Threading.Tasks;

namespace Flowery.WinUI.Kiosk.Slides;

public sealed partial class DataSlide : UserControl
{
    private readonly Random _random = new();
    private DispatcherTimer? _updates;
    private Storyboard? _entrance;
    private TaskCompletionSource _firstUpdate = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public DataSlide()
    {
        InitializeComponent();
        SessionFlow.Value = 1280m;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    internal Task FirstUpdate => _firstUpdate.Task;
    internal bool IsUpdateTimerRunning => _updates is not null;

    private void OnLoaded(object sender, RoutedEventArgs args)
    {
        StopAnimations();
        _firstUpdate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        RunEntranceAnimation();
        _updates = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _updates.Tick += OnUpdate;
        _updates.Start();
    }

    private void OnUpdate(object? sender, object args)
    {
        var cpu = _random.Next(35, 85);
        var ram = _random.Next(60, 92);
        var io = _random.Next(10, 45);
        CpuProgress.Value = cpu;
        CpuText.Text = $"{cpu}%";
        RamProgress.Value = ram;
        RamText.Text = $"{ram}%";
        IoProgress.Value = io;
        IoText.Text = $"{io}%";
        SessionFlow.Value += _random.Next(1, 15);
        HealthProgress.Value = 100 - ((cpu + ram) / 10);
        _firstUpdate.TrySetResult();
    }

    private void RunEntranceAnimation()
    {
        _entrance = new Storyboard();
        AddPopIn(_entrance, HealthProgress.Parent as FrameworkElement, -40, 0);
        AddPopIn(_entrance, ResourceCard, 40, 0.2);
        _entrance.Begin();
    }

    private static void AddPopIn(Storyboard storyboard, FrameworkElement? element, double offset, double delay)
    {
        if (element is null)
            return;
        element.Opacity = 0;
        var transform = FloweryAnimationHelpers.EnsureCompositeTransform(element);
        transform.TranslateX = offset;
        var move = new DoubleAnimation
        {
            To = 0,
            Duration = new Duration(TimeSpan.FromSeconds(1.2)),
            EasingFunction = new ElasticEase { Oscillations = 1, Springiness = 6 },
            BeginTime = TimeSpan.FromSeconds(delay),
            EnableDependentAnimation = true
        };
        Storyboard.SetTarget(move, element);
        Storyboard.SetTargetProperty(move, "(UIElement.RenderTransform).(CompositeTransform.TranslateX)");
        var fade = new DoubleAnimation
        {
            To = 1,
            Duration = new Duration(TimeSpan.FromSeconds(0.8)),
            BeginTime = TimeSpan.FromSeconds(delay),
            EnableDependentAnimation = true
        };
        Storyboard.SetTarget(fade, element);
        Storyboard.SetTargetProperty(fade, "Opacity");
        storyboard.Children.Add(move);
        storyboard.Children.Add(fade);
    }

    private void OnUnloaded(object sender, RoutedEventArgs args)
    {
        StopAnimations();
        _firstUpdate.TrySetCanceled();
    }

    private void StopAnimations()
    {
        if (_updates is not null)
        {
            _updates.Stop();
            _updates.Tick -= OnUpdate;
            _updates = null;
        }
        _entrance?.Stop();
        _entrance = null;
    }
}
