using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Reflection;
using System.Windows;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Controls;

namespace Ink_Canvas.Helpers
{
    /// <summary>
    /// 自动更新辅助类：负责检查版本、下载安装包、执行安装与清理更新目录。
    /// </summary>
    internal class AutoUpdateHelper
    {
        /// <summary>
        /// 更新服务基础地址。
        /// </summary>
        private const string UpdateServerBaseUrl = "http://8.134.100.248:8080";

        /// <summary>
        /// 检查服务器版本是否高于当前本地版本。
        /// </summary>
        /// <returns>若有更新返回远端版本号；否则返回 <c>null</c>。</returns>
        public static async Task<string> CheckForUpdates()
        {
            try
            {
                string localVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
                string remoteAddress = $"{UpdateServerBaseUrl}/version";
                string remoteVersion = await GetRemoteVersion(remoteAddress);

                if (remoteVersion != null)
                {
                    Version local = new Version(localVersion);
                    Version remote = new Version(remoteVersion.Trim());
                    if (remote > local)
                    {
                        LogHelper.WriteLogToFile("AutoUpdate | New version Available: " + remoteVersion);
                        return remoteVersion.Trim();
                    }
                    else
                    {
                        LogHelper.WriteLogToFile("AutoUpdate | Local version is up-to-date or newer.");
                        return null;
                    }
                }
                else
                {
                    LogHelper.WriteLogToFile("Failed to retrieve remote version.", LogHelper.LogType.Error);
                    return null;
                }
            }
            catch (FormatException ex)
            {
                LogHelper.WriteLogToFile($"AutoUpdate | Version format error: {ex.Message}", LogHelper.LogType.Error);
                return null;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"AutoUpdate | Error checking for updates: {ex.Message}", LogHelper.LogType.Error);
                return null;
            }
        }

        /// <summary>
        /// 从指定地址读取远程版本号文本。
        /// </summary>
        /// <param name="fileUrl">版本文件 URL。</param>
        /// <returns>去除空白后的版本字符串；失败时返回 <c>null</c>。</returns>
        public static async Task<string> GetRemoteVersion(string fileUrl)
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    client.Timeout = TimeSpan.FromSeconds(15);
                    HttpResponseMessage response = await client.GetAsync(fileUrl);
                    response.EnsureSuccessStatusCode();

