using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Flowery.Enums;
using Flowery.Helpers;
using Flowery.Theming;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using Colors = Microsoft.UI.Colors;

namespace Flowery.Controls.ColorPicker
{
    /// <summary>
    /// A modal color picker dialog that combines the color wheel, lightness slider, screen color picker,
    /// palette grid and color editor, with Undo/Redo over the colors chosen while it is open.
    /// Use <see cref="ShowAsync(XamlRoot, Color)"/> to show it; the task returns the selected color, or null when cancelled.
    /// </summary>
    public partial class DaisyColorPickerDialog : FloweryDialogBase
    {
        internal const double ColorWheelSize = 180;
        internal const double SliderHeight = 18;
        internal const double ColorPreviewHeight = 22;
        internal const double ButtonWidth = 80;
        internal const double PanelSpacing = 8;
        internal const double TinySpacing = 4;
        internal const double LeftPanelRightMargin = 12;
        internal const int LabelFontSize = 11;
        internal const int SmallFontSize = 10;
        internal const int ColorGridColumns = 16;
        internal const int ColorHistoryCapacity = 50;
        internal const double ColorGridCellSize = 12;
        internal const double ColorGridSpacing = 2;
        internal const string UndoButtonName = "PART_UndoButton";
        internal const string RedoButtonName = "PART_RedoButton";

        /// <summary>
        /// Dialog-local color history: the latest <see cref="ColorHistoryCapacity"/> values, with Undo and Redo.
        /// </summary>
        internal sealed class DialogColorHistory
        {
            private readonly List<Color> _colors = new(ColorHistoryCapacity);
            private int _index = -1;

            public bool CanUndo => _index > 0;
            public bool CanRedo => _index >= 0 && _index < _colors.Count - 1;
            public int Count => _colors.Count;

            public void Reset(Color color)
            {
                _colors.Clear();
                _colors.Add(color);
                _index = 0;
            }

            public void Record(Color color)
            {
                if (_index >= 0 && _colors[_index].Equals(color)) return;

                if (_index < _colors.Count - 1)
                {
                    _colors.RemoveRange(_index + 1, _colors.Count - _index - 1);
                }

                _colors.Add(color);
                if (_colors.Count > ColorHistoryCapacity)
                {
                    _colors.RemoveAt(0);
                }

                _index = _colors.Count - 1;
            }

            public bool TryUndo(out Color color)
            {
                if (!CanUndo)
                {
                    color = default;
                    return false;
                }

                color = _colors[--_index];
                return true;
            }

            public bool TryRedo(out Color color)
            {
                if (!CanRedo)
                {
                    color = default;
                    return false;
                }

                color = _colors[++_index];
                return true;
            }
        }

        private readonly DialogColorHistory _colorHistory = new();
        private TaskCompletionSource<Color?>? _tcs;
        private XamlRoot? _xamlRoot;
        private Panel? _hostPanel;
        private long _isOpenCallbackToken;
        private bool _isClosing;
        private bool _lockUpdates;
        private bool _isApplyingColorHistory;
        private bool _isColorHistoryInitialized;

        private readonly DaisyColorWheel _colorWheel;
        private readonly DaisyColorSlider _lightnessSlider;
        private readonly DaisyScreenColorPicker _screenColorPicker;
        private readonly FrameworkElement _screenColorPickerSection;
        private readonly DaisyColorGrid _colorGrid;
        private readonly FrameworkElement _colorGridSection;
        private readonly DaisyColorEditor _colorEditor;
        private readonly FrameworkElement _colorEditorSection;
        private readonly FrameworkElement _leftPanel;
        private readonly Border _colorPreview;
        private readonly Border _originalColorPreview;
        private readonly TextBlock _originalColorHexText;
        private readonly DaisyButton _undoButton;
        private readonly DaisyButton _redoButton;
        private readonly DaisyButton _okButton;
        private readonly DaisyButton _cancelButton;

        #region Dependency Properties

        public static readonly DependencyProperty ColorProperty =
            DependencyProperty.Register(nameof(Color), typeof(Color), typeof(DaisyColorPickerDialog),
                new PropertyMetadata(Colors.Red, OnColorChanged));

