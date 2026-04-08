using Ink_Canvas.Helpers;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Ink_Canvas
{
    public partial class MainWindow : Window
    {
        /// <summary>
        /// 从任意上下文向主窗口发送通知消息。
        /// </summary>
        public static void ShowNewMessage(string notice, bool isShowImmediately = true)
        {
            (Application.Current?.Windows.Cast<Window>().FirstOrDefault(window => window is MainWindow) as MainWindow)?.ShowNotificationAsync(notice, isShowImmediately);
        }

        /// <summary>
        /// 通知展示延时关闭的取消令牌源。
        /// </summary>
        private CancellationTokenSource ShowNotificationCancellationTokenSource = new CancellationTokenSource();

        /// <summary>
        /// 在通知区域显示消息，并在短暂延时后自动隐藏。
        /// </summary>
        public async void ShowNotificationAsync(string notice, bool isShowImmediately = true)
        {
            try
            {
                ShowNotificationCancellationTokenSource.Cancel();
                ShowNotificationCancellationTokenSource = new CancellationTokenSource();
                var token = ShowNotificationCancellationTokenSource.Token;

                TextBlockNotice.Text = notice;
                AnimationsHelper.ShowWithSlideFromBottomAndFade(GridNotifications);

                try
                {
                    await Task.Delay(2000, token);
                    AnimationsHelper.HideWithSlideAndFade(GridNotifications);
                }
                catch (TaskCanceledException) { }
            }
            catch { }
        }
    }
}
