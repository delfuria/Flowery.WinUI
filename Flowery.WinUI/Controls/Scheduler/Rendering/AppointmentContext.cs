// Adapted from Xtreme.Components Scheduler (MIT).
// See ThirdPartyNotices/Xtreme.Components.txt for attribution and license.
using System.Globalization;
using Microsoft.UI.Xaml.Media;
using Flowery.Controls.Scheduler.Models;
using Flowery.Controls.Scheduler.Utilities;
using Flowery.Localization;

namespace Flowery.Controls.Scheduler.Rendering;

public enum AppointmentSurface { Timed, Chip, Bar }

/// <summary>The typed data context supplied to appointment and tooltip templates.</summary>
public sealed class AppointmentContext
{
    private readonly CultureInfo _culture;
    private readonly string _timePattern;

    internal AppointmentContext(AppointmentModel appointment, AppointmentSurface surface,
        Brush background, Brush bar, Brush foreground, Brush border, double cornerRadius, bool showTiles,
        CultureInfo culture, string timePattern)
    {
        Appointment = appointment;
        Surface = surface;
        BackBrush = background;
        BarBrush = bar;
        Foreground = foreground;
        BorderBrush = border;
        CornerRadius = cornerRadius;
        ShowTiles = showTiles;
        _culture = culture;
        _timePattern = timePattern;
    }

    public AppointmentModel Appointment { get; }
    public AppointmentSurface Surface { get; }
    public Brush BackBrush { get; }
    public Brush BarBrush { get; }
    public Brush Foreground { get; }
    public Brush BorderBrush { get; }
    public double CornerRadius { get; }
    public bool ShowTiles { get; }
    public bool ShowTime => !IsAllDay;
    public string Subject => Appointment.Subject;
    public string? Note => Appointment.Description;
    public string? Memo => Appointment.Memo;
    public DateTime Start => Appointment.Start;
    public DateTime End => Appointment.End;
    public bool IsAllDay => Appointment.IsAllDay;
    public bool IsRecurring => Appointment.IsRecurring;
    public bool IsLocked => Appointment.IsLocked;
    public bool IsComplete => Appointment.IsComplete;
    public bool IsCancelled => Appointment.IsCancelled;
    public bool IsTentative => Appointment.IsTentative;
    public bool IsConfirmed => Appointment.IsConfirmed;
    public IList<AppointmentIndicator> Indicators => Appointment.Indicators;
    public IList<AppointmentDetail> Details => Appointment.Details;
    public IList<AppointmentDecoration> Adornments => Appointment.Adornments;
    public IList<AppointmentDecoration> Tiles => Appointment.Tiles;
    public object? Tag => Appointment.Tag;
    public string StartTime => Start.ToString(_timePattern, _culture);
    public string TimeRange => IsAllDay ? FloweryLocalization.GetStringInternal("Scheduler_AllDay") :
        StartTime + " – " + End.ToString(_timePattern, _culture);
    public string CompactStart => _timePattern.Contains('H') ? StartTime : SchedulerTimeFormatter.Compact(Start);
    public string CompactTimeRange => IsAllDay || _timePattern.Contains('H') ? TimeRange : SchedulerTimeFormatter.RangeFormatter(Start, End);
}
