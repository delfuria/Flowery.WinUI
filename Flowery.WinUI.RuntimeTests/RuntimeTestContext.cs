using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Flowery.WinUI.RuntimeTests
{
    public static class RuntimeTestContext
    {
        public static Window? Window { get; private set; }
        public static Panel? HostPanel { get; private set; }

        internal static void Initialize(Window window, Panel hostPanel)
        {
            Window = window;
            HostPanel = hostPanel;
        }
    }
}
