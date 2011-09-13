using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;
using System.Diagnostics;

namespace PortableTransfer {
    public class TransferLog {
        static readonly AutoResetEvent LogEvent = new AutoResetEvent(true);
        public static readonly string LogDirectoryPath;
        public static readonly string MainLogPath;
        static TransferLog() {
            LogDirectoryPath = Path.Combine(TransferConfigManager.UserDirectoryPath, "Log");
            if (!Directory.Exists(LogDirectoryPath)) Directory.CreateDirectory(LogDirectoryPath);
            MainLogPath = GetLogFilePath("log");
        }
        public static string GetLogFilePath(string ext) {
            return Path.Combine(LogDirectoryPath, "portableTransfer." + ext.Trim('.'));
        }
        public static void LogException(Exception ex) {
            Log(ex.ToString());
        }
        public static void Log(string message) {
            ThreadPool.QueueUserWorkItem(new WaitCallback(delegate(object obj) {
                LogEvent.WaitOne();
                try {
                    int counter = 3;
                    do {
                        try {
                            DateTime now = DateTime.Now;
                            File.AppendAllText(MainLogPath, string.Format("[{0} {1}] {2}\r\n", now.ToLongDateString(), now.ToLongTimeString(), message), Encoding.UTF8);
                            break;
                        } catch (Exception ex) {
                            LogByCurrentProcess(string.Format("I = {0}: {1}", 3 - counter, ex.ToString()));
                        }
                    } while (counter-- > 0);
                } catch (Exception ex) {
                    LogByCurrentProcess(ex.ToString());
                } finally {
                    LogEvent.Set();
                }
            }));
        }

        static void LogByCurrentProcess(string message) {
            DateTime now = DateTime.Now;
            File.AppendAllText(GetLogFilePath(Process.GetCurrentProcess().Id.ToString()), string.Format("[{0} {1}] {2}\r\n", now.ToLongDateString(), now.ToLongTimeString(), message), Encoding.UTF8);
        }
    }
}
