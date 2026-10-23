using System;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Flowery.WinUI.RuntimeTests
{
    internal static class RuntimeTestHelpers
    {
        public static Panel GetHostPanel()
        {
            if (RuntimeTestContext.HostPanel is Panel hostPanel)
            {
                return hostPanel;
            }

            Assert.Inconclusive("Runtime test host panel is not initialized.");
            return new Grid();
        }

        public static void AttachToHost(FrameworkElement element)
        {
            var hostPanel = GetHostPanel();
            hostPanel.Children.Clear();
            hostPanel.Children.Add(element);
            element.UpdateLayout();
        }

        public static Task EnsureLoadedAsync(FrameworkElement element)
        {
            if (element.IsLoaded)
            {
                return Task.CompletedTask;
            }

            var tcs = new TaskCompletionSource<object?>();
            RoutedEventHandler? handler = null;
            handler = (_, _) =>
            {
                element.Loaded -= handler;
                tcs.TrySetResult(null);
            };
            element.Loaded += handler;
            return tcs.Task;
        }

        public static async Task CompleteLayoutAsync(FrameworkElement element, params FrameworkElement[] requiredElements)
        {
            await EnsureLoadedAsync(element).WaitAsync(TimeSpan.FromSeconds(5));
            foreach (var required in requiredElements)
                await EnsureLoadedAsync(required).WaitAsync(TimeSpan.FromSeconds(5));
            var completion = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
            if (!element.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
                {
                    element.UpdateLayout();
                    completion.TrySetResult(null);
                }))
                throw new InvalidOperationException("The runtime dispatcher rejected layout verification.");
            await completion.Task.WaitAsync(TimeSpan.FromSeconds(5));
        }
    }
}
