using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace WorkoutCatalog.Helpers;

public static class WindowHelper
{
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

    public static void ApplyDarkMode(Window window)
    {
        var hwnd = new WindowInteropHelper(window).EnsureHandle();
        int value = 1;
        DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref value, sizeof(int));
    }
}