        /// <summary>The currently selected color.</summary>
        public Color Color
        {
            get => (Color)GetValue(ColorProperty);
            set => SetValue(ColorProperty, value);
        }

        public static readonly DependencyProperty OriginalColorProperty =
            DependencyProperty.Register(nameof(OriginalColor), typeof(Color), typeof(DaisyColorPickerDialog),
                new PropertyMetadata(Colors.Red, OnOriginalColorChanged));

        /// <summary>The color the dialog was opened with, shown next to the new color.</summary>
        public Color OriginalColor
        {
            get => (Color)GetValue(OriginalColorProperty);
            set => SetValue(OriginalColorProperty, value);
        }

        public static readonly DependencyProperty CustomColorsProperty =
            DependencyProperty.Register(nameof(CustomColors), typeof(ColorCollection), typeof(DaisyColorPickerDialog),
                new PropertyMetadata(null, OnCustomColorsChanged));

        /// <summary>Custom colors shown below the palette. OK adds the selected color to them.</summary>
        public ColorCollection? CustomColors
        {
            get => (ColorCollection?)GetValue(CustomColorsProperty);
            set => SetValue(CustomColorsProperty, value);
        }

        public static readonly DependencyProperty ShowAlphaChannelProperty =
            DependencyProperty.Register(nameof(ShowAlphaChannel), typeof(bool), typeof(DaisyColorPickerDialog),
                new PropertyMetadata(true, OnLayoutOptionChanged));

        /// <summary>Whether the editor shows the alpha channel.</summary>
        public bool ShowAlphaChannel
        {
            get => (bool)GetValue(ShowAlphaChannelProperty);
            set => SetValue(ShowAlphaChannelProperty, value);
        }

        public static readonly DependencyProperty ShowColorWheelProperty =
            DependencyProperty.Register(nameof(ShowColorWheel), typeof(bool), typeof(DaisyColorPickerDialog),
                new PropertyMetadata(true, OnLayoutOptionChanged));

        /// <summary>Whether the color wheel and lightness slider are shown.</summary>
        public bool ShowColorWheel
        {
            get => (bool)GetValue(ShowColorWheelProperty);
            set => SetValue(ShowColorWheelProperty, value);
        }

        public static readonly DependencyProperty ShowColorGridProperty =
            DependencyProperty.Register(nameof(ShowColorGrid), typeof(bool), typeof(DaisyColorPickerDialog),
                new PropertyMetadata(true, OnLayoutOptionChanged));

        /// <summary>Whether the palette grid is shown.</summary>
        public bool ShowColorGrid
        {
            get => (bool)GetValue(ShowColorGridProperty);
            set => SetValue(ShowColorGridProperty, value);
        }

        public static readonly DependencyProperty ShowColorEditorProperty =
            DependencyProperty.Register(nameof(ShowColorEditor), typeof(bool), typeof(DaisyColorPickerDialog),
                new PropertyMetadata(true, OnLayoutOptionChanged));

        /// <summary>Whether the RGB/HSL/hex editor is shown.</summary>
        public bool ShowColorEditor
        {
            get => (bool)GetValue(ShowColorEditorProperty);
            set => SetValue(ShowColorEditorProperty, value);
        }

        public static readonly DependencyProperty ShowScreenColorPickerProperty =
            DependencyProperty.Register(nameof(ShowScreenColorPicker), typeof(bool), typeof(DaisyColorPickerDialog),
                new PropertyMetadata(true, OnLayoutOptionChanged));

        /// <summary>
        /// Whether the screen color picker is shown. It is always hidden on Android and iOS, where screen capture is unavailable.
        /// </summary>
        public bool ShowScreenColorPicker
        {
            get => (bool)GetValue(ShowScreenColorPickerProperty);
            set => SetValue(ShowScreenColorPickerProperty, value);
        }

        public static readonly DependencyProperty PreserveAlphaChannelProperty =
            DependencyProperty.Register(nameof(PreserveAlphaChannel), typeof(bool), typeof(DaisyColorPickerDialog),
                new PropertyMetadata(false));

        /// <summary>When true, choosing a palette color keeps the current alpha value.</summary>
        public bool PreserveAlphaChannel
        {
            get => (bool)GetValue(PreserveAlphaChannelProperty);
            set => SetValue(PreserveAlphaChannelProperty, value);
        }

