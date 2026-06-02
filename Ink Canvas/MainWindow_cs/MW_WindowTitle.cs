using System.Windows;

namespace Ink_Canvas
{
    public partial class MainWindow : Window
    {
        private const string ScreenAnnotationWindowTitle = "Ink Canvas Artistry 屏幕批注";
        private const string PowerPointAnnotationWindowTitle = "Ink Canvas Artistry PPT批注";
        private const string WhiteboardWindowTitle = "Ink Canvas Artistry 白板";

        /// <summary>
        /// 根据当前 PowerPoint 放映、白板或屏幕批注状态刷新主窗口标题。
        /// </summary>
        private void UpdateWindowTitle()
        {
            if (BtnPPTSlideShowEnd.Visibility == Visibility.Visible)
            {
                Title = PowerPointAnnotationWindowTitle;
                return;
            }

            Title = currentMode == 0 ? ScreenAnnotationWindowTitle : WhiteboardWindowTitle;
        }
    }
}
