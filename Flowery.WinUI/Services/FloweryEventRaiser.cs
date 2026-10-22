using System;

namespace Flowery.Services
{
    /// <summary>
    /// Raises library-wide events one handler at a time. A plain multicast invoke stops at the first handler that
    /// throws, so every control subscribed after it would miss a theme or contrast change.
    /// </summary>
    internal static class FloweryEventRaiser
    {
        private const string LogCategory = "Events";

        internal static void Raise(EventHandler? handlers, object? sender, EventArgs args, string eventName)
        {
            if (handlers is null)
                return;

            foreach (EventHandler handler in handlers.GetInvocationList())
            {
                try
                {
                    handler(sender, args);
                }
                catch (Exception ex)
                {
                    LogFailure(eventName, handler, ex);
                }
            }
        }

        internal static void Raise<TArgs>(EventHandler<TArgs>? handlers, object? sender, TArgs args, string eventName)
        {
            if (handlers is null)
                return;

            foreach (EventHandler<TArgs> handler in handlers.GetInvocationList())
            {
                try
                {
                    handler(sender, args);
                }
                catch (Exception ex)
                {
                    LogFailure(eventName, handler, ex);
                }
            }
        }

        private static void LogFailure(string eventName, Delegate handler, Exception ex)
        {
            FloweryLogging.Log(FloweryLogLevel.Error, LogCategory,
                $"[{eventName}] {handler.Method.DeclaringType?.Name}.{handler.Method.Name} failed: {ex.Message}", ex);
        }
    }
}