        #endregion

        /// <summary>Occurs when the selected color changes while the dialog is open.</summary>
        public event EventHandler<ColorChangedEventArgs>? PreviewColorChanged;

        /// <summary>Whether Undo is currently possible.</summary>
        public bool CanUndo => _colorHistory.CanUndo;

        /// <summary>Whether Redo is currently possible.</summary>
        public bool CanRedo => _colorHistory.CanRedo;

        public DaisyColorPickerDialog()
        {
            IsDialogResizingEnabled = false;

            _colorWheel = new DaisyColorWheel { Width = ColorWheelSize, Height = ColorWheelSize };
            _colorWheel.ColorChanged += OnColorWheelColorChanged;

            _lightnessSlider = new DaisyColorSlider
            {
                Channel = ColorSliderChannel.Lightness,
                Width = ColorWheelSize,
                Height = SliderHeight
            };
            _lightnessSlider.ColorChanged += OnLightnessSliderChanged;

            _screenColorPicker = new DaisyScreenColorPicker { Width = ColorWheelSize };
            _screenColorPicker.ColorChanged += OnComponentColorChanged;

            _colorGrid = new DaisyColorGrid
            {
                Palette = ColorCollection.Paint,
                ShowCustomColors = true,
                CellSize = new Windows.Foundation.Size(ColorGridCellSize, ColorGridCellSize),
                Columns = ColorGridColumns,
                Spacing = new Windows.Foundation.Size(ColorGridSpacing, ColorGridSpacing)
            };
            _colorGrid.ColorChanged += OnColorGridColorChanged;

            _colorEditor = new DaisyColorEditor
            {
                ShowAlphaChannel = ShowAlphaChannel,
                ShowHexInput = true,
                ShowRgbSliders = true,
                ShowHslSliders = true
            };
            _colorEditor.ColorChanged += OnComponentColorChanged;

            _originalColorHexText = new TextBlock
            {
                FontSize = SmallFontSize,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            _originalColorPreview = CreatePreviewBorder();
            _originalColorPreview.Child = _originalColorHexText;
            _colorPreview = CreatePreviewBorder();

            _undoButton = CreateHistoryButton(UndoButtonName, L("ColorPicker_Undo"), mirrorIcon: true);
            _undoButton.Click += (_, _) => Undo();
            _redoButton = CreateHistoryButton(RedoButtonName, L("ColorPicker_Redo"), mirrorIcon: false);
            _redoButton.Click += (_, _) => Redo();

            _okButton = new DaisyButton { Content = L("Common_Ok"), Variant = DaisyButtonVariant.Primary, Size = DaisySize.Small, MinWidth = ButtonWidth };
            _okButton.Click += (_, _) => Confirm();
            _cancelButton = new DaisyButton { Content = L("Common_Cancel"), Size = DaisySize.Small, MinWidth = ButtonWidth };
            _cancelButton.Click += (_, _) => Cancel();

            Content = BuildLayout(
                out _leftPanel,
                out _screenColorPickerSection,
                out _colorGridSection,
                out _colorEditorSection);

            CustomColors = ColorCollection.CreateCustom(ColorGridColumns);
            ApplyLayoutOptions();
            UpdateAllControls();
            UpdateHistoryButtonStates();
        }

        /// <summary>
        /// Shows the color picker dialog and returns the selected color, or null if cancelled.
        /// </summary>
        /// <param name="xamlRoot">The XamlRoot of the element that opens the dialog.</param>
        /// <param name="initialColor">The initial color to display.</param>
        public static Task<Color?> ShowAsync(XamlRoot xamlRoot, Color initialColor) =>
            ShowAsync(xamlRoot, initialColor, showAlphaChannel: true, customColors: null);

        /// <summary>
        /// Shows the color picker dialog with options and returns the selected color, or null if cancelled.
        /// </summary>
        /// <param name="xamlRoot">The XamlRoot of the element that opens the dialog.</param>
        /// <param name="initialColor">The initial color to display.</param>
        /// <param name="showAlphaChannel">Whether the editor shows the alpha channel.</param>
        /// <param name="customColors">Custom colors to show and extend; a new collection is used when null.</param>
        public static Task<Color?> ShowAsync(XamlRoot xamlRoot, Color initialColor, bool showAlphaChannel, ColorCollection? customColors)
        {
            var dialog = new DaisyColorPickerDialog
            {
                Color = initialColor,
                OriginalColor = initialColor,
                ShowAlphaChannel = showAlphaChannel
            };

            if (customColors != null)
            {
                dialog.CustomColors = customColors;
            }

            return dialog.ShowAsync(xamlRoot);
        }

        /// <summary>
        /// Shows this dialog instance. The task returns the selected color, or null if cancelled.
        /// </summary>
        public Task<Color?> ShowAsync(XamlRoot xamlRoot)
        {
            ArgumentNullException.ThrowIfNull(xamlRoot);
            if (_tcs != null)
                throw new InvalidOperationException("The dialog is already shown.");

            var tcs = new TaskCompletionSource<Color?>();
            _tcs = tcs;
            _xamlRoot = xamlRoot;
            _isClosing = false;
            ApplySmartSizingWithAutoHeight(xamlRoot);

            _hostPanel = FloweryDialogHost.EnsureHost(xamlRoot);
            if (_hostPanel == null)
            {
                Finish(null);
                return tcs.Task;
            }

            InitializeColorHistory();
            _isOpenCallbackToken = RegisterPropertyChangedCallback(IsOpenProperty, OnIsOpenChanged);
            _hostPanel.Children.Add(this);
            IsOpen = true;
            return tcs.Task;
        }

        /// <summary>Steps back to the previous color in the dialog history.</summary>
        public bool Undo()
        {
            if (!_colorHistory.TryUndo(out var color)) return false;
            ApplyHistoryColor(color);
            return true;
        }

        /// <summary>Steps forward to the next color in the dialog history.</summary>
        public bool Redo()
        {
            if (!_colorHistory.TryRedo(out var color)) return false;
            ApplyHistoryColor(color);
            return true;
        }

        /// <summary>Closes the dialog and returns the selected color; adds it to the custom colors.</summary>
        public void Confirm()
        {
            if (CustomColors != null)
            {
                _colorGrid.AddCustomColor(Color);
            }

            Finish(Color);
        }

        /// <summary>Closes the dialog without a result.</summary>
        public void Cancel() => Finish(null);

        internal void InitializeColorHistory()
        {
            _colorHistory.Reset(Color);
            _isColorHistoryInitialized = true;
            UpdateHistoryButtonStates();
        }

        protected virtual void OnPreviewColorChanged(ColorChangedEventArgs e)
        {
            PreviewColorChanged?.Invoke(this, e);
        }

        private static void OnColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not DaisyColorPickerDialog dialog || dialog._lockUpdates) return;

