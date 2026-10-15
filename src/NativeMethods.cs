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

        [StructLayout(LayoutKind.Sequential)]
        internal struct Point
        {
            public int X;
            public int Y;
        }

        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool SetDllDirectory(string path);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern IntPtr SetParent(IntPtr childWindow, IntPtr newParentWindow);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool GetClientRect(IntPtr window, out Rect rectangle);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int virtualKey);

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out Point point);

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

        // El salvapantallas cubre todas las pantallas con una ventana por
        // monitor, pero solo la que tiene el foco recibe teclado y solo la que
        // esta bajo el cursor recibe raton. Para cerrarlo con fiabilidad hay
        // que consultar el estado global del teclado y del puntero.
        internal static bool TryGetCursorPosition(out System.Drawing.Point position)
        {
            Point native;
            if (GetCursorPos(out native))
            {
                position = new System.Drawing.Point(native.X, native.Y);
                return true;
            }
            position = System.Drawing.Point.Empty;
            return false;
        }

        internal static bool IsAnyInputKeyPressed()
        {
            int virtualKey;
            for (virtualKey = 1; virtualKey <= 254; virtualKey++)
            {
                // Se omiten los botones del raton: su pulsacion se trata
                // aparte para no confundirla con un movimiento involuntario.
                if (virtualKey == 1 || virtualKey == 2 || virtualKey == 4)
                {
                    continue;
                }
                if ((GetAsyncKeyState(virtualKey) & 0x8000) != 0)
                {
                    return true;
                }
            }
            return false;
        }

        internal static bool IsAnyMouseButtonPressed()
        {
            return (GetAsyncKeyState(1) & 0x8000) != 0 ||
                (GetAsyncKeyState(2) & 0x8000) != 0 ||
                (GetAsyncKeyState(4) & 0x8000) != 0;
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
