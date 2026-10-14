using System;

namespace Flowery.Services
{
    internal sealed class FloweryPlatformLogSink : IFloweryLogSink
    {
        public void Log(FloweryLogEntry entry)
        {
            var message = FloweryLogging.Format(entry);
            FloweryPlatformLogger.WriteLine(entry.Level, entry.Category, message);
        }
    }

    internal static class FloweryPlatformLogger
    {
        internal static void WriteLine(FloweryLogLevel level, string category, string message)
        {
            System.Diagnostics.Debug.WriteLine(message);
        }

    }
}
