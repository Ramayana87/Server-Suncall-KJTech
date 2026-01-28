using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    public class Logging
    {
        #region Declare Const

        public const string ERROR = "ERROR";
        public const string WATCH = "WATCH";
        public const string TRACE = "TRACE";
        public const string CHARGE = "CHARGE";

        #endregion

        private static readonly SemaphoreSlim semaphore = new SemaphoreSlim(1);

        #region Write log

        public static void Write(string errorType, params string[] messages)
        {
            var timeNow = DateTime.Now;

            Task.Run(async () =>
            {
                try
                {
                    string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log", 
                        timeNow.ToString("yyyy"), timeNow.ToString("MM"));
                    
                    Directory.CreateDirectory(logPath);
                    
                    string logFile = Path.Combine(logPath, $"Log-{timeNow:yyyyMMdd}.txt");

                    var logMessage = BuildLogMessage(errorType, timeNow, messages);

                    await semaphore.WaitAsync();

                    await File.AppendAllTextAsync(logFile, logMessage, Encoding.UTF8);
                }
                catch { }
                finally
                {
                    semaphore.Release();
                }
            });
        }

        public static void Write(string errorType, Exception ex)
        {
            var timeNow = DateTime.Now;

            Task.Run(async () =>
            {
                try
                {
                    string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log", 
                        timeNow.ToString("yyyy"), timeNow.ToString("MM"));
                    
                    Directory.CreateDirectory(logPath);
                    
                    string logFile = Path.Combine(logPath, $"Log-{timeNow:yyyyMMdd}.txt");

                    var st = new StackTrace(ex, true);
                    var frame = st.GetFrame(st.FrameCount - 1);
                    var method = frame?.GetMethod()?.DeclaringType?.FullName ?? "Unknown";
                    var line = frame?.GetFileLineNumber() ?? 0;

                    string errorMessage = ex.InnerException != null 
                        ? $"{ex.Message}\t{ex.InnerException.Message}"
                        : ex.Message;

                    var logMessage = BuildLogMessage(errorType, timeNow, errorMessage, $"{method}>{line}");

                    await semaphore.WaitAsync();

                    await File.AppendAllTextAsync(logFile, logMessage, Encoding.UTF8);
                }
                catch { }
                finally
                {
                    semaphore.Release();
                }
            });
        }

        private static string BuildLogMessage(string errorType, DateTime timeNow, params string[] messages)
        {
            var sb = new StringBuilder();
            sb.Append($"[{errorType.ToUpper()}]\t{timeNow:yyyy-MM-dd}\t{timeNow:HH:mm:ss:fff}");
            
            foreach (var message in messages)
            {
                if (!string.IsNullOrEmpty(message))
                {
                    sb.Append($"\t{message.Replace("\n", "").Replace("\r", "")}");
                }
            }
            
            sb.AppendLine();
            return sb.ToString();
        }

        #endregion
    }
}
