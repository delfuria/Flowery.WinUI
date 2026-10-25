using Flowery.Controls;
using Flowery.Controls.Scheduler.Contracts;
using Flowery.Controls.Scheduler.Models;
using Flowery.Controls.Scheduler.Utilities;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Foundation;
using Windows.System;

namespace Flowery.WinUI.RuntimeTests.Controls;

public sealed partial class Given_DaisyScheduler
{
    [TestMethod]
    public async Task When_GroupMoveFollowsHostRefresh_ItUsesTheSelectedVisibleResourceLanes()
    {
        foreach (var allDay in new[] { false, true })
        {
            var scope = await SchedulerScope.CreateAsync(SchedulerView.Day);
            scope.Scheduler.ResourceKindFilter = ResourceKind.Room;
            scope.Scheduler.SchedulerResources = new[]
            {
                new ResourceModel { Id = "alex", Kind = ResourceKind.Person },
                new ResourceModel { Id = "bailey", Kind = ResourceKind.Person },
                new ResourceModel { Id = "a", Kind = ResourceKind.Room },
                new ResourceModel { Id = "equipment", Kind = ResourceKind.Equipment },
                new ResourceModel { Id = "b", Kind = ResourceKind.Room },
                new ResourceModel { Id = "c", Kind = ResourceKind.Room }
            };
            var first = new AppointmentModel { Id = 1, Start = Monday.AddHours(9), End = Monday.AddHours(10), ResourceIds = { "alex", "a", "b" } };
            var second = new AppointmentModel { Id = 2, Start = Monday.AddHours(11), End = Monday.AddHours(12), ResourceIds = { "alex", "a", "b" } };
            if (allDay)
            {
                second.IsAllDay = true;
                second.Start = Monday;
                second.End = Monday.AddDays(1);
            }
            scope.Scheduler.Appointments = new[] { first, second };
            await scope.LayoutAsync();
            await scope.ClickAsync(scope.Frames(first).First());
            await scope.ClickAsync(scope.Frames(second).Last(), modifiers: VirtualKeyModifiers.Control);
            // Replacement instances must retain which of their two room columns was selected.
            first = first.Clone();
            second = second.Clone();
            scope.Scheduler.Appointments = new[] { first, second };
            await scope.LayoutAsync();
            Assert.HasCount(2, scope.Scheduler.SelectedAppointments, "allDay " + allDay);
            AppointmentsMoveArgs? request = null;
            scope.Scheduler.AppointmentsMoveRequested += (_, args) => request = args;
            var source = scope.Frames(first).First();
            var target = scope.Frames(first).Last();
            var origin = scope.Point(source, source.ActualWidth / 2, source.ActualHeight / 3);
            var destination = scope.Point(target, target.ActualWidth / 2, target.ActualHeight / 3);
            await scope.DragAsync(source, origin, destination);
            Assert.IsNotNull(request, "allDay " + allDay);
            var movedFirst = request.Items.Single(item => ReferenceEquals(item.Appointment, first));
            var movedSecond = request.Items.Single(item => ReferenceEquals(item.Appointment, second));
            Assert.AreEqual("a", movedFirst.FromResourceId);
            Assert.AreEqual("b", movedFirst.ToResourceId);
            Assert.AreEqual("b", movedSecond.FromResourceId);
            Assert.AreEqual("c", movedSecond.ToResourceId);
            Assert.AreEqual(first.Start, movedFirst.NewStart);
            Assert.AreEqual(second.Start, movedSecond.NewStart);
            CollectionAssert.AreEqual(new object[] { "alex", "a", "b" }, first.ResourceIds.ToArray());
            CollectionAssert.AreEqual(new object[] { "alex", "a", "b" }, second.ResourceIds.ToArray());
        }
    }

