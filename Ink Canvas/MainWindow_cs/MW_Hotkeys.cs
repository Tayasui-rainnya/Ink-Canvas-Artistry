using System.Windows;
using System.Windows.Input;

namespace Ink_Canvas
{
    public partial class MainWindow : Window
    {
        /// <summary>
        /// 注册应用级全局快捷键。
        /// </summary>
        private void RegisterGlobalHotkeys()
        {
            Hotkey.Regist(this, HotkeyModifiers.MOD_SHIFT, Key.Escape, HotKey_ExitPPTSlideShow);
            Hotkey.Regist(this, HotkeyModifiers.MOD_CONTROL, Key.E, HotKey_Clear);
            Hotkey.Regist(this, HotkeyModifiers.MOD_ALT, Key.C, HotKey_Capture);
            Hotkey.Regist(this, HotkeyModifiers.MOD_ALT, Key.V, HotKey_Hide);
            Hotkey.Regist(this, HotkeyModifiers.MOD_ALT, Key.D, HotKey_DrawTool);
            Hotkey.Regist(this, HotkeyModifiers.MOD_ALT, Key.Q, HotKey_QuitDrawMode);
            Hotkey.Regist(this, HotkeyModifiers.MOD_ALT, Key.B, HotKey_Board);
        }

        /// <summary>
        /// 快捷键：结束当前 PPT 放映。
        /// </summary>
        private void HotKey_ExitPPTSlideShow()
        {
            if(BtnPPTSlideShowEnd.Visibility == Visibility.Visible)
            {
                BtnPPTSlideShowEnd_Click(null, null);
            }
        }

        /// <summary>
        /// 快捷键：清空当前内容。
        /// </summary>
        private void HotKey_Clear()
        {
            SymbolIconDelete_MouseUp(null, null);
        }

        /// <summary>
        /// 快捷键：保存截图到桌面。
        /// </summary>
        private void HotKey_Capture()
        {
            SaveScreenShotToDesktop();
        }
        
        /// <summary>
        /// 快捷键：隐藏/显示界面（表情按钮逻辑）。
        /// </summary>
        private void HotKey_Hide()
        {
            SymbolIconEmoji_MouseUp(null, null);
        }

        /// <summary>
        /// 快捷键：切换到画笔工具。
        /// </summary>
        private void HotKey_DrawTool()
        {
            PenIcon_Click(null, null);
        }

        /// <summary>
        /// 快捷键：退出绘制模式并回到光标状态。
        /// </summary>
        private void HotKey_QuitDrawMode()
        {
            if (currentMode != 0)
            {
                ImageBlackboard_Click(null, null);
            }
            CursorIcon_Click(null, null);
        }

        /// <summary>
        /// 快捷键：进入白板模式。
        /// </summary>
        private void HotKey_Board()
        {
            ImageBlackboard_Click(null, null);
        }

        /// <summary>
        /// 鼠标滚轮翻页（仅在 PPT 放映模式下生效）。
        /// </summary>
        private void Window_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (BtnPPTSlideShowEnd.Visibility != Visibility.Visible || currentMode != 0) return;
            if (e.Delta >= 120)
            {
                BtnPPTSlidesUp_Click(null, null);
            }
            else if (e.Delta <= -120)
            {
                BtnPPTSlidesDown_Click(null, null);
            }
        }

        /// <summary>
        /// 键盘翻页快捷处理（仅在 PPT 放映模式下生效）。
        /// </summary>
        private void Main_Grid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (BtnPPTSlideShowEnd.Visibility != Visibility.Visible || currentMode != 0) return;

            if (e.Key == Key.Down || e.Key == Key.PageDown || e.Key == Key.Right || e.Key == Key.N || e.Key == Key.Space)
            {
                BtnPPTSlidesDown_Click(null, null);
            }
            if (e.Key == Key.Up || e.Key == Key.PageUp || e.Key == Key.Left || e.Key == Key.P)
            {
                BtnPPTSlidesUp_Click(null, null);
            }
        }

        /// <summary>
        /// 窗口级按键处理（Esc 退出放映）。
        /// </summary>
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                KeyExit(null, null);
            }
        }

        /// <summary>
        /// 命令可执行性统一放行。
        /// </summary>
        private void CommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        /// <summary>
        /// 命令：撤销。
        /// </summary>
        private void HotKey_Undo(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                SymbolIconUndo_Click(null, null);
            }
            catch { }
        }

        /// <summary>
        /// 命令：重做。
        /// </summary>
        private void HotKey_Redo(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                SymbolIconRedo_Click(null, null);
            }
            catch { }
        }

        /// <summary>
        /// 命令：退出放映。
        /// </summary>
        private void KeyExit(object sender, ExecutedRoutedEventArgs e)
        {
            BtnPPTSlideShowEnd_Click(null, null);
        }

        /// <summary>
        /// 命令：切换到选择工具。
        /// </summary>
        private void KeyChangeToSelect(object sender, ExecutedRoutedEventArgs e)
        {
            if (StackPanelCanvasControls.Visibility == Visibility.Visible)
            {
                SymbolIconSelect_Click(null, null);
            }
        }

        /// <summary>
        /// 命令：切换到橡皮擦工具。
        /// </summary>
        private void KeyChangeToEraser(object sender, ExecutedRoutedEventArgs e)
        {
            if (StackPanelCanvasControls.Visibility == Visibility.Visible)
            {
                if (Eraser_Icon.Background != null)
                {
                    EraserIconByStrokes_Click(null, null);
                }
                else
                {
                    EraserIcon_Click(null, null);
                }
            }
        }

        /// <summary>
        /// 命令：切换到直线绘制。
        /// </summary>
        private void KeyDrawLine(object sender, ExecutedRoutedEventArgs e)
        {
            if (StackPanelCanvasControls.Visibility == Visibility.Visible)
            {
                BtnDrawLine_Click(lastMouseDownSender, null);
            }
        }
    }
}
