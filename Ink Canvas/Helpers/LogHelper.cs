using System;
using System.IO;
using System.Reflection;

namespace Ink_Canvas.Helpers
{
    /// <summary>
    /// 应用日志辅助类，提供文本日志与对象属性日志写入能力。
    /// </summary>
    class LogHelper
    {
        /// <summary>
        /// 日志文件名（位于 <see cref="App.RootPath"/> 下）。
        /// </summary>
        public static string LogFile = "Log.txt";

        /// <summary>
        /// 以 Info 级别写入一条日志。
        /// </summary>
        /// <param name="str">日志内容。</param>
        public static void NewLog(string str)
        {
            WriteLogToFile(str, LogType.Info);
        }

        /// <summary>
        /// 异常日志入口（当前保留空实现）。
        /// </summary>
        /// <param name="ex">异常对象。</param>
        public static void NewLog(Exception ex)
        {

        }

        /// <summary>
        /// 将字符串日志写入日志文件。
        /// </summary>
        /// <param name="str">日志内容。</param>
        /// <param name="logType">日志级别。</param>
        public static void WriteLogToFile(string str, LogType logType = LogType.Info)
        {
            string strLogType = "Info";
            switch (logType)
            {
                case LogType.Event:
                    strLogType = "Event";
                    break;
                case LogType.Trace:
                    strLogType = "Trace";
                    break;
                case LogType.Error:
                    strLogType = "Error";
                    break;
            }
            try
            {
                var file = App.RootPath + LogFile;
                if (!Directory.Exists(App.RootPath))
                {
                    Directory.CreateDirectory(App.RootPath);
                }
                StreamWriter sw = new StreamWriter(file, true);
                sw.WriteLine(string.Format("{0} [{1}] {2}", DateTime.Now.ToString("O"), strLogType, str));
                sw.Close();
            }
            catch { }
        }

        /// <summary>
        /// 将对象公开属性逐项写入日志文件，便于调试状态。
        /// </summary>
        /// <param name="obj">待记录对象。</param>
        /// <param name="logType">日志级别。</param>
        public static void WriteObjectLogToFile(object obj, LogType logType = LogType.Info)
        {
            string strLogType = "Info";
            switch (logType)
            {
                case LogType.Event:
                    strLogType = "Event";
                    break;
                case LogType.Trace:
                    strLogType = "Trace";
                    break;
                case LogType.Error:
                    strLogType = "Error";
                    break;
            }
            try
            {
                var file = App.RootPath + LogFile;
                if (!Directory.Exists(App.RootPath))
                {
                    Directory.CreateDirectory(App.RootPath);
                }
                using (StreamWriter sw = new StreamWriter(file, true))
                {
                    sw.WriteLine($"{DateTime.Now:O} [{strLogType}] Object Log:");
                    if (obj != null)
                    {
                        Type type = obj.GetType();
                        PropertyInfo[] properties = type.GetProperties();
                        foreach (PropertyInfo property in properties)
                        {
                            object value = property.GetValue(obj, null);
                            sw.WriteLine($"{property.Name}: {value}");
                        }
                    }
                    else
                    {
                        sw.WriteLine("null");
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// 日志级别枚举。
        /// </summary>
        public enum LogType
        {
            Info,
            Trace,
            Error,
            Event
        }
    }
}
