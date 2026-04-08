using System.Windows.Automation;

namespace Ink_Canvas.Helpers
{
    /// <summary>
    /// 基于 UI Automation 的窗口存在性检查工具。
    /// </summary>
    internal class WinTabWindowsChecker
    {
        /*
        public static bool IsWindowMinimized(string windowName, bool matchFullName = true) {
            // 获取Win+Tab预览中的窗口
            AutomationElementCollection windows = AutomationElement.RootElement.FindAll(
                TreeScope.Children, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Window));

            foreach (AutomationElement window in windows) {
                //LogHelper.WriteLogToFile("" + window.Current.Name);

                string windowTitle = window.Current.Name;

                // 如果窗口标题包含 windowName，则进行检查
                if (!string.IsNullOrEmpty(windowTitle) && windowTitle.Contains(windowName)) {
                    if (matchFullName) {
                        if (windowTitle.Length == windowName.Length) {
                            // 检查窗口是否最小化
                            WindowPattern windowPattern = window.GetCurrentPattern(WindowPattern.Pattern) as WindowPattern;
                            if (windowPattern != null) {
                                bool isMinimized = windowPattern.Current.WindowVisualState == WindowVisualState.Minimized;
                                //LogHelper.WriteLogToFile("" + windowTitle + isMinimized);
                                return isMinimized;
                            }
                        }
                    } else {
                        // 检查窗口是否最小化
                        WindowPattern windowPattern = window.GetCurrentPattern(WindowPattern.Pattern) as WindowPattern;
                        if (windowPattern != null) {
                            bool isMinimized = windowPattern.Current.WindowVisualState == WindowVisualState.Minimized;
                            return isMinimized;
                        }
                    }
                }
            }
            // 未找到软件白板窗口
            return true;
        }
        */

        /// <summary>
        /// 检查指定标题窗口是否存在。
        /// </summary>
        /// <param name="windowName">目标窗口标题或标题关键字。</param>
        /// <param name="matchFullName">是否要求完整标题匹配。</param>
        /// <returns>存在返回 <c>true</c>，否则返回 <c>false</c>。</returns>
        public static bool IsWindowExisted(string windowName, bool matchFullName = true)
        {
            // 获取Win+Tab预览中的窗口
            AutomationElementCollection windows = AutomationElement.RootElement.FindAll(
                TreeScope.Children, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Window));

            foreach (AutomationElement window in windows)
            {
                //LogHelper.WriteLogToFile("" + window.Current.Name);

                string windowTitle = window.Current.Name;

                // 如果窗口标题包含 windowName，则进行检查
                if (!string.IsNullOrEmpty(windowTitle) && windowTitle.Contains(windowName))
                {
                    if (matchFullName)
                    {
                        if (windowTitle.Length == windowName.Length)
                        {
                            WindowPattern windowPattern = window.GetCurrentPattern(WindowPattern.Pattern) as WindowPattern;
                            if (windowPattern != null)
                            {
                                return true;
                            }
                        }
                    }
                    else
                    {
                        WindowPattern windowPattern = window.GetCurrentPattern(WindowPattern.Pattern) as WindowPattern;
                        if (windowPattern != null)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }
}
