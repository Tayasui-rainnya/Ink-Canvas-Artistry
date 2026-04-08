using Ink_Canvas.Helpers;
using iNKORE.UI.WPF.Modern;
using System;
using System.Windows;
using System.Windows.Input;

namespace Ink_Canvas
{
    /// <summary>
    /// Interaction logic for StopwatchWindow.xaml
    /// </summary>
    public partial class OperatingGuideWindow : Window
    {
        /// <summary>
        /// 操作指南窗口构造函数。
        /// </summary>
        public OperatingGuideWindow()
        {
            InitializeComponent();
            AnimationsHelper.ShowWithSlideFromBottomAndFade(this, 0.25);
            MainWindow mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow != null)
            {
                if (mainWindow.GetMainWindowTheme() == "Light")
                {
                    ThemeManager.SetRequestedTheme(this, ElementTheme.Light);
                    ResourceDictionary rd = new ResourceDictionary() { Source = new Uri("Resources/Styles/Light-PopupWindow.xaml", UriKind.Relative) };
                    Application.Current.Resources.MergedDictionaries.Add(rd);
                }
                else
                {
                    ThemeManager.SetRequestedTheme(this, ElementTheme.Dark);
                    ResourceDictionary rd = new ResourceDictionary() { Source = new Uri("Resources/Styles/Dark-PopupWindow.xaml", UriKind.Relative) };
                    Application.Current.Resources.MergedDictionaries.Add(rd);
                }
            }
        }

        /// <summary>
        /// 点击关闭按钮。
        /// </summary>
        private void BtnClose_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Close();
        }

        /// <summary>
        /// 窗口拖动事件。
        /// </summary>
        private void WindowDragMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) DragMove();
        }

        /// <summary>
        /// 抑制触控边界反馈动画。
        /// </summary>
        private void SCManipulationBoundaryFeedback(object sender, ManipulationBoundaryFeedbackEventArgs e) {
            e.Handled = true;
        }
    }
}
