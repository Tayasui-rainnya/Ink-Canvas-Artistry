using System;
using System.IO;
using System.Windows;

namespace Ink_Canvas.Helpers
{
    /// <summary>
    /// 自动保存文件清理工具：按时间阈值删除旧文件并回收空目录。
    /// </summary>
    internal class DelAutoSavedFiles
    {
        /// <summary>
        /// 删除指定目录中超过阈值天数的自动保存文件。
        /// </summary>
        /// <param name="directoryPath">根目录路径。</param>
        /// <param name="daysThreshold">保留天数阈值。</param>
        public static void DeleteFilesOlder(string directoryPath, int daysThreshold)
        {
            string[] extensionsToDel = { ".icstk", ".icart", ".png" };
            if (Directory.Exists(directoryPath))
            {
                // 获取目录中的所有子目录
                string[] subDirectories = Directory.GetDirectories(directoryPath, "*", SearchOption.AllDirectories);
                foreach (string subDirectory in subDirectories)
                {
                    try
                    {
                        // 获取子目录下的所有文件
                        string[] files = Directory.GetFiles(subDirectory);
                        foreach (string filePath in files)
                        {
                            // 获取文件的创建日期
                            DateTime creationDate = File.GetCreationTime(filePath);
                            // 获取文件的扩展名
                            string fileExtension = Path.GetExtension(filePath);
                            // 如果文件的创建日期早于指定天数且是要删除的扩展名，则删除文件
                            if (creationDate < DateTime.Now.AddDays(-daysThreshold))
                            {
                                if (Array.Exists(extensionsToDel, ext => ext.Equals(fileExtension, StringComparison.OrdinalIgnoreCase))
                                    || Path.GetFileName(filePath).Equals("Position", StringComparison.OrdinalIgnoreCase))
                                {
                                    File.Delete(filePath);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogHelper.WriteLogToFile("DelAutoSavedFiles | 处理文件时出错: " + ex.ToString(), LogHelper.LogType.Error);
                    }
                }

                try
                { // 递归删除空文件夹
                    DeleteEmptyFolders(directoryPath);
                }
                catch (Exception ex)
                {
                    LogHelper.WriteLogToFile("DelAutoSavedFiles | 处理文件时出错: " + ex.ToString(), LogHelper.LogType.Error);
                }
            }
        }

        /// <summary>
        /// 递归删除空文件夹。
        /// </summary>
        /// <param name="directoryPath">待处理目录。</param>
        private static void DeleteEmptyFolders(string directoryPath)
        {
            foreach (string dir in Directory.GetDirectories(directoryPath))
            {
                DeleteEmptyFolders(dir);
                if (Directory.GetFiles(dir).Length == 0 && Directory.GetDirectories(dir).Length == 0)
                {
                    Directory.Delete(dir, false);
                }
            }
        }
    }
}
