using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using Flowery.Controls.ColorPicker;
using PickerColorChangedEventArgs = Flowery.Controls.ColorPicker.ColorChangedEventArgs;

namespace Flowery.WinUI.Gallery.Examples
{
    public sealed partial class ColorPickerExamples : ScrollableExamplePage
    {
        // Custom colors persist across dialog openings for this page.
        private readonly ColorCollection _customColors = ColorCollection.CreateCustom(16);

        public bool IsScreenPickerSupported => !OperatingSystem.IsAndroid() && !OperatingSystem.IsIOS();

        public ColorPickerExamples()
        {
            InitializeComponent();
        }

        protected override ScrollViewer? ScrollViewer => MainScrollViewer;

        private static string FormatRgbHex(Color color)
        {
            return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }

        private void ColorWheel1_ColorChanged(object? sender, PickerColorChangedEventArgs e)
        {
            if (ColorWheelValue1 != null)
                ColorWheelValue1.Text = $"Color: {FormatRgbHex(e.Color)}";
        }

        private void ColorGrid1_ColorChanged(object? sender, PickerColorChangedEventArgs e)
        {
            if (ColorGridValue1 != null)
                ColorGridValue1.Text = $"Selected: {FormatRgbHex(e.Color)}";
        }

        private void ScreenPicker1_ColorChanged(object? sender, PickerColorChangedEventArgs e)
        {
            if (ScreenPickerValue1 != null)
                ScreenPickerValue1.Text = $"Picked: {FormatRgbHex(e.Color)}";
        }

        private async void OpenDialogButton_Click(object? sender, RoutedEventArgs e)
        {
            var currentColor = SelectedColorPreview?.Background is SolidColorBrush brush
                ? brush.Color
                : Colors.Red;

            if (XamlRoot is not { } xamlRoot)
                return;

            var result = await DaisyColorPickerDialog.ShowAsync(xamlRoot, currentColor, showAlphaChannel: true, _customColors);
            if (result is not { } color)
                return;

            if (SelectedColorPreview != null)
                SelectedColorPreview.Background = new SolidColorBrush(color);

            if (SelectedColorText != null)
                SelectedColorText.Text = $"Selected: {FormatRgbHex(color)}";
        }
    }
}
