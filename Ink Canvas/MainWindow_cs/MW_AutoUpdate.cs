using Ink_Canvas.Helpers;
using System.Windows;

namespace Ink_Canvas
{
    public partial class MainWindow : Window
    {
        /// <summary>
        /// 自动检查并下载新版本，如果有新版本则提示用户或静默更新
        /// </summary>
        private async void AutoUpdate()
        {
            // 检查是否有新版本
            AvailableLatestVersion = await AutoUpdateHelper.CheckForUpdates();

            if (AvailableLatestVersion != null)
            {
                bool IsDownloadSuccessful = false;
                // 下载新版本安装包并保存状态
                IsDownloadSuccessful = await AutoUpdateHelper.DownloadSetupFileAndSaveStatus(AvailableLatestVersion);

                if (IsDownloadSuccessful)
                {
                    // 非静默更新时弹窗询问用户是否立即更新
                    if (!Settings.Startup.IsAutoUpdateWithSilence)
                    {
                        MessageBoxResult result = MessageBox.Show(
                            $"Ink Canvas Artistry 新版本 (v{AvailableLatestVersion}) 安装包已下载完成，是否立即更新？",
                            "Ink Canvas Artistry - 新版本可用",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question);

                        if (result == MessageBoxResult.Yes)
                        {
                            // 用户同意后安装新版本
                            AutoUpdateHelper.InstallNewVersionApp(AvailableLatestVersion, false);
                        }
                    }
                    else
                    {
                        // 静默更新时启动定时器
                        if (timerCheckAutoUpdateWithSilence != null)
                        {
                            timerCheckAutoUpdateWithSilence.Enabled = true;
                            timerCheckAutoUpdateWithSilence.Start();
                            LogHelper.WriteLogToFile($"AutoUpdate | Silent update timer started for version {AvailableLatestVersion}.");
                        }
                        else
                        {
                            LogHelper.WriteLogToFile($"AutoUpdate | timerCheckAutoUpdateWithSilence is null.", LogHelper.LogType.Error);
                        }
                    }
                }
                else
                {
                    // 下载失败写入日志
                    LogHelper.WriteLogToFile($"AutoUpdate | Download failed for version {AvailableLatestVersion}.");
                }
            }
            else
            {
                // 未检测到新版本或检测失败，写入日志并清理更新文件夹
                LogHelper.WriteLogToFile($"AutoUpdate | No new version found or failed to check.");
                AutoUpdateHelper.DeleteUpdatesFolder();
            }
        }
    }
}