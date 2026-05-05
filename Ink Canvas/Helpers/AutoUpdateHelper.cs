using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;
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
        private const string UpdateServerBaseUrl = "https://github.com/Tayasui-rainnya/Ink-Canvas-Artistry/releases";

        /// <summary>
        /// 临时更新信息接口：通过 GitHub Releases API 自动获取最新的非预发布版本。
        /// </summary>
        private const string GitHubReleasesApiUrl = "https://api.github.com/repos/Tayasui-rainnya/Ink-Canvas-Artistry/releases?per_page=10";

        /// <summary>
        /// 检查服务器版本是否高于当前本地版本。
        /// </summary>
        /// <returns>若有更新返回远端版本号（原始 tag 格式）；否则返回 <c>null</c>。</returns>
        public static async Task<string> CheckForUpdates()
        {
            try
            {
                string localVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
                var remoteVersionInfo = await GetRemoteVersion(GitHubReleasesApiUrl);

                if (remoteVersionInfo.HasValue)
                {
                    Version local = new Version(localVersion);
                    if (remoteVersionInfo.Value.ParsedVersion > local)
                    {
                        LogHelper.WriteLogToFile("AutoUpdate | New version Available: " + remoteVersionInfo.Value.RawTag);
                        return remoteVersionInfo.Value.RawTag;
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
        /// 版本信息结构，包含原始 tag 和解析后的 Version 对象。
        /// </summary>
        public struct VersionInfo
        {
            public string RawTag { get; set; }
            public Version ParsedVersion { get; set; }
        }

        /// <summary>
        /// （临时方案）从 GitHub Releases API 获取最新的非预发布版本号。
        /// </summary>
        /// <param name="fileUrl">GitHub Releases API URL。</param>
        /// <returns>包含原始 tag 和解析后的 Version 的结构体；失败时返回 <c>null</c>。</returns>
        public static async Task<VersionInfo?> GetRemoteVersion(string fileUrl)
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    client.Timeout = TimeSpan.FromSeconds(15);
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("InkCanvasArtistry-AutoUpdater/1.0");
                    HttpResponseMessage response = await client.GetAsync(fileUrl);
                    response.EnsureSuccessStatusCode();

                    string releasesJson = await response.Content.ReadAsStringAsync();
                    JArray releases = JArray.Parse(releasesJson);
                    foreach (JToken release in releases)
                    {
                        bool isPrerelease = (bool?)release["prerelease"] == true;
                        bool isDraft = (bool?)release["draft"] == true;
                        if (isPrerelease || isDraft)
                        {
                            continue;
                        }

                        string rawTag = (string)release["tag_name"];
                        rawTag = rawTag?.Trim();
                        if (!string.IsNullOrWhiteSpace(rawTag))
                        {
                            string versionString = rawTag.TrimStart('v', 'V');
                            try
                            {
                                Version parsedVersion = new Version(versionString);
                                return new VersionInfo
                                {
                                    RawTag = rawTag,
                                    ParsedVersion = parsedVersion
                                };
                            }
                            catch (Exception ex)
                            {
                                LogHelper.WriteLogToFile($"AutoUpdate | Failed to parse version from tag '{rawTag}': {ex.Message}", LogHelper.LogType.Error);
                                continue;
                            }
                        }
                    }
                }
                catch (HttpRequestException ex)
                {
                    LogHelper.WriteLogToFile($"AutoUpdate | HTTP request error getting version from {fileUrl}: {ex.Message}", LogHelper.LogType.Error);
                }
                catch (TaskCanceledException ex)
                {
                    LogHelper.WriteLogToFile($"AutoUpdate | Timeout getting version from {fileUrl}: {ex.Message}", LogHelper.LogType.Error);
                }
                catch (Newtonsoft.Json.JsonException ex)
                {
                    LogHelper.WriteLogToFile($"AutoUpdate | JSON parse error getting version from {fileUrl}: {ex.Message}", LogHelper.LogType.Error);
                }
                catch (Exception ex)
                {
                    LogHelper.WriteLogToFile($"AutoUpdate | Error getting remote version from {fileUrl}: {ex.Message}", LogHelper.LogType.Error);
                }
                return null;
            }
        }

        /// <summary>
        /// 从 URL 获取纯文本内容（用于获取 SHA256 文件等）。
        /// </summary>
        /// <param name="fileUrl">文件的 URL。</param>
        /// <returns>文件的纯文本内容；失败时返回 <c>null</c>。</returns>
        private static async Task<string> GetPlainTextFromUrl(string fileUrl)
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    client.Timeout = TimeSpan.FromSeconds(15);
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("InkCanvasArtistry-AutoUpdater/1.0");
                    HttpResponseMessage response = await client.GetAsync(fileUrl);
                    response.EnsureSuccessStatusCode();

                    string content = await response.Content.ReadAsStringAsync();
                    return content;
                }
                catch (HttpRequestException ex)
                {
                    LogHelper.WriteLogToFile($"AutoUpdate | HTTP request error getting plain text from {fileUrl}: {ex.Message}", LogHelper.LogType.Error);
                }
                catch (TaskCanceledException ex)
                {
                    LogHelper.WriteLogToFile($"AutoUpdate | Timeout getting plain text from {fileUrl}: {ex.Message}", LogHelper.LogType.Error);
                }
                catch (Exception ex)
                {
                    LogHelper.WriteLogToFile($"AutoUpdate | Error getting plain text from {fileUrl}: {ex.Message}", LogHelper.LogType.Error);
                }
                return null;
            }
        }

        /// <summary>
        /// 本地更新缓存目录路径。
        /// </summary>
        private static string updatesFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Ink Canvas Artistry", "AutoUpdate");

        /// <summary>
        /// 下载状态写入锁，避免并发写同一状态文件时出现竞争。
        /// </summary>
        private static readonly object downloadStatusWriteLock = new object();

        /// <summary>
        /// 下载指定版本安装包，并写入下载状态标记文件。
        /// </summary>
        /// <param name="version">目标版本号（原始 tag 格式，如 "v1.2.3" 或 "1.2.3"）。</param>
        /// <returns>下载成功返回 <c>true</c>，否则返回 <c>false</c>。</returns>
        public static async Task<bool> DownloadSetupFileAndSaveStatus(string version)
        {
            try
            {
                string versionWithoutPrefix = version.TrimStart('v', 'V');
                string statusFilePath = Path.Combine(updatesFolderPath, $"DownloadV{versionWithoutPrefix}Status.txt");
                string setupFileName = $"Ink.Canvas.Artistry.V{versionWithoutPrefix}.Setup.exe";
                string destinationPath = Path.Combine(updatesFolderPath, setupFileName);

                if (File.Exists(statusFilePath)
                    && File.Exists(destinationPath)
                    && string.Equals(File.ReadAllText(statusFilePath).Trim(), "true", StringComparison.OrdinalIgnoreCase))
                {
                    LogHelper.WriteLogToFile("AutoUpdate | Setup file already downloaded.");
                    return true;
                }

                string prefixVersion = version.StartsWith("v", StringComparison.OrdinalIgnoreCase) ? version : "v" + versionWithoutPrefix;
                string noPrefixVersion = versionWithoutPrefix;
                string[] downloadUrls = new[]
                {
                    $"{UpdateServerBaseUrl}/download/{prefixVersion}/{setupFileName}",
                    $"{UpdateServerBaseUrl}/download/{noPrefixVersion}/{setupFileName}"
                }.Distinct().ToArray();

                SaveDownloadStatus(statusFilePath, false);
                bool downloadSucceeded = false;
                foreach (string downloadUrl in downloadUrls)
                {
                    try
                    {
                        LogHelper.WriteLogToFile($"AutoUpdate | Attempting download from: {downloadUrl} to {destinationPath}");
                        await DownloadFile(downloadUrl, destinationPath);
                        downloadSucceeded = true;
                        break;
                    }
                    catch (Exception ex)
                    {
                        LogHelper.WriteLogToFile($"AutoUpdate | Download attempt failed from {downloadUrl}: {ex.Message}", LogHelper.LogType.Error);
                    }
                }

                if (!downloadSucceeded)
                {
                    throw new InvalidOperationException($"AutoUpdate | Could not download setup file for version {version} from GitHub Releases.");
                }
                SaveDownloadStatus(statusFilePath, true);

                LogHelper.WriteLogToFile("AutoUpdate | Setup file successfully downloaded.");
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"AutoUpdate | Error downloading setup file for version {version}: {ex.Message}", LogHelper.LogType.Error);
                string versionWithoutPrefix = version.TrimStart('v', 'V');
                string statusFilePath = Path.Combine(updatesFolderPath, $"DownloadV{versionWithoutPrefix}Status.txt");
                SaveDownloadStatus(statusFilePath, false);
                try
                {
                    string setupFileName = $"Ink.Canvas.Artistry.V{versionWithoutPrefix}.Setup.exe";
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
        /// <param name="statusFilePath">状态文件路径。</param>
        /// <param name="isSuccess">是否下载成功。</param>
        private static void SaveDownloadStatus(string statusFilePath, bool isSuccess)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(statusFilePath))
                {
                    LogHelper.WriteLogToFile("AutoUpdate | statusFilePath is null, cannot save download status.", LogHelper.LogType.Error);
                    return;
                }

                string directory = Path.GetDirectoryName(statusFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                lock (downloadStatusWriteLock)
                {
                    File.WriteAllText(statusFilePath, isSuccess.ToString());
                }
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
        /// <param name="version">待安装版本号（原始 tag 格式，如 "v1.2.3" 或 "1.2.3"）。</param>
        /// <param name="isInSilence">是否使用更静默的安装参数。</param>
        public static void InstallNewVersionApp(string version, bool isInSilence)
        {
            try
            {
                string versionWithoutPrefix = version.TrimStart('v', 'V');
                string setupFileName = $"Ink.Canvas.Artistry.V{versionWithoutPrefix}.Setup.exe";
                string setupFilePath = Path.Combine(updatesFolderPath, setupFileName);

                if (!File.Exists(setupFilePath))
                {
                    LogHelper.WriteLogToFile($"AutoUpdate | Setup file not found: {setupFilePath}", LogHelper.LogType.Error);
                    return;
                }
                if (!VerifyInstallerIntegrity(version, setupFilePath).GetAwaiter().GetResult())
                {
                    LogHelper.WriteLogToFile($"AutoUpdate | Setup integrity verification failed for version {version}.", LogHelper.LogType.Error);
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
        /// 校验安装包完整性：对比本地文件 SHA-256 与服务端发布值。
        /// </summary>
        /// <param name="version">待安装版本号（原始 tag 格式，如 "v1.2.3" 或 "1.2.3"）。</param>
        /// <param name="setupFilePath">本地安装包路径。</param>
        /// <returns>校验通过返回 <c>true</c>，否则返回 <c>false</c>。</returns>
        private static async Task<bool> VerifyInstallerIntegrity(string version, string setupFilePath)
        {
            try
            {
                string versionWithoutPrefix = version.TrimStart('v', 'V');
                string setupFileName = $"Ink.Canvas.Artistry.V{versionWithoutPrefix}.Setup.exe";

                string prefixVersion = version.StartsWith("v", StringComparison.OrdinalIgnoreCase) ? version : "v" + versionWithoutPrefix;
                string noPrefixVersion = versionWithoutPrefix;
                string[] shaFileUrls = new[]
                {
                    $"{UpdateServerBaseUrl}/download/{prefixVersion}/{setupFileName}.sha256",
                    $"{UpdateServerBaseUrl}/download/{noPrefixVersion}/{setupFileName}.sha256"
                }.Distinct().ToArray();

                string expectedHash = null;
                foreach (string shaFileUrl in shaFileUrls)
                {
                    expectedHash = await GetPlainTextFromUrl(shaFileUrl);
                    if (!string.IsNullOrWhiteSpace(expectedHash))
                    {
                        LogHelper.WriteLogToFile($"AutoUpdate | Retrieved hash from: {shaFileUrl}");
                        break;
                    }
                }

                if (string.IsNullOrWhiteSpace(expectedHash))
                {
                    LogHelper.WriteLogToFile($"AutoUpdate | Missing remote hash file for version {version}", LogHelper.LogType.Error);
                    return false;
                }

                string normalizedExpectedHash = NormalizeSha256Value(expectedHash);
                string localHash = ComputeSha256(setupFilePath);
                bool isMatch = string.Equals(localHash, normalizedExpectedHash, StringComparison.OrdinalIgnoreCase);

                if (!isMatch)
                {
                    LogHelper.WriteLogToFile($"AutoUpdate | SHA256 mismatch. Local: {localHash}, Expected: {normalizedExpectedHash}", LogHelper.LogType.Error);
                }

                return isMatch;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"AutoUpdate | Error verifying installer integrity: {ex}", LogHelper.LogType.Error);
                return false;
            }
        }

        /// <summary>
        /// 将 SHA-256 文本规范化为仅包含 64 位十六进制摘要。
        /// </summary>
        /// <param name="shaText">服务端返回的 SHA 文本。</param>
        /// <returns>规范化后的 SHA-256 十六进制字符串。</returns>
        private static string NormalizeSha256Value(string shaText)
        {
            if (string.IsNullOrWhiteSpace(shaText)) return string.Empty;
            string firstToken = shaText.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;
            return firstToken.Trim().ToLowerInvariant();
        }

        /// <summary>
        /// 计算指定文件的 SHA-256 值。
        /// </summary>
        /// <param name="filePath">目标文件路径。</param>
        /// <returns>64 位十六进制摘要。</returns>
        private static string ComputeSha256(string filePath)
        {
            using (FileStream stream = File.OpenRead(filePath))
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(stream);
                StringBuilder sb = new StringBuilder(hashBytes.Length * 2);
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("x2"));
                }
                return sb.ToString();
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

    /// <summary>
    /// 自动更新静默时段的时间选项与判定辅助类。
    /// </summary>
    internal class AutoUpdateWithSilenceTimeComboBox
    {
        /// <summary>
        /// 小时候选列表（00-23）。
        /// </summary>
        public static ObservableCollection<string> Hours { get; set; } = new ObservableCollection<string>();

        /// <summary>
        /// 分钟候选列表（按 20 分钟步进）。
        /// </summary>
        public static ObservableCollection<string> Minutes { get; set; } = new ObservableCollection<string>();

        /// <summary>
        /// 初始化静默时段下拉框选项（仅首次初始化）。
        /// </summary>
        /// <param name="startTimeComboBox">静默开始时间下拉框。</param>
        /// <param name="endTimeComboBox">静默结束时间下拉框。</param>
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

        /// <summary>
        /// 判断当前时间是否处于静默时段。
        /// </summary>
        /// <param name="startTime">静默开始时间，格式 HH:mm。</param>
        /// <param name="endTime">静默结束时间，格式 HH:mm。</param>
        /// <returns>在静默时段内返回 <c>true</c>，否则返回 <c>false</c>。</returns>
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
