using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Flowery.Controls;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Flowery.WinUI.RuntimeTests.Controls;

[TestClass]
public class Given_FloweryComponentSidebar
{
    [TestMethod]
    public async Task When_SearchChangesWhileItemsAreLoading_ExpandedCategoriesGetTheirItems()
    {
        var categories = new ObservableCollection<SidebarCategory>();
        for (var categoryIndex = 0; categoryIndex < 12; categoryIndex++)
        {
            var category = new SidebarCategory { Name = $"SearchCategory{categoryIndex}", DisplayName = $"Category {categoryIndex}" };
            for (var itemIndex = 0; itemIndex < 4; itemIndex++)
            {
                category.Items.Add(new SidebarItem
                {
                    Id = $"search-item-{categoryIndex}-{itemIndex}",
                    Name = $"SearchItem{categoryIndex}_{itemIndex}",
                    DisplayName = itemIndex == 0 ? $"Tab {categoryIndex}" : $"Other {categoryIndex}-{itemIndex}"
                });
            }
            categories.Add(category);
        }

        var sidebar = new FloweryComponentSidebar { Categories = categories, Height = 600 };
        try
        {
            RuntimeTestHelpers.AttachToHost(sidebar);
            await RuntimeTestHelpers.EnsureLoadedAsync(sidebar).WaitAsync(TimeSpan.FromSeconds(5));

            // Apply each rebuild at Normal priority before the pending Low-priority item batch can run.
            foreach (var searchText in new[] { "t", "ta", "tab" })
            {
                sidebar.SearchText = searchText;
                await NextDispatchAsync(sidebar.DispatcherQueue, DispatcherQueuePriority.Normal);
            }

            // Each Low-priority turn lets another category fill; no elapsed-time polling is needed.
            for (var batch = 0; batch < categories.Count; batch++)
                await NextDispatchAsync(sidebar.DispatcherQueue, DispatcherQueuePriority.Low);
            sidebar.UpdateLayout();

            var expanders = FindDescendants<DaisyExpander>(sidebar).ToArray();
            Assert.HasCount(categories.Count, expanders);
            for (var categoryIndex = 0; categoryIndex < expanders.Length; categoryIndex++)
            {
                var expander = expanders[categoryIndex];
                Assert.IsTrue(expander.IsExpanded, $"Category {categoryIndex} must stay expanded after searching.");
                var items = expander.ExpanderContent as Panel;
                Assert.IsNotNull(items, $"Category {categoryIndex} matching 'tab' must contain its items.");
                Assert.HasCount(1, items.Children, $"Category {categoryIndex} must contain only its matching item.");
                var item = ((FrameworkElement)items.Children[0]).Tag as SidebarItem;
                Assert.IsNotNull(item);
                Assert.AreSame(categories[categoryIndex].Items[0], item);
                Assert.AreEqual($"Tab {categoryIndex}", item.DisplayName);
            }
        }
        finally
        {
            // Clear model subscriptions and any pending item queue before unloading the native control.
            sidebar.Categories = [];
            await NextDispatchAsync(sidebar.DispatcherQueue, DispatcherQueuePriority.Normal);
            await NextDispatchAsync(sidebar.DispatcherQueue, DispatcherQueuePriority.Low);
            await UnloadAsync(sidebar);
        }
    }

    private static async Task NextDispatchAsync(DispatcherQueue dispatcher, DispatcherQueuePriority priority)
    {
        var completion = new TaskCompletionSource();
        Assert.IsTrue(dispatcher.TryEnqueue(priority, () => completion.TrySetResult()),
            "The native dispatcher must accept the sidebar test barrier.");
        await completion.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    private static async Task UnloadAsync(FrameworkElement element)
    {
        if (!element.IsLoaded)
        {
            RuntimeTestHelpers.GetHostPanel().Children.Remove(element);
            return;
        }

        var completion = new TaskCompletionSource();
        void OnUnloaded(object sender, RoutedEventArgs args) => completion.TrySetResult();
        element.Unloaded += OnUnloaded;
        try
        {
            RuntimeTestHelpers.GetHostPanel().Children.Remove(element);
            await completion.Task.WaitAsync(TimeSpan.FromSeconds(5));
        }
        finally
        {
            element.Unloaded -= OnUnloaded;
        }
    }

    private static IEnumerable<T> FindDescendants<T>(DependencyObject root) where T : DependencyObject
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            if (child is T match)
                yield return match;
            foreach (var descendant in FindDescendants<T>(child))
                yield return descendant;
        }
    }
}