    [TestMethod]
    public async Task When_DailyMoveCrossesMidnight_ItIsRejectedAndAValidMoveIsStillAccepted()
    {
        var scope = await SchedulerScope.CreateAsync(SchedulerView.Day);
        scope.Appointment.Start = Monday.AddHours(22);
        scope.Appointment.End = Monday.AddDays(4).AddHours(23);
        scope.Appointment.SpanMode = AppointmentSpanMode.Daily;
        scope.Scheduler.Appointments = new[] { scope.Appointment };
        await scope.LayoutAsync();
        var frame = scope.Frame(scope.Appointment);
        AppointmentMoveArgs? request = null;
        scope.Scheduler.AppointmentMoveRequested += (_, args) => request = args;
        var origin = scope.Point(frame, frame.ActualWidth / 2, frame.ActualHeight / 2);
        await scope.DragAsync(frame, origin, Offset(origin, 0, 48));
        Assert.IsNull(request);
        Assert.AreEqual(5, AppointmentOccurrences.InRange(scope.Appointment, Monday, Monday.AddDays(7)).Count());
        frame = scope.Frame(scope.Appointment);
        origin = scope.Point(frame, frame.ActualWidth / 2, frame.ActualHeight / 4);
        await scope.DragAsync(frame, origin, Offset(origin, 0, -48));
        Assert.IsNotNull(request);
        Assert.AreEqual(Monday.AddHours(21), request.NewStart);
        Assert.AreEqual(Monday.AddDays(4).AddHours(22), request.NewEnd);
    }

    [TestMethod]
    public async Task When_GroupEditMakesADailyWindowInvalid_TheWholeEditIsRejected()
    {
        foreach (var resize in new[] { false, true })
        {
            var scope = await SchedulerScope.CreateAsync(SchedulerView.Day);
            scope.Appointment.End = Monday.AddHours(12);
            var daily = new AppointmentModel
            {
                Id = 2, SpanMode = AppointmentSpanMode.Daily,
                Start = Monday.AddHours(resize ? 9.5 : 22),
                End = Monday.AddDays(4).AddHours(resize ? 10 : 23)
            };
            scope.Scheduler.Appointments = new[] { scope.Appointment, daily };
            await scope.LayoutAsync();
            await scope.ClickAsync(scope.Frame(daily));
            await scope.ClickAsync(scope.Frame(scope.Appointment), modifiers: VirtualKeyModifiers.Control);
            var requests = 0;
            scope.Scheduler.AppointmentsMoveRequested += (_, _) => requests++;
            scope.Scheduler.AppointmentsResizeRequested += (_, _) => requests++;
            var frame = scope.Frame(scope.Appointment);
            var origin = scope.Point(frame, frame.ActualWidth / 2, resize ? frame.ActualHeight - 2 : frame.ActualHeight / 3);
            await scope.DragAsync(frame, origin, Offset(origin, 0, resize ? -24 : 48));
            Assert.AreEqual(0, requests, "resize " + resize);
            Assert.AreEqual(5, AppointmentOccurrences.InRange(daily, Monday, Monday.AddDays(7)).Count());
            Assert.AreEqual(Monday.AddHours(9), scope.Appointment.Start);
            Assert.AreEqual(Monday.AddHours(12), scope.Appointment.End);
        }
    }

    [TestMethod]
    public async Task When_SpringGapIsClickedOrDroppedOn_InputIsRejectedWithoutBreakingLaterInput()
    {
        foreach (var view in new[] { SchedulerView.Planner, SchedulerView.Timeline })
        {
            var scope = await SchedulerScope.CreateAsync(view);
            var date = new DateTime(2026, 3, 29);
            scope.Scheduler.SelectedDate = new DateTimeOffset(date);
            scope.Scheduler.SchedulerResources = new[] { new ResourceModel { Id = 1, TimeZoneId = "Europe/Berlin" } };
            scope.Scheduler.Appointments = Array.Empty<AppointmentModel>();
            scope.Scheduler.SingleClickCreates = true;
            await scope.LayoutAsync();
            var lane = Descendants<Canvas>(scope.Scheduler).Single(canvas => canvas.AllowDrop);
            var selections = 0;
            var creations = new List<AppointmentCreateArgs>();
            var drops = new List<ExternalDropArgs>();
            scope.Scheduler.EmptySlotSelected += (_, _) => selections++;
            scope.Scheduler.AppointmentCreateRequested += (_, args) => creations.Add(args);
            scope.Scheduler.ExternalAppointmentDropped += (_, args) => drops.Add(args);
            var gap = HorizontalSlotPoint(scope, lane, date.AddHours(2.5));
            await scope.ClickLaneAsync(lane, scope.Point(lane, gap.X, gap.Y));
            Assert.AreEqual(0, selections, view.ToString());
            Assert.IsEmpty(creations, view.ToString());
            var item = new Droppable();
            Assert.IsFalse(scope.Scheduler.CanDropExternal(lane, gap, item), view.ToString());
            Assert.IsFalse(scope.Scheduler.DropExternal(lane, gap, item), view.ToString());
            Assert.IsEmpty(drops, view.ToString());
            var valid = HorizontalSlotPoint(scope, lane, date.AddHours(1.5));
            await scope.ClickLaneAsync(lane, scope.Point(lane, valid.X, valid.Y));
            Assert.AreEqual(1, selections, view.ToString());
            Assert.AreEqual(date.AddMinutes(30), creations.Single().Start, view.ToString());
            Assert.IsTrue(scope.Scheduler.CanDropExternal(lane, valid, item), view.ToString());
            Assert.IsTrue(scope.Scheduler.DropExternal(lane, valid, item), view.ToString());
            Assert.AreEqual(date.AddMinutes(30), drops.Single().Start, view.ToString());
        }
    }

