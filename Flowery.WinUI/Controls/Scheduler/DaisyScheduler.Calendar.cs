using Flowery.Controls.Scheduler.Models;
using Flowery.Controls.Scheduler.Rendering;
using Flowery.Controls.Scheduler.Utilities;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Flowery.Controls;

public partial class DaisyScheduler
{
    private sealed record CalendarDate(DateTime Date);

    private sealed record CalendarParts(Grid Root, Grid Navigator, Grid Heading, Grid Days, Grid Detail,
        ScrollViewer Appointments, DaisyButton[] Arrows, DaisyButton[] DayButtons);

    private CalendarParts? _calendarParts;

    private const string ButtonForegroundKey = "DaisyButtonForeground";
    private const string ButtonForegroundPointerOverKey = "DaisyButtonForegroundPointerOver";

    private FrameworkElement BuildCalendar()
    {
        // Consecutive Calendar renders keep the 44 day and arrow buttons and update them in place:
        // creating and loading the buttons cost most of this view's render time.
        var parts = _calendarParts is { } cached && ReferenceEquals(_viewHost?.Child, cached.Root)
            ? cached : _calendarParts = CreateCalendar();
        var month = new DateTime(SelectedDate.Year, SelectedDate.Month, 1);
        var start = WeekStart(month, FirstDayOfWeek);
        var resourceIds = (SchedulerResources ?? Array.Empty<ResourceModel>())
            .Where(resource => ResourceKindFilter is null || resource.Kind == ResourceKindFilter)
            .Select(static resource => resource.Id).ToHashSet();
        var occupiedDays = new HashSet<DateTime>();
        var end = start.AddDays(42);
        foreach (var appointment in Appointments ?? Array.Empty<AppointmentModel>())
        {
            if (ResourceKindFilter is not null && !appointment.ResourceIds.Any(resourceIds.Contains))
                continue;
            foreach (var occurrence in AppointmentOccurrences.InRange(appointment, start, end))
                for (var day = occurrence.Start.Date < start ? start : occurrence.Start.Date;
                     day < end && day < occurrence.End; day = day.AddDays(1))
                    occupiedDays.Add(day);
        }
        parts.Root.ColumnSpacing = SpacingMedium;
        parts.Navigator.Width = Token<double>("DaisySchedulerCalendarWidth") * SizeScale;
        parts.Navigator.RowSpacing = SpacingSmall;
        RemoveLabels(parts.Heading);
        parts.Heading.Children.Add(Label(month.ToString("MMMM yyyy", Culture)));
        foreach (var button in parts.Arrows)
        {
            button.Size = _size;
            AutomationProperties.SetName(button, Text(button.Tag is < 0 ? "Previous" : "Next"));
        }
        var days = parts.Days;
        RemoveLabels(days);
        days.Children.Add(Label(Text("WeekNumber"), true));
        for (var column = 0; column < 7; column++)
        {
            var day = Label(Culture.DateTimeFormat.GetShortestDayName(start.AddDays(column).DayOfWeek), true);
            day.HorizontalAlignment = HorizontalAlignment.Center;
            Grid.SetColumn(day, column + 1);
            days.Children.Add(day);
        }
        var muted = Brush("DaisyBaseContentMutedBrush");
        // Avalonia's Padding replaces the inner padding; in WinUI that is ContentPadding.
        // Vertical padding would clip the day number in the ExtraSmall button height.
        var padding = new Thickness(SpacingXS, 0, SpacingXS, 0);
        for (var week = 0; week < 6; week++)
        {
            var weekStart = start.AddDays(week * 7);
            var number = Label(CalendarWeekNumber(weekStart).ToString(Culture), true);
            number.Margin = new Thickness(SpacingXS);
            Grid.SetRow(number, week + 1);
            days.Children.Add(number);
            for (var column = 0; column < 7; column++)
            {
                var date = weekStart.AddDays(column);
                var selected = date == SelectedDate.Date;
                var occupied = occupiedDays.Contains(date);
                var button = parts.DayButtons[week * 7 + column];
                button.Content = date.Day.ToString(Culture);
                button.Tag = new CalendarDate(date);
                button.Size = _size;
                button.Variant = selected || occupied ? DaisyButtonVariant.Primary : DaisyButtonVariant.Ghost;
                button.ButtonStyle = occupied && !selected ? DaisyButtonStyle.Soft : DaisyButtonStyle.Default;
                button.FontWeight = date == DateTime.Today || occupied ? FontWeights.Bold : FontWeights.Normal;
                // ContentPadding is a boxed value, so every assignment would count as a change and apply again.
                if (button.ContentPadding != padding)
                    button.ContentPadding = padding;
                SetDayForeground(button, date.Month != month.Month ? muted : null);
                AutomationProperties.SetName(button, date.ToString("D", Culture));
            }
        }

        var detail = parts.Detail;
        detail.RowSpacing = SpacingSmall;
        RemoveLabels(detail);
        detail.Children.Add(Label(SelectedDate.ToString("D", Culture)));
        var entries = new StackPanel { Name = "PART_CalendarAppointments", Spacing = SpacingSmall };
        var appointments = Items.Where(appointment => OnDate(appointment, SelectedDate.Date) &&
            (ResourceKindFilter is null || appointment.ResourceIds.Any(resourceIds.Contains)))
            .OrderByDescending(static appointment => appointment.IsAllDay).ThenBy(static appointment => appointment.Start).ToArray();
        foreach (var appointment in appointments)
            entries.Children.Add(CreateAppointment(appointment, AppointmentSurface.Timed));
        if (appointments.Length == 0)
            entries.Children.Add(Label(Text("NoEvents"), true));
        parts.Appointments.Content = entries;
        _bodyScroll = parts.Appointments;
        return parts.Root;
    }