            var color = (Color)e.NewValue;
            if (dialog._isColorHistoryInitialized && !dialog._isApplyingColorHistory)
            {
                dialog._colorHistory.Record(color);
            }

            dialog.UpdateHistoryButtonStates();
            dialog.UpdateAllControls();
            dialog.OnPreviewColorChanged(new ColorChangedEventArgs(color));
        }

        private static void OnOriginalColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyColorPickerDialog dialog)
                dialog.UpdateOriginalPreview();
        }

        private static void OnCustomColorsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyColorPickerDialog dialog)
                dialog._colorGrid.CustomColors = e.NewValue as ColorCollection;
        }

        private static void OnLayoutOptionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyColorPickerDialog dialog)
                dialog.ApplyLayoutOptions();
        }

        private void ApplyLayoutOptions()
        {
            if (_colorEditor == null) return;

            _colorEditor.ShowAlphaChannel = ShowAlphaChannel;
            var screenSupported = !OperatingSystem.IsAndroid() && !OperatingSystem.IsIOS();
            _screenColorPickerSection.Visibility = ShowScreenColorPicker && screenSupported ? Visibility.Visible : Visibility.Collapsed;
            _colorWheel.Visibility = ShowColorWheel ? Visibility.Visible : Visibility.Collapsed;
            _lightnessSlider.Visibility = ShowColorWheel ? Visibility.Visible : Visibility.Collapsed;
            _leftPanel.Visibility = ShowColorWheel || _screenColorPickerSection.Visibility == Visibility.Visible
                ? Visibility.Visible
                : Visibility.Collapsed;
            _colorGridSection.Visibility = ShowColorGrid ? Visibility.Visible : Visibility.Collapsed;
            _colorEditorSection.Visibility = ShowColorEditor ? Visibility.Visible : Visibility.Collapsed;
        }

        private void UpdateAllControls()
        {
            _lockUpdates = true;
            try
            {
                var color = Color;
                var hsl = new HslColor(color);

                _colorWheel.Color = color;
                _colorWheel.Lightness = hsl.L;
                _lightnessSlider.Color = color;
                _colorGrid.Color = color;
                _colorEditor.Color = color;
                _colorPreview.Background = new SolidColorBrush(color);
            }
            finally
            {
                _lockUpdates = false;
            }

            UpdateOriginalPreview();
        }

        private void UpdateOriginalPreview()
        {
            var original = OriginalColor;
            _originalColorPreview.Background = new SolidColorBrush(original);
            _originalColorHexText.Text = $"#{original.R:X2}{original.G:X2}{original.B:X2}";
            var luminance = 0.299 * original.R + 0.587 * original.G + 0.114 * original.B;
            _originalColorHexText.Foreground = new SolidColorBrush(luminance > 128 ? Colors.Black : Colors.White);
        }

        private void OnColorWheelColorChanged(object? sender, ColorChangedEventArgs e)
        {
            if (_lockUpdates) return;
            Color = e.Color;
        }

        private void OnLightnessSliderChanged(object? sender, ColorChangedEventArgs e)
        {
            if (_lockUpdates) return;
            _colorWheel.Lightness = new HslColor(e.Color).L;
            Color = e.Color;
        }

        private void OnColorGridColorChanged(object? sender, ColorChangedEventArgs e)
        {
            if (_lockUpdates) return;
            Color = PreserveAlphaChannel
                ? Color.FromArgb(Color.A, e.Color.R, e.Color.G, e.Color.B)
                : e.Color;
        }

        private void OnComponentColorChanged(object? sender, ColorChangedEventArgs e)
        {
            if (_lockUpdates) return;
            Color = e.Color;
        }

        private void ApplyHistoryColor(Color color)
        {
            _isApplyingColorHistory = true;
            try
            {
                Color = color;
            }
            finally
            {
                _isApplyingColorHistory = false;
            }

            UpdateHistoryButtonStates();
        }

        private void UpdateHistoryButtonStates()
        {
            if (_undoButton == null) return;
            _undoButton.IsEnabled = _colorHistory.CanUndo;
            _redoButton.IsEnabled = _colorHistory.CanRedo;
        }

        private void OnIsOpenChanged(DependencyObject sender, DependencyProperty dp)
        {
            // Escape and outside clicks close the modal; treat that as Cancel.
            if (!IsOpen && !_isClosing)
                Finish(null);
        }

        private void Finish(Color? result)
        {
            if (_isClosing) return;
            _isClosing = true;

            if (IsOpen)
                IsOpen = false;

            if (_isOpenCallbackToken != 0)
            {
                UnregisterPropertyChangedCallback(IsOpenProperty, _isOpenCallbackToken);
                _isOpenCallbackToken = 0;
            }

            if (_hostPanel != null)
            {
                _hostPanel.Children.Remove(this);
                _hostPanel = null;
            }

            var tcs = _tcs;
            _tcs = null;
            _xamlRoot = null;
            _isColorHistoryInitialized = false;
            tcs?.TrySetResult(result);
        }

        private static Border CreatePreviewBorder() => new()
        {
            Height = ColorPreviewHeight,
            BorderBrush = DaisyResourceLookup.GetBrush("DaisyBase300Brush"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(2)
        };

        private static DaisyButton CreateHistoryButton(string name, string label, bool mirrorIcon)
        {
            var button = new DaisyButton
            {
                Name = name,
                IconData = FloweryPathHelpers.GetIconPathData("DaisyIconRefresh"),
                Variant = DaisyButtonVariant.Ghost,
                Size = DaisySize.Small,
                Shape = DaisyButtonShape.Square,
                IsEnabled = false
            };
            if (mirrorIcon)
            {
                button.RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5);
                button.RenderTransform = new ScaleTransform { ScaleX = -1 };
            }

            ToolTipService.SetToolTip(button, label);
            AutomationProperties.SetName(button, label);
            AutomationProperties.SetAutomationId(button, name);
            return button;
        }

        // Texts are resolved once per dialog; ShowAsync creates a new dialog, so it follows the current culture
        private static string L(string key) => FloweryLocalization.GetStringInternal(key);

        private static TextBlock CreateLabel(string text, bool bold = false) => new()
        {
            Text = text,
            FontSize = LabelFontSize,
            FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal,
            Foreground = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush"),
            Margin = new Thickness(0, 0, 0, 2)
        };

        private FrameworkElement BuildLayout(
            out FrameworkElement leftPanel,
            out FrameworkElement screenSection,
            out FrameworkElement gridSection,
            out FrameworkElement editorSection)
        {
            var title = new TextBlock
            {
                Text = L("ColorPicker_Title"),
                FontWeight = FontWeights.SemiBold,
                FontSize = 16,
                Foreground = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush"),
                Margin = new Thickness(0, 0, 0, 12)
            };

            var main = new Grid { ColumnSpacing = LeftPanelRightMargin };
            main.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            main.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var left = new StackPanel { Spacing = PanelSpacing, VerticalAlignment = VerticalAlignment.Top };
            left.Children.Add(_colorWheel);
            var lightness = new StackPanel { Spacing = TinySpacing };
            lightness.Children.Add(CreateLabel(L("ColorPicker_Lightness")));
            lightness.Children.Add(_lightnessSlider);
            left.Children.Add(lightness);
            var screen = new StackPanel { Spacing = TinySpacing, Margin = new Thickness(0, PanelSpacing, 0, 0) };
            screen.Children.Add(CreateLabel(L("ColorPicker_ScreenPicker"), bold: true));
            screen.Children.Add(_screenColorPicker);
            left.Children.Add(screen);
            main.Children.Add(left);

            var right = new StackPanel { Spacing = PanelSpacing, VerticalAlignment = VerticalAlignment.Top };
            Grid.SetColumn(right, 1);

            var preview = new Grid { ColumnSpacing = PanelSpacing, Margin = new Thickness(0, 0, 0, TinySpacing) };
            preview.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            preview.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var originalStack = new StackPanel();
            originalStack.Children.Add(CreateLabel(L("ColorPicker_Original")));
            originalStack.Children.Add(_originalColorPreview);
            var newStack = new StackPanel();
            newStack.Children.Add(CreateLabel(L("ColorPicker_New")));
            newStack.Children.Add(_colorPreview);
            Grid.SetColumn(newStack, 1);
            preview.Children.Add(originalStack);
            preview.Children.Add(newStack);
            right.Children.Add(preview);

            var grid = new StackPanel();
            grid.Children.Add(CreateLabel(L("ColorPicker_Palette"), bold: true));
            grid.Children.Add(_colorGrid);
            right.Children.Add(grid);

            var editor = new StackPanel { Margin = new Thickness(0, TinySpacing, 0, 0) };
            editor.Children.Add(CreateLabel(L("ColorPicker_Values"), bold: true));
            editor.Children.Add(_colorEditor);
            right.Children.Add(editor);
            main.Children.Add(right);

            var footer = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Spacing = PanelSpacing,
                Margin = new Thickness(0, 12, 0, 0)
            };
            footer.Children.Add(_undoButton);
            footer.Children.Add(_redoButton);
            footer.Children.Add(new Border { Width = PanelSpacing });
            footer.Children.Add(_okButton);
            footer.Children.Add(_cancelButton);

            var root = new StackPanel { Padding = new Thickness(16) };
            root.Children.Add(title);
            root.Children.Add(new ScrollViewer
            {
                Content = main,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollMode = ScrollMode.Auto
            });
            root.Children.Add(footer);

            leftPanel = left;
            screenSection = screen;
            gridSection = grid;
            editorSection = editor;
            return root;
        }
    }
}
