using Flowery.Controls.Scheduler.Models;
using Flowery.Localization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;

namespace Flowery.Controls;

public partial class DaisyScheduler
{
    private sealed record ToolbarAction(SchedulerToolbarButton Button, bool Adornment);

    private static string Text(string key) => FloweryLocalization.GetStringInternal("Scheduler_" + key);

    private void UpdateToolbar()
    {
        if (_toolbar is null || _views is null || _customButtons is null || _dateButton is null)
            return;
        _toolbar.Margin = Token<Thickness>("DaisySchedulerToolbarMargin");
        _views.Margin = Token<Thickness>("DaisySchedulerViewButtonsMargin");
        foreach (var button in _toolbar.Children.OfType<DaisyButton>())
        {
            button.Size = _size;
            button.Margin = Token<Thickness>("DaisySchedulerToolbarItemMargin");
            if (button.Tag is string key)
            {
                if (key is "Today" or "Settings")
                    button.Content = Text(key);
                AutomationProperties.SetName(button, Text(key == "Date" ? "ChooseDate" : key));
            }
        }
        var dates = VisibleDates();
        _dateButton.Content = CurrentView == SchedulerView.Month ? SelectedDate.ToString("MMMM yyyy", Culture) :
            dates.Length == 1 ? dates[0].ToString("D", Culture) :
            dates[0].ToString("d MMM", Culture) + " – " + dates[^1].ToString("d MMM yyyy", Culture);
        _views.Children.Clear();
        foreach (var view in Enum.GetValues<SchedulerView>())
        {
            var button = new DaisyButton
            {
                Content = Text(view.ToString()), Size = _size, Tag = view,
                Variant = view == CurrentView ? DaisyButtonVariant.Primary : DaisyButtonVariant.Ghost,
                Margin = new Thickness(2)
            };
            button.Click += OnToolbarClick;
            _views.Children.Add(button);
        }
        _customButtons.Children.Clear();
        foreach (var button in ToolbarButtons)
            _customButtons.Children.Add(CreateToolbarButton(button, false));
        foreach (var button in ToolbarAdornments)
            _customButtons.Children.Add(CreateToolbarButton(button, true));
    }

    private DaisyButton CreateToolbarButton(SchedulerToolbarButton model, bool adornment)
    {
        var content = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
        if (model.Image is { Length: > 0 } bytes)
            content.Children.Add(OwnedImage(bytes, 18 * SizeScale));
        else if (model.Glyph is { Length: > 0 } glyph)
            content.Children.Add(Label(glyph));
        if (model.Label is { Length: > 0 } label)
            content.Children.Add(Label(label));
        var button = new DaisyButton
        {
            Content = content, Size = _size, IsEnabled = model.IsEnabled,
            Variant = model.IsToggle && model.IsChecked ? DaisyButtonVariant.Primary : DaisyButtonVariant.Ghost,
            Tag = new ToolbarAction(model, adornment), Margin = new Thickness(2)
        };
        SetSchedulerTooltip(button, model.Tooltip);
        AutomationProperties.SetName(button, model.Label ?? model.Tooltip ?? model.Id ?? string.Empty);
        if (model.MenuItems is { Count: > 0 } entries)
        {
            var menu = new MenuFlyout();
            foreach (var entry in entries)
                menu.Items.Add(CreateMenuItem(entry, adornment));
            button.Flyout = menu;
        }
        else
            button.Click += OnToolbarClick;
        return button;
    }

    private MenuFlyoutItemBase CreateMenuItem(SchedulerToolbarButton model, bool adornment)
    {
        if (model.MenuItems is { Count: > 0 } entries)
        {
            var submenu = new MenuFlyoutSubItem { Text = model.Label ?? string.Empty, IsEnabled = model.IsEnabled, Tag = new ToolbarAction(model, adornment) };
            foreach (var entry in entries)
                submenu.Items.Add(CreateMenuItem(entry, adornment));
            return submenu;
        }
        var item = new MenuFlyoutItem { Text = model.Label ?? string.Empty, IsEnabled = model.IsEnabled, Tag = new ToolbarAction(model, adornment) };
        item.Click += OnCustomMenuClick;
        return item;
    }

