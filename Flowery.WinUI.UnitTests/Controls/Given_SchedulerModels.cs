using Flowery.Controls.Scheduler.Models;
using Flowery.Controls.Scheduler.Rendering;
using Flowery.Controls.Scheduler.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Flowery.WinUI.UnitTests.Controls;

[TestClass]
public sealed class Given_SchedulerModels
{
    private static readonly DateTime Monday = new(2026, 10, 12);

    [TestMethod]
    public void When_AppointmentsOverlap_PackingAndCloneKeepTheUpstreamDataContract()
    {
        var first = new AppointmentModel { Start = Monday.AddHours(9), End = Monday.AddHours(11), ResourceIds = { 1 }, Tiles = { new AppointmentDecoration { Text = "Tile" } } };
        var second = new AppointmentModel { Start = Monday.AddHours(10), End = Monday.AddHours(12) };
        var third = new AppointmentModel { Start = Monday.AddHours(13), End = Monday.AddHours(14) };
        var layouts = EventBlockLayout.Layout(Monday, new[] { first, second, third });
        Assert.AreEqual(2, layouts.Single(layout => layout.Appointment == first).ColumnCount);
        Assert.AreEqual(1, layouts.Single(layout => layout.Appointment == third).ColumnCount);
        var copy = first.Clone();
        copy.ResourceIds.Add(2);
        copy.Tiles.Clear();
        Assert.HasCount(1, first.ResourceIds);
        Assert.HasCount(1, first.Tiles);
    }

    [TestMethod]
    public void When_DailySpanIsExpanded_EachDayGetsTheStoredClockWindow()
    {
        var series = new AppointmentModel { Start = Monday.AddHours(9), End = Monday.AddDays(4).AddHours(12), SpanMode = AppointmentSpanMode.Daily };
        var occurrences = AppointmentOccurrences.InRange(series, Monday, Monday.AddDays(7)).ToArray();
        Assert.HasCount(5, occurrences);
        Assert.IsTrue(occurrences.All(occurrence => occurrence.Start.TimeOfDay == TimeSpan.FromHours(9) && occurrence.End.TimeOfDay == TimeSpan.FromHours(12)));
        var crossing = new AppointmentModel { Start = Monday.AddHours(22), End = Monday.AddDays(1).AddHours(1), SpanMode = AppointmentSpanMode.Daily };
        Assert.IsEmpty(AppointmentOccurrences.InRange(crossing, Monday, Monday.AddDays(7)));
    }
}
