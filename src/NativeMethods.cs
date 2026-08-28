using System;
using System.Runtime.InteropServices;

namespace PlasmaOldSchool
{
    internal static class NativeMethods
    {
        internal const int GwlStyle = -16;
        internal const int WsChild = 0x40000000;
        internal const int WsPopup = unchecked((int)0x80000000);

        [StructLayout(LayoutKind.Sequential)]
        internal struct Rect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool SetDllDirectory(string path);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern IntPtr SetParent(IntPtr childWindow, IntPtr newParentWindow);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool GetClientRect(IntPtr window, out Rect rectangle);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongW", SetLastError = true)]
        internal static extern int GetWindowLong(IntPtr window, int index);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongW", SetLastError = true)]
        internal static extern int SetWindowLong(IntPtr window, int index, int newValue);

        internal static void TryEnableDpiAwareness()
        {
            try
            {
                SetProcessDPIAware();
            }
            catch
            {
                // Windows anteriores pueden no exponer esta llamada.
            }
        }

        internal static void TrySetDllDirectory(string path)
        {
            if (String.IsNullOrEmpty(path)) return;
            try
            {
                SetDllDirectory(path);
            }
            catch
            {
                // OpenGL y CPU siguen disponibles si no puede cambiarse la ruta.
            }
        }
    }
}
