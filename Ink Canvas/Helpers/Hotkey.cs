using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace Ink_Canvas
{
    /// <summary>
    /// 全局快捷键注册与分发工具类。
    /// </summary>
    static class Hotkey
    {
        #region 系统api
        /// <summary>
        /// 调用 Win32 API 注册系统级快捷键。
        /// </summary>
        /// <param name="hWnd">接收热键消息的窗口句柄。</param>
        /// <param name="id">热键唯一标识。</param>
        /// <param name="fsModifiers">组合键修饰符。</param>
        /// <param name="vk">虚拟键码。</param>
        /// <returns>注册是否成功。</returns>
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool RegisterHotKey(IntPtr hWnd, int id, HotkeyModifiers fsModifiers, uint vk);

        /// <summary>
        /// 调用 Win32 API 注销系统级快捷键。
        /// </summary>
        /// <param name="hWnd">接收热键消息的窗口句柄。</param>
        /// <param name="id">热键唯一标识。</param>
        /// <returns>注销是否成功。</returns>
        [DllImport("user32.dll")]
        static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        #endregion

        /// <summary>
        /// 注册快捷键
        /// </summary>
        /// <param name="window">持有快捷键窗口</param>
        /// <param name="fsModifiers">组合键</param>
        /// <param name="key">快捷键</param>
        /// <param name="callBack">回调函数</param>
        public static bool Regist(Window window, HotkeyModifiers fsModifiers, Key key, HotKeyCallBackHanlder callBack)
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            var _hwndSource = HwndSource.FromHwnd(hwnd);

            if (keyid == 10)
            {
                _hwndSource.AddHook(WndProc);
            }

            int id = keyid++;

            var vk = KeyInterop.VirtualKeyFromKey(key);
            if (!RegisterHotKey(hwnd, id, fsModifiers, (uint)vk))
            {
                //throw new Exception("regist hotkey fail.");
                return false;
            }
            keymap[id] = callBack;
            return true;
        }

        /// <summary> 
        /// 快捷键消息处理 
        /// </summary> 
        /// <param name="hwnd">窗口句柄。</param>
        /// <param name="msg">窗口消息编号。</param>
        /// <param name="wParam">消息参数，包含热键 id。</param>
        /// <param name="lParam">消息参数，包含组合键信息。</param>
        /// <param name="handled">是否已处理该消息。</param>
        /// <returns>默认返回 <see cref="IntPtr.Zero"/>。</returns>
        static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY)
            {
                int id = wParam.ToInt32();
                if (keymap.TryGetValue(id, out var callback))
                {
                    callback();
                }
                handled = true;
            }
            return IntPtr.Zero;
        }

        /// <summary> 
        /// 注销快捷键 
        /// </summary> 
        /// <param name="hWnd">持有快捷键窗口的句柄</param> 
        /// <param name="callBack">回调函数</param> 
        public static void UnRegist(IntPtr hWnd, HotKeyCallBackHanlder callBack)
        {
            foreach (KeyValuePair<int, HotKeyCallBackHanlder> var in keymap)
            {
                if (var.Value == callBack)
                    UnregisterHotKey(hWnd, var.Key);
            }
        }

        /// <summary>
        /// Windows 热键消息编号。
        /// </summary>
        const int WM_HOTKEY = 0x312;

        /// <summary>
        /// 应用内热键 id 起始值（避免与保留值冲突）。
        /// </summary>
        static int keyid = 10;

        /// <summary>
        /// 热键 id 与回调函数的映射表。
        /// </summary>
        static Dictionary<int, HotKeyCallBackHanlder> keymap = new Dictionary<int, HotKeyCallBackHanlder>();

        /// <summary>
        /// 热键触发后的回调委托。
        /// </summary>
        public delegate void HotKeyCallBackHanlder();
    }

    /// <summary>
    /// 热键组合修饰符。
    /// </summary>
    enum HotkeyModifiers
    {
        /// <summary>Alt 键。</summary>
        MOD_ALT = 0x1,
        /// <summary>Ctrl 键。</summary>
        MOD_CONTROL = 0x2,
        /// <summary>Shift 键。</summary>
        MOD_SHIFT = 0x4,
        /// <summary>Win 键。</summary>
        MOD_WIN = 0x8
    }
}
