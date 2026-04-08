using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Ink_Canvas.Helpers
{
    /// <summary>
    /// 前台窗口信息读取工具：窗口标题、类名、位置尺寸与进程名。
    /// </summary>
    internal class ForegroundWindowInfo
    {
        /// <summary>
        /// 获取当前前台窗口句柄。
        /// </summary>
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;

            public int Width => Right - Left;
            public int Height => Bottom - Top;
        }

        /// <summary>
        /// 读取窗口标题文本。
        /// </summary>
        public static string WindowTitle()
        {
            IntPtr foregroundWindowHandle = GetForegroundWindow();

            const int nChars = 256;
            StringBuilder windowTitle = new StringBuilder(nChars);
            GetWindowText(foregroundWindowHandle, windowTitle, nChars);

            return windowTitle.ToString();
        }

        /// <summary>
        /// 读取窗口类名。
        /// </summary>
        public static string WindowClassName()
        {
            IntPtr foregroundWindowHandle = GetForegroundWindow();

            const int nChars = 256;
            StringBuilder className = new StringBuilder(nChars);
            GetClassName(foregroundWindowHandle, className, nChars);

            return className.ToString();
        }

        /// <summary>
        /// 读取窗口矩形信息（左上右下坐标及宽高）。
        /// </summary>
        public static RECT WindowRect()
        {
            IntPtr foregroundWindowHandle = GetForegroundWindow();

            RECT windowRect;
            GetWindowRect(foregroundWindowHandle, out windowRect);

            return windowRect;
        }

        /// <summary>
        /// 获取前台窗口所属进程名称。
        /// </summary>
        /// <returns>进程名；若无法获取则返回 <c>Unknown</c>。</returns>
        public static string ProcessName()
        {
            IntPtr foregroundWindowHandle = GetForegroundWindow();
            uint processId;
            GetWindowThreadProcessId(foregroundWindowHandle, out processId);

            try
            {
                Process process = Process.GetProcessById((int)processId);
                return process.ProcessName;
            }
            catch (ArgumentException)
            {
                // Process with the given ID not found
                return "Unknown";
            }
        }
    }
}
