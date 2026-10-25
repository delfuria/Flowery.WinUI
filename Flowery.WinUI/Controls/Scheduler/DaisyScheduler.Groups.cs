using Flowery.Controls.Scheduler.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;

namespace Flowery.Controls;

public partial class DaisyScheduler
{
    public static readonly DependencyProperty ResourceKindFilterProperty = Register<ResourceKind?>(nameof(ResourceKindFilter), null);

    /// <summary>Limits resource lanes to this kind. Null shows every kind.</summary>
    public ResourceKind? ResourceKindFilter
    {
        get => (ResourceKind?)GetValue(ResourceKindFilterProperty);
        set => SetValidated(ResourceKindFilterProperty, value);
    }

    public static readonly DependencyProperty ResourceGroupsProperty = Register<IReadOnlyList<ResourceGroupModel>?>(nameof(ResourceGroups), null);

    /// <summary>Named, collapsible groups in the horizontal Timeline day view. Reassign after changing models.</summary>
    public IReadOnlyList<ResourceGroupModel>? ResourceGroups
    {
        get => (IReadOnlyList<ResourceGroupModel>?)GetValue(ResourceGroupsProperty);
        set => SetValue(ResourceGroupsProperty, value);
    }

    public static readonly DependencyProperty CollapsedResourceGroupIdsProperty = Register<IReadOnlyList<object>?>(nameof(CollapsedResourceGroupIds), null);

    /// <summary>Collapsed group ids. Null/empty opens all groups. Group input models are never changed by toggling.</summary>
    public IReadOnlyList<object>? CollapsedResourceGroupIds
    {
        get => (IReadOnlyList<object>?)GetValue(CollapsedResourceGroupIdsProperty);
        set => SetValue(CollapsedResourceGroupIdsProperty, value);
    }

    private sealed record TimelineGroup(string? Name, IReadOnlyList<ResourceModel> Resources,
        DateTime? Date = null, ResourceGroupModel? Model = null);

    private IEnumerable<TimelineGroup> TimelineGroups(IReadOnlyList<ResourceModel> resources, IReadOnlyList<DateTime> days)
    {
        if (CurrentView == SchedulerView.Timeline && ResourceGroups is { Count: > 0 } groups)
        {
            foreach (var group in groups)
                yield return new TimelineGroup(group.Name, resources.Where(group.Matches).ToArray(), Model: group);
        }
        else if (CurrentView == SchedulerView.Planner && GroupMode == SchedulerGroupMode.ByDay)
        {
            foreach (var day in days)
                yield return new TimelineGroup(day.ToString("D", Culture), resources, day);
        }
        else
            yield return new TimelineGroup(null, resources);
    }

    private FrameworkElement GroupHeading(TimelineGroup group)
    {
        if (group.Model is not { } model)
            return Label(group.Name ?? string.Empty);
        var collapsed = CollapsedResourceGroupIds?.Contains(model.Id) == true;
        var button = new DaisyButton
        {
            Content = (collapsed ? "▸ " : "▾ ") + group.Name + " (" + group.Resources.Count.ToString(Culture) + ")",
            Variant = DaisyButtonVariant.Ghost, Size = _size, Tag = model, HorizontalContentAlignment = HorizontalAlignment.Left
        };
        AutomationProperties.SetName(button, model.Name);
        button.Click += OnResourceGroupToggle;
        return button;
    }

    private void OnResourceGroupToggle(object? sender, RoutedEventArgs e)
    {
        if (sender is not DaisyButton { Tag: ResourceGroupModel group })
            return;
        var collapsed = (CollapsedResourceGroupIds ?? Array.Empty<object>()).ToList();
        if (!collapsed.Remove(group.Id))
            collapsed.Add(group.Id);
        SetValue(CollapsedResourceGroupIdsProperty, collapsed.ToArray());
    }
}
