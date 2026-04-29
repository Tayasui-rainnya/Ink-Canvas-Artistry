using iNKORE.UI.WPF.Modern;
using System;
using System.Windows.Media;
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
                    this.Resources.MergedDictionaries.Add(rd);
                }
                else
                {
                    ThemeManager.SetRequestedTheme(this, ElementTheme.Dark);
                    ResourceDictionary rd = new ResourceDictionary() { Source = new Uri("Resources/Styles/Dark-PopupWindow.xaml", UriKind.Relative) };
                    this.Resources.MergedDictionaries.Add(rd);
                }
            }

            timer.Interval = TimeSpan.FromMilliseconds(200);
            timer.Tick += Timer_Tick;
            Closed += ClockWindow_Closed;
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

        private void BtnFullscreen_Click(object sender, RoutedEventArgs e)
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
                Topmost = true;
                isFullscreen = true;
                RootBorder.Margin = new Thickness(0);
                RootBorder.Background = Brushes.Black;
                RootBorder.BorderBrush = Brushes.Black;
                RootBorder.BorderThickness = new Thickness(0);
                TextBlockCurrentTime.Foreground = Brushes.White;
            }
            else
            {
                WindowState = WindowState.Normal;
                WindowStyle = WindowStyle.SingleBorderWindow;
                ResizeMode = ResizeMode.CanResize;
                Topmost = true;

                Left = restoreLeft;
                Top = restoreTop;
                Width = restoreWidth;
                Height = restoreHeight;
                isFullscreen = false;
                RootBorder.Margin = new Thickness(20);
                RootBorder.Background = (Brush)FindResource("PopupWindowBackground");
                RootBorder.BorderBrush = (Brush)FindResource("PopupWindowBorderBrush");
                RootBorder.BorderThickness = new Thickness(1);
                TextBlockCurrentTime.Foreground = (Brush)FindResource("PopupWindowForeground");
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

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            CloseClockWindow();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Escape) return;
            // Esc 与右下角“×”共用同一关闭路径，确保计时器释放与关闭行为一致。
            CloseClockWindow();
        }

        private void CloseClockWindow()
        {
            // 统一关闭入口，避免未来多入口关闭时出现资源释放不一致。
            timer.Stop();
            Close();
        }

        private void ClockWindow_Closed(object sender, EventArgs e)
        {
            timer.Stop();
            timer.Tick -= Timer_Tick;
            Closed -= ClockWindow_Closed;
        }
    }
}