    private void OnCustomMenuClick(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem { Tag: ToolbarAction action })
            InvokeToolbarAction(action);
    }

    private void InvokeToolbarAction(ToolbarAction action)
    {
        if (action.Button.Command is { } command && command.CanExecute(action.Button.CommandParameter))
            command.Execute(action.Button.CommandParameter);
        if (action.Adornment)
            AdornmentToggleRequested?.Invoke(this, action.Button);
        else
            ToolbarButtonClicked?.Invoke(this, action.Button);
    }

    private void OnToolbarClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button)
            return;
        switch (button.Tag)
        {
            case SchedulerView view:
                SetValue(CurrentViewProperty, view);
                break;
            case ToolbarAction action:
                InvokeToolbarAction(action);
                break;
            case "Previous": Navigate(-1); break;
            case "Next": Navigate(1); break;
            case "Today":
                SetValue(SelectedDatesProperty, null);
                SetValue(SelectedDateProperty, new DateTimeOffset(DateTime.Today));
                break;
            case "Date":
                var calendar = new CalendarView
                {
                    SelectionMode = CalendarViewSelectionMode.Single,
                    FirstDayOfWeek = (Windows.Globalization.DayOfWeek)FirstDayOfWeek
                };
                // CalendarView formats with the app language; follow the scheduler's formatting culture instead.
                if (Culture.Name.Length > 0)
                    calendar.Language = Culture.Name;
                calendar.SelectedDates.Add(SelectedDate);
                calendar.SetDisplayDate(SelectedDate);
                calendar.SelectedDatesChanged += OnDatePicked;
                new Flyout { Content = calendar }.ShowAt(button);
                break;
            case "Settings":
                new Flyout { Content = BuildSettings(), FlyoutPresenterStyle = SchedulerSurfaceStyle(typeof(FlyoutPresenter)) }.ShowAt(button);
                break;
        }
    }

    private void OnDatePicked(CalendarView sender, CalendarViewSelectedDatesChangedEventArgs e)
    {
        if (e.AddedDates is [var date, ..])
        {
            SetValue(SelectedDatesProperty, null);
            SetValue(SelectedDateProperty, new DateTimeOffset(date.Date));
        }
    }

    private FrameworkElement BuildSettings()
    {
        // WinUI overlays the vertical scrollbar; the right padding keeps the selects clear of it.
        var settings = new StackPanel
        {
            Spacing = SpacingXS, MinWidth = Token<double>("DaisySchedulerSettingsMinimumWidth"),
            Padding = new Thickness(0, 0, SpacingLarge, 0)
        };
        settings.Children.Add(SettingCheckBox("ShowTiles", ShowTiles));
        settings.Children.Add(SettingCheckBox("ShowWeekends", ShowWeekends));
        settings.Children.Add(SettingCheckBox("HideIdleResources", HideIdleResources));
        settings.Children.Add(SettingCheckBox("GroupByDay", GroupMode == SchedulerGroupMode.ByDay));
        AddPreferences(settings);
        var increments = new[]
        {
            string.Format(Culture, Text("Minutes"), 15),
            string.Format(Culture, Text("Minutes"), 30),
            string.Format(Culture, Text("Minutes"), 60)
        };
        var scale = new DaisySelect { Size = _size, Tag = "TimeScale", ItemsSource = increments, SelectedIndex = SnapMinutes <= 15 ? 0 : SnapMinutes <= 30 ? 1 : 2 };
        scale.SelectionChanged += OnTimeScaleChanged;
        settings.Children.Add(Label(Text("TimeScale")));
        settings.Children.Add(scale);
        foreach (var option in _tileOptions)
        {
            var check = new DaisyCheckBox { Content = option.Label, IsChecked = option.IsOn, Tag = option, Size = _size };
            check.Checked += OnTileOptionChanged;
            check.Unchecked += OnTileOptionChanged;
            settings.Children.Add(check);
        }
        return new ScrollViewer
        {
            Content = settings, MaxHeight = Token<double>("DaisySchedulerSettingsMaximumHeight"),
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
    }

    private DaisyCheckBox SettingCheckBox(string key, bool value)
    {
        var check = new DaisyCheckBox { Content = Text(key), IsChecked = value, Tag = key, Size = _size };
        check.Checked += OnSettingChanged;
        check.Unchecked += OnSettingChanged;
        return check;
    }

    private void OnSettingChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is not DaisyCheckBox { Tag: string key } check)
            return;
        var value = check.IsChecked == true;
        switch (key)
        {
            case "ShowTiles": SetValue(ShowTilesProperty, value); break;
            case "ShowWeekends": SetValue(ShowWeekendsProperty, value); break;
            case "HideIdleResources": SetValue(HideIdleResourcesProperty, value); break;
            case "GroupByDay": SetValue(GroupModeProperty, value ? SchedulerGroupMode.ByDay : SchedulerGroupMode.ByResource); break;
        }
    }

    private void OnTimeScaleChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is DaisySelect { SelectedIndex: >= 0 } select)
            SetValue(TimeIncrementProperty, new TimeIncrementConfig
            {
                MinorTick = TimeSpan.FromMinutes(select.SelectedIndex switch { 0 => 15, 1 => 30, _ => 60 }),
                MajorTick = TimeIncrement?.MajorTick ?? TimeSpan.FromHours(1),
                PixelsPerMinute = TimeIncrement?.PixelsPerMinute ?? 0.8
            });
    }

    private void OnTileOptionChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is DaisyCheckBox { Tag: SchedulerTileOption option } check)
            TileOptionToggled?.Invoke(this, new SchedulerTileOption { Key = option.Key, Label = option.Label, IsOn = check.IsChecked == true });
    }
}
