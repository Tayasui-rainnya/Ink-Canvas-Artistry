using iNKORE.UI.WPF.Modern;
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace Ink_Canvas
{
    public partial class ClockWindow : Window
    {
        // 是否全屏
        private bool isFullscreen;

        // 记录进入全屏前的窗口边界，用于退出全屏还原
        private double restoreLeft;
        private double restoreTop;
        private double restoreWidth;
        private double restoreHeight;

        private readonly DispatcherTimer timer = new DispatcherTimer();

        public ClockWindow()
        {
            InitializeComponent();

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

            timer.Interval = TimeSpan.FromMilliseconds(200);
            timer.Tick += Timer_Tick;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            SyncFullscreenButtonState();
            UpdateCurrentTime();
            timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            UpdateCurrentTime();
        }

        private void UpdateCurrentTime()
        {
            TextBlockCurrentTime.Text = DateTime.Now.ToString("HH:mm:ss");
        }

        private void BtnFullscreen_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!isFullscreen)
            {
                restoreLeft = Left;
                restoreTop = Top;
                restoreWidth = Width;
                restoreHeight = Height;

                WindowStyle = WindowStyle.None;
                ResizeMode = ResizeMode.NoResize;
                WindowState = WindowState.Maximized;
                isFullscreen = true;
            }
            else
            {
                WindowState = WindowState.Normal;
                WindowStyle = WindowStyle.SingleBorderWindow;
                ResizeMode = ResizeMode.CanResize;

                Left = restoreLeft;
                Top = restoreTop;
                Width = restoreWidth;
                Height = restoreHeight;
                isFullscreen = false;
            }

            // 同步全屏按钮图标与提示文本
            SyncFullscreenButtonState();
        }

        private void SyncFullscreenButtonState()
        {
            if (isFullscreen)
            {
                SymbolIconFullscreen.Glyph = "\uE92C";
                BtnFullscreen.ToolTip = "退出全屏";
            }
            else
            {
                SymbolIconFullscreen.Glyph = "\uE92D";
                BtnFullscreen.ToolTip = "全屏";
            }
        }

        private void BtnClose_MouseUp(object sender, MouseButtonEventArgs e)
        {
            timer.Stop();
            Close();
        }
    }
}