    private CalendarParts CreateCalendar()
    {
        var root = new Grid().Define("Auto,*");
        var navigator = new Grid { Name = "PART_MonthNavigator" }.Define(null, "Auto,Auto");
        var heading = new Grid().Define("*,Auto,Auto");
        var arrows = new DaisyButton[2];
        for (var index = 0; index < arrows.Length; index++)
        {
            var button = new DaisyButton { Content = index == 0 ? "↑" : "↓", Variant = DaisyButtonVariant.Ghost, Tag = index == 0 ? -1 : 1 };
            button.Click += OnCalendarMonthChanged;
            Grid.SetColumn(button, index + 1);
            heading.Children.Add(button);
            arrows[index] = button;
        }
        navigator.Children.Add(heading);
        var days = new Grid { Name = "PART_CalendarDays" }.Define("Auto,*,*,*,*,*,*,*", "Auto,Auto,Auto,Auto,Auto,Auto,Auto");
        var dayButtons = new DaisyButton[42];
        for (var index = 0; index < dayButtons.Length; index++)
        {
            var button = new DaisyButton { HorizontalAlignment = HorizontalAlignment.Stretch, MinWidth = 0 };
            button.Click += OnCalendarDateSelected;
            Grid.SetRow(button, index / 7 + 1);
            Grid.SetColumn(button, index % 7 + 1);
            days.Children.Add(button);
            dayButtons[index] = button;
        }
        Grid.SetRow(days, 1);
        navigator.Children.Add(days);
        root.Children.Add(new ScrollViewer
        {
            Content = navigator, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        });

        var detail = new Grid().Define(null, "Auto,*");
        var scroll = CreateScroll(new StackPanel());
        scroll.Name = "PART_CalendarAppointmentScroll";
        scroll.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
        Grid.SetRow(scroll, 1);
        detail.Children.Add(scroll);
        Grid.SetColumn(detail, 1);
        root.Children.Add(detail);
        return new CalendarParts(root, navigator, heading, days, detail, scroll, arrows, dayButtons);
    }

    private static void RemoveLabels(Panel panel)
    {
        for (var index = panel.Children.Count - 1; index >= 0; index--)
            if (panel.Children[index] is TextBlock)
                panel.Children.RemoveAt(index);
    }

    // DaisyButton sets its Foreground itself in every state; its lightweight styling resources override that.
    // Avalonia's local Foreground on days outside the month does the same there.
    private static void SetDayForeground(DaisyButton button, Brush? foreground)
    {
        var current = button.Resources.TryGetValue(ButtonForegroundKey, out var value) ? value : null;
        if (ReferenceEquals(current, foreground))
            return;
        if (foreground is null)
        {
            button.Resources.Remove(ButtonForegroundKey);
            button.Resources.Remove(ButtonForegroundPointerOverKey);
        }
        else
        {
            button.Resources[ButtonForegroundKey] = foreground;
            button.Resources[ButtonForegroundPointerOverKey] = foreground;
        }
        // A button that has not loaded yet applies the resources at its first measure.
        if (button.IsLoaded)
            button.RefreshResolvedSize();
    }

    private int CalendarWeekNumber(DateTime date)
    {
        var day = date.Ticks / TimeSpan.TicksPerDay;
        var year = date.Year;
        var first = FirstWeekStart(new DateTime(year, 1, 1).Ticks / TimeSpan.TicksPerDay);
        if (day < first && year > 1)
            first = FirstWeekStart(new DateTime(year - 1, 1, 1).Ticks / TimeSpan.TicksPerDay);
        else if (year < 9999)
        {
            var next = FirstWeekStart(new DateTime(year + 1, 1, 1).Ticks / TimeSpan.TicksPerDay);
            if (day >= next) first = next;
        }
        return (int)((day - first) / 7 + 1);
    }

    private void OnCalendarMonthChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is DaisyButton { Tag: int direction })
        {
            SetValue(SelectedDatesProperty, null);
            SetValue(SelectedDateProperty, SelectedDate.AddMonths(direction));
        }
    }

    private void OnCalendarDateSelected(object? sender, RoutedEventArgs e)
    {
        if (sender is DaisyButton { Tag: CalendarDate target })
        {
            SetValue(SelectedDatesProperty, null);
            SetValue(SelectedDateProperty, new DateTimeOffset(target.Date));
        }
    }
}
