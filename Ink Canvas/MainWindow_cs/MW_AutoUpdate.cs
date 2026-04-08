using Ink_Canvas.Helpers;
using System.Windows;

namespace Ink_Canvas
{
    public partial class MainWindow : Window
    {
        /// <summary>
        /// 自动更新主流程：检查版本、下载安装包，并根据设置触发立即安装或静默时段安装。
        /// </summary>
        private async void AutoUpdate()
        {
            AvailableLatestVersion = await AutoUpdateHelper.CheckForUpdates();

            if (AvailableLatestVersion != null)
            {
                bool IsDownloadSuccessful = false;
                IsDownloadSuccessful = await AutoUpdateHelper.DownloadSetupFileAndSaveStatus(AvailableLatestVersion);

                if (IsDownloadSuccessful)
                {
                    if (!Settings.Startup.IsAutoUpdateWithSilence)
                    {
                        MessageBoxResult result = MessageBox.Show(
                            $"Ink Canvas Artistry 新版本 (v{AvailableLatestVersion}) 安装包已下载完成，是否立即更新？",
                            "Ink Canvas Artistry - 新版本可用",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question);

                        if (result == MessageBoxResult.Yes)
                        {
                            AutoUpdateHelper.InstallNewVersionApp(AvailableLatestVersion, false);
                        }
                    }
                    else
                    {
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
                    LogHelper.WriteLogToFile($"AutoUpdate | Download failed for version {AvailableLatestVersion}.");
                }
            }
            else
            {
                LogHelper.WriteLogToFile($"AutoUpdate | No new version found or failed to check.");
                AutoUpdateHelper.DeleteUpdatesFolder();
            }
        }
    }
}
