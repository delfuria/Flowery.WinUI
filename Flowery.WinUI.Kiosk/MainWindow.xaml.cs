namespace Flowery.WinUI.Kiosk;

public sealed partial class MainWindow : Window
{
    public MainWindow(bool manual)
    {
        InitializeComponent();
        Title = "Flowery.WinUI Kiosk";
        MainPage.ConfigureManualMode(manual);
        Closed += OnClosed;
    }

    internal KioskMainPage KioskPage => MainPage;

    private async void OnClosed(object sender, WindowEventArgs args)
    {
        Closed -= OnClosed;
        await MainPage.ShutdownAsync();
        FlowerySizeManager.MainWindow = null;
        FloweryScaleManager.MainWindow = null;
    }
}
