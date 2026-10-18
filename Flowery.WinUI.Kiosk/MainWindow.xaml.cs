namespace Flowery.WinUI.Kiosk;

public sealed partial class MainWindow : Window
{
    private const int MaximumWindowWidth = 1920;
    private const int MaximumWindowHeight = 1080;

    public MainWindow(bool manual)
    {
        InitializeComponent();
        Title = "Flowery.WinUI Kiosk";
        LimitWindowSize();
        MainPage.ConfigureManualMode(manual);
        Closed += OnClosed;
    }

    internal KioskMainPage KioskPage => MainPage;

    private void LimitWindowSize()
    {
        if (AppWindow.Presenter is not Microsoft.UI.Windowing.OverlappedPresenter presenter)
            return;

        presenter.PreferredMaximumWidth = MaximumWindowWidth;
        presenter.PreferredMaximumHeight = MaximumWindowHeight;
    }

    private async void OnClosed(object sender, WindowEventArgs args)
    {
        Closed -= OnClosed;
        await MainPage.ShutdownAsync();
        FlowerySizeManager.MainWindow = null;
        FloweryScaleManager.MainWindow = null;
    }
}