    [TestMethod]
    public async Task When_SpringGapMoveOrResize_NoChangeIsPublished()
    {
        foreach (var resize in new[] { false, true })
        {
            var scope = await SchedulerScope.CreateAsync(SchedulerView.Timeline);
            var date = new DateTime(2026, 3, 29);
            scope.Scheduler.SelectedDate = new DateTimeOffset(date);
            scope.Scheduler.SchedulerResources = new[] { new ResourceModel { Id = 1, TimeZoneId = "Europe/Berlin" } };
            scope.Appointment.Start = date;
            scope.Appointment.End = date.AddMinutes(30);
            scope.Scheduler.Appointments = new[] { scope.Appointment };
            if (resize)
                scope.Scheduler.SelectedAppointment = scope.Appointment;
            await scope.LayoutAsync();
            var requests = 0;
            scope.Scheduler.AppointmentMoveRequested += (_, _) => requests++;
            scope.Scheduler.AppointmentResizeRequested += (_, _) => requests++;
            var frame = scope.Frame(scope.Appointment);
            var lane = (Canvas)frame.Parent;
            var origin = scope.Point(frame, resize ? frame.ActualWidth - 2 : frame.ActualWidth / 2, frame.ActualHeight / 2);
            await scope.DragAsync(frame, origin, Offset(origin, lane.Width / 1440 * (resize ? 60 : 90), 0));
            Assert.AreEqual(0, requests, "resize " + resize);
            Assert.AreEqual(date, scope.Appointment.Start);
            Assert.AreEqual(date.AddMinutes(30), scope.Appointment.End);
        }
    }

    [TestMethod]
    public async Task When_HidingIdleResources_TheDisplayedTimeBasisIsUsed()
    {
        foreach (var view in new[] { SchedulerView.Day, SchedulerView.Week, SchedulerView.Planner, SchedulerView.Timeline })
        {
            var scope = await SchedulerScope.CreateAsync(view);
            scope.Scheduler.SchedulerResources = new[] { new ResourceModel { Id = 1, TimeZoneId = "America/New_York" } };
            scope.Appointment.Start = Monday.AddHours(1);
            scope.Appointment.End = Monday.AddHours(2);
            scope.Scheduler.Appointments = new[] { scope.Appointment };
            await scope.LayoutAsync();
            var horizontal = view is SchedulerView.Planner or SchedulerView.Timeline;
            Assert.AreEqual(horizontal ? 0 : 1, scope.Frames(scope.Appointment).Count(), view.ToString());
            scope.Scheduler.HideIdleResources = true;
            await scope.LayoutAsync();
            Assert.AreEqual(horizontal ? 0 : 1, scope.Frames(scope.Appointment).Count(), view.ToString());
            // Public occupancy queries retain their resource-local contract in every view.
            Assert.IsEmpty(scope.Scheduler.GetOccupiedResources(2026, 42), view.ToString());
            scope.Appointment.IsCancelled = true;
            scope.Scheduler.Appointments = new[] { scope.Appointment };
            await scope.LayoutAsync();
            Assert.IsEmpty(scope.Frames(scope.Appointment), view.ToString());
        }
    }

    /// <summary>A point one minute after <paramref name="time"/> in a horizontal lane, relative to the lane.</summary>
    private static Point HorizontalSlotPoint(SchedulerScope scope, Canvas lane, DateTime time)
    {
        var origin = scope.Scheduler.SelectedDate.Date;
        var days = scope.Scheduler.CurrentView == SchedulerView.Planner ? 7 : 1;
        if (days == 7)
            origin = origin.AddDays(-((7 + (int)origin.DayOfWeek - (int)scope.Scheduler.FirstDayOfWeek) % 7));
        return new Point(((time - origin).TotalMinutes + 1) * lane.Width / (days * 1440), lane.Height / 2);
    }
}
