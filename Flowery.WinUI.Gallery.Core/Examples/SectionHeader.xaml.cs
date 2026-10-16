using Flowery.Controls;
using Flowery.Localization;
using Flowery.Theming;
using Flowery.WinUI.Gallery.Localization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Flowery.WinUI.Gallery.Examples
{
    public sealed partial class SectionHeader : UserControl
    {
        public static readonly DependencyProperty SectionIdProperty =
            DependencyProperty.Register(
                nameof(SectionId),
                typeof(string),
                typeof(SectionHeader),
                new PropertyMetadata(string.Empty, OnTitleSourceChanged));

        public string SectionId
        {
            get => (string)GetValue(SectionIdProperty);
            set => SetValue(SectionIdProperty, value);
        }

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(
                nameof(Title),
                typeof(string),
                typeof(SectionHeader),
                new PropertyMetadata(string.Empty, OnTitleSourceChanged));

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public SectionHeader()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            DaisyThemeManager.ThemeChanged += OnThemeChanged;
            FlowerySizeManager.SizeChanged += OnGlobalSizeChanged;
            GalleryLocalization.CultureChanged += OnCultureChanged;
            ApplyTheme();
            ApplySize(FlowerySizeManager.CurrentSize);
            UpdateTitleText();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            DaisyThemeManager.ThemeChanged -= OnThemeChanged;
            FlowerySizeManager.SizeChanged -= OnGlobalSizeChanged;
            GalleryLocalization.CultureChanged -= OnCultureChanged;
        }

        private static void OnTitleSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SectionHeader header)
            {
                header.UpdateTitleText();
            }
        }

        private void OnCultureChanged(object? sender, string cultureName)
        {
            if (DispatcherQueue is { } dispatcherQueue)
            {
                dispatcherQueue.TryEnqueue(UpdateTitleText);
            }
        }

        private void UpdateTitleText()
        {
            TitleTextBlock.Text = GetDisplayTitle();
        }

        /// <summary>
        /// Sections that match a sidebar entry show its translated name, followed by the control class
        /// in parentheses when the entry represents a single control, e.g. "Tag-Auswahl (DaisyTagPicker)".
        /// Other sections show <see cref="Title"/> unchanged.
        /// </summary>
        private string GetDisplayTitle()
        {
            if (GallerySidebarData.FindItem(SectionId) is not { } item)
            {
                return Title;
            }

            var name = FloweryLocalization.GetString(item.Name);
            return item.ControlName is { Length: > 0 } controlName && !name.Contains(controlName, StringComparison.Ordinal)
                ? $"{name} ({controlName})"
                : name;
        }

        private void OnThemeChanged(object? sender, string themeName)
        {
            ApplyTheme();
        }

        private void OnGlobalSizeChanged(object? sender, DaisySize size)
        {
            ApplySize(size);
        }

        private void ApplyTheme()
        {
            // ThemeResource doesn't auto-refresh in WinUI when MergedDictionaries change,
            // so we manually update the foreground on theme change.
            var brush = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush");
            if (brush != null)
            {
                TitleTextBlock.Foreground = brush;
            }
        }

        private void ApplySize(DaisySize size)
        {
            TitleTextBlock.FontSize = size switch
            {
                DaisySize.ExtraSmall => 12,
                DaisySize.Small => 14,
                DaisySize.Medium => 16,
                DaisySize.Large => 18,
                DaisySize.ExtraLarge => 20,
                _ => 14
            };
        }
    }
}
