using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Windows.Graphics;

namespace Flowery.WinUI.Gallery;

internal static class NativeWindowPlacement
{
    public static string? ApplyIfRequested(Window window)
    {
        var requestedDevice = Environment.GetEnvironmentVariable("FLOWERY_MONITOR_DEVICE");
        if (string.IsNullOrWhiteSpace(requestedDevice))
            return null;

        var region = Environment.GetEnvironmentVariable("FLOWERY_MONITOR_REGION") ?? "full";
        if (!string.Equals(region, "full", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(region, "lower-half", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("FLOWERY_MONITOR_REGION must be 'full' or 'lower-half'.");
        }

        DisplayArea? selected = null;
        var displays = DisplayArea.FindAll();
        for (var index = 0; index < displays.Count; index++)
        {
            var display = displays[index];
            var monitor = Win32Interop.GetMonitorFromDisplayId(display.DisplayId);
            var information = new MonitorInfoEx
            {
                Size = (uint)Marshal.SizeOf<MonitorInfoEx>(),
                DeviceName = string.Empty
            };
            if (!GetMonitorInfo(monitor, ref information))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to read Windows monitor information.");

            if (string.Equals(information.DeviceName, requestedDevice, StringComparison.OrdinalIgnoreCase))
            {
                selected = display;
                break;
            }
        }

        if (selected == null)
            throw new InvalidOperationException($"Requested Windows monitor '{requestedDevice}' is not available.");

        var workArea = selected.WorkArea;
        var desired = workArea;
        if (string.Equals(region, "lower-half", StringComparison.OrdinalIgnoreCase))
        {
            desired.Y += workArea.Height / 2;
            desired.Height = workArea.Height - workArea.Height / 2;
        }

        var appWindow = window.AppWindow;
        appWindow.MoveAndResize(desired);
        var position = appWindow.Position;
        var size = appWindow.Size;
        var bounds = new RectInt32(position.X, position.Y, size.Width, size.Height);
        if (bounds.X < desired.X || bounds.Y < desired.Y
            || bounds.X + bounds.Width > desired.X + desired.Width
            || bounds.Y + bounds.Height > desired.Y + desired.Height)
        {
            throw new InvalidOperationException($"Window placement exceeded the requested monitor region: {Format(bounds)}.");
        }

        var portrait = selected.OuterBounds.Height > selected.OuterBounds.Width;
        return $"Device={requestedDevice}; Region={region}; Portrait={portrait}; IsPrimary={selected.IsPrimary}; "
            + $"WorkArea={Format(workArea)}; WindowBounds={Format(bounds)}; Activated=False";
    }

    private static string Format(RectInt32 bounds) => $"{bounds.X},{bounds.Y},{bounds.Width},{bounds.Height}";

    [DllImport("user32.dll", EntryPoint = "GetMonitorInfoW", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfoEx information);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct MonitorInfoEx
    {
        public uint Size;
        public NativeRect Monitor;
        public NativeRect WorkArea;
        public uint Flags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DeviceName;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}