                    string versionString = await response.Content.ReadAsStringAsync();
                    return versionString?.Trim();
                }
                catch (HttpRequestException ex)
                {
                    LogHelper.WriteLogToFile($"AutoUpdate | HTTP request error getting version from {fileUrl}: {ex.Message}", LogHelper.LogType.Error);
                }
                catch (TaskCanceledException ex)
                {
                    LogHelper.WriteLogToFile($"AutoUpdate | Timeout getting version from {fileUrl}: {ex.Message}", LogHelper.LogType.Error);
                }
                catch (Exception ex)
                {
                    LogHelper.WriteLogToFile($"AutoUpdate | Error getting remote version from {fileUrl}: {ex.Message}", LogHelper.LogType.Error);
                }
                return null;
            }
        }

        /// <summary>
        /// 本地更新缓存目录路径。
        /// </summary>
        private static string updatesFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Ink Canvas Artistry", "AutoUpdate");

        /// <summary>
        /// 下载状态记录文件路径（按版本生成）。
        /// </summary>
        private static string statusFilePath = null;

        /// <summary>
        /// 下载指定版本安装包，并写入下载状态标记文件。
        /// </summary>
        /// <param name="version">目标版本号。</param>
        /// <returns>下载成功返回 <c>true</c>，否则返回 <c>false</c>。</returns>
        public static async Task<bool> DownloadSetupFileAndSaveStatus(string version)
        {
            try
            {
                statusFilePath = Path.Combine(updatesFolderPath, $"DownloadV{version}Status.txt");

                if (File.Exists(statusFilePath) && File.ReadAllText(statusFilePath).Trim().ToLower() == "true")
                {
                    LogHelper.WriteLogToFile("AutoUpdate | Setup file already downloaded.");
                    return true;
                }

                string setupFileName = $"Ink.Canvas.Artistry.V{version}.Setup.exe";
                string downloadUrl = $"{UpdateServerBaseUrl}/download/{setupFileName}";
                string destinationPath = Path.Combine(updatesFolderPath, setupFileName);

                LogHelper.WriteLogToFile($"AutoUpdate | Attempting download from: {downloadUrl} to {destinationPath}");

                SaveDownloadStatus(false);
                await DownloadFile(downloadUrl, destinationPath);
                SaveDownloadStatus(true);

                LogHelper.WriteLogToFile("AutoUpdate | Setup file successfully downloaded.");
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"AutoUpdate | Error downloading setup file for version {version}: {ex.Message}", LogHelper.LogType.Error);
                SaveDownloadStatus(false);
                try
                {
                    string setupFileName = $"Ink.Canvas.Artistry.V{version}.Setup.exe";
                    string destinationPath = Path.Combine(updatesFolderPath, setupFileName);
                    if (File.Exists(destinationPath))
                    {
                        File.Delete(destinationPath);
                    }
                }
                catch (Exception deleteEx)
                {
                    LogHelper.WriteLogToFile($"AutoUpdate | Error deleting incomplete download: {deleteEx.Message}", LogHelper.LogType.Error);
                }
                return false;
            }
        }

        /// <summary>
        /// 下载文件到目标路径，失败时抛出异常交由上层处理。
        /// </summary>
        /// <param name="fileUrl">下载地址。</param>
        /// <param name="destinationPath">本地保存路径。</param>
        private static async Task DownloadFile(string fileUrl, string destinationPath)
        {
            string directory = Path.GetDirectoryName(destinationPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                LogHelper.WriteLogToFile($"AutoUpdate | Created directory: {directory}");
            }

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    client.Timeout = TimeSpan.FromMinutes(5);
                    HttpResponseMessage response = await client.GetAsync(fileUrl);
                    response.EnsureSuccessStatusCode();

                    using (FileStream fileStream = File.Create(destinationPath))
                    {
                        await response.Content.CopyToAsync(fileStream);
                    }
                    LogHelper.WriteLogToFile($"AutoUpdate | File downloaded successfully to {destinationPath}");
                }
                catch (HttpRequestException ex)
                {
                    LogHelper.WriteLogToFile($"AutoUpdate | HTTP request error downloading from {fileUrl}: {ex.Message}", LogHelper.LogType.Error);
                    throw;
                }
                catch (TaskCanceledException ex)
                {
                    LogHelper.WriteLogToFile($"AutoUpdate | Timeout downloading from {fileUrl}: {ex.Message}", LogHelper.LogType.Error);
                    throw;
                }
                catch (IOException ex)
                {
                    LogHelper.WriteLogToFile($"AutoUpdate | IO error saving to {destinationPath}: {ex.Message}", LogHelper.LogType.Error);
                    throw;
                }
                catch (Exception ex)
                {
                    LogHelper.WriteLogToFile($"AutoUpdate | Generic error downloading from {fileUrl}: {ex.Message}", LogHelper.LogType.Error);
                    throw;
                }
            }
        }

        /// <summary>
        /// 将安装包下载状态写入状态文件。
        /// </summary>
        /// <param name="isSuccess">是否下载成功。</param>
        private static void SaveDownloadStatus(bool isSuccess)
        {
            try
            {
                if (statusFilePath == null)
                {
                    LogHelper.WriteLogToFile("AutoUpdate | statusFilePath is null, cannot save download status.", LogHelper.LogType.Error);
                    return;
                }

                string directory = Path.GetDirectoryName(statusFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(statusFilePath, isSuccess.ToString());
                LogHelper.WriteLogToFile($"AutoUpdate | Saved download status ({isSuccess}) to {statusFilePath}");
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"AutoUpdate | Error saving download status: {ex.Message}", LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// 启动指定版本安装程序并退出当前应用。
        /// </summary>
        /// <param name="version">待安装版本号。</param>
        /// <param name="isInSilence">是否使用更静默的安装参数。</param>
        public static void InstallNewVersionApp(string version, bool isInSilence)
        {
            try
            {
                string setupFileName = $"Ink.Canvas.Artistry.V{version}.Setup.exe";
                string setupFilePath = Path.Combine(updatesFolderPath, setupFileName);

                if (!File.Exists(setupFilePath))
                {
                    LogHelper.WriteLogToFile($"AutoUpdate | Setup file not found: {setupFilePath}", LogHelper.LogType.Error);
                    return;
                }

                string InstallCommand = $"\"{setupFilePath}\" /SILENT";
                if (isInSilence) InstallCommand += " /VERYSILENT";

                LogHelper.WriteLogToFile($"AutoUpdate | Executing install command: {InstallCommand}");
                ExecuteCommandLine(InstallCommand);
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"AutoUpdate | Error installing update: {ex.Message}", LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// 通过命令行执行安装命令，并在启动后关闭当前应用。
        /// </summary>
        /// <param name="command">要执行的命令行内容。</param>
        private static void ExecuteCommandLine(string command)
        {
            try
            {
                ProcessStartInfo processStartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {command}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = new Process { StartInfo = processStartInfo })
                {
                    process.Start();
                    LogHelper.WriteLogToFile($"AutoUpdate | Started process for command: {command}");
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        LogHelper.WriteLogToFile($"AutoUpdate | Shutting down application for update.");
                        Application.Current.Shutdown();
                    });
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"AutoUpdate | Error executing command line '{command}': {ex.Message}", LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// 删除自动更新缓存目录及其中全部文件。
        /// </summary>
        public static void DeleteUpdatesFolder()
        {
            try
            {
                if (Directory.Exists(updatesFolderPath))
                {
                    Directory.Delete(updatesFolderPath, true);
                    LogHelper.WriteLogToFile($"AutoUpdate | Deleted updates folder: {updatesFolderPath}");
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"AutoUpdate clearing| Error deleting updates folder: {ex.Message}", LogHelper.LogType.Error);
            }
        }
    }

    internal class AutoUpdateWithSilenceTimeComboBox
    {
        public static ObservableCollection<string> Hours { get; set; } = new ObservableCollection<string>();
        public static ObservableCollection<string> Minutes { get; set; } = new ObservableCollection<string>();

        public static void InitializeAutoUpdateWithSilenceTimeComboBoxOptions(ComboBox startTimeComboBox, ComboBox endTimeComboBox)
        {
            if (Hours.Any() || Minutes.Any()) return;

            for (int hour = 0; hour <= 23; ++hour)
            {
                Hours.Add(hour.ToString("00"));
            }
            for (int minute = 0; minute <= 59; minute += 20)
            {
                Minutes.Add(minute.ToString("00"));
            }
            var timeOptions = Hours.SelectMany(h => Minutes.Select(m => $"{h}:{m}")).ToList();
            startTimeComboBox.ItemsSource = timeOptions;
            endTimeComboBox.ItemsSource = timeOptions;
        }

        public static bool CheckIsInSilencePeriod(string startTime, string endTime)
        {
            if (string.IsNullOrEmpty(startTime) || string.IsNullOrEmpty(endTime)) return false;
            if (startTime == endTime) return true;

            DateTime currentTime = DateTime.Now;
            DateTime StartTime, EndTime;

            if (!DateTime.TryParseExact(startTime, "HH:mm", null, System.Globalization.DateTimeStyles.None, out StartTime) ||
                !DateTime.TryParseExact(endTime, "HH:mm", null, System.Globalization.DateTimeStyles.None, out EndTime))
            {
                LogHelper.WriteLogToFile($"AutoUpdate | Invalid time format for silence period: Start='{startTime}', End='{endTime}'", LogHelper.LogType.Error);
                return false;
            }

            TimeSpan currentTimeOfDay = currentTime.TimeOfDay;
            TimeSpan startTimeOfDay = StartTime.TimeOfDay;
            TimeSpan endTimeOfDay = EndTime.TimeOfDay;

            if (startTimeOfDay <= endTimeOfDay)
            {
                return currentTimeOfDay >= startTimeOfDay && currentTimeOfDay < endTimeOfDay;
            }
            else
            {
                return currentTimeOfDay >= startTimeOfDay || currentTimeOfDay < endTimeOfDay;
            }
        }
    }
}
