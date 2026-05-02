using Ink_Canvas.Helpers;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Ink_Canvas
{
    public partial class MainWindow : Window
    {
        /// <summary>
        /// 主窗口尚未就绪时的通知缓冲队列。
        /// </summary>
        private static readonly Queue<(string notice, bool isShowImmediately)> PendingNotifications = new Queue<(string notice, bool isShowImmediately)>();

        /// <summary>
        /// 通知缓冲队列的并发访问锁。
        /// </summary>
        private static readonly object PendingNotificationsLock = new object();

        /// <summary>
        /// 从任意上下文向主窗口发送通知消息。
        /// </summary>
        public static void ShowNewMessage(string notice, bool isShowImmediately = true)
        {
            MainWindow mainWindow = Application.Current?.Windows.Cast<Window>().FirstOrDefault(window => window is MainWindow) as MainWindow;
            if (mainWindow != null)
            {
                mainWindow.ShowNotificationAsync(notice, isShowImmediately);
                return;
            }

            lock (PendingNotificationsLock)
            {
                PendingNotifications.Enqueue((notice, isShowImmediately));
            }
            LogHelper.WriteLogToFile($"Notification queued before MainWindow is available: {notice}", LogHelper.LogType.Trace);
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
                FlushPendingNotifications();
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

        /// <summary>
        /// 将窗口创建前缓冲的通知按顺序回放到当前窗口。
        /// </summary>
        private void FlushPendingNotifications()
        {
            while (true)
            {
                (string notice, bool isShowImmediately) notification;
                lock (PendingNotificationsLock)
                {
                    if (PendingNotifications.Count == 0) return;
                    notification = PendingNotifications.Dequeue();
                }

                TextBlockNotice.Text = notification.notice;
                AnimationsHelper.ShowWithSlideFromBottomAndFade(GridNotifications);
            }
        }
    }
}
