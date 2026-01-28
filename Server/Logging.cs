using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

        public static void Write(string errorType, params string[] arrMess)
        {
            var timeNow = DateTime.Now;

            Task.Run(async () =>
            {
                try
                {
                    //Đặt tên file log
                    var sPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log", timeNow.ToString("yyyy"), timeNow.ToString("MM"));
                    var directoryInfo = new DirectoryInfo(sPath);
                    if (!directoryInfo.Exists)
                    {
                        //Neu khong ton tai thu muc ghi log 
                        directoryInfo.Create(); //Tao moi thu muc ghi log
                    }
                    //var filenameLog = AutoRenameFile(sPath, "Log-" + timeNow.ToString("yyyyMMdd")) + ".txt";

                    var fileLog = Path.Combine(sPath, "Log-" + timeNow.ToString("yyyyMMdd") + ".txt");

                    // Bắt đầu ghi log
                    var strInfor = string.Format("[{0}]\t{1:yyyy-MM-dd}\t{2:HH:mm:ss:fff}", errorType.ToUpper(), timeNow, timeNow);
                    //strInfor += "\t";
                    foreach (var str in arrMess)
                    {
                        if (!string.IsNullOrEmpty(str))
                            strInfor += "\t" + str.Replace("\n", "").Replace("\r", "");
                    }

                    strInfor += Environment.NewLine;

                    await semaphore.WaitAsync();

                    using (var fileStream = new FileStream(fileLog, FileMode.Append, FileAccess.Write, FileShare.ReadWrite, 4096))
                    {
                        await fileStream.WriteAsync(Encoding.UTF8.GetBytes(strInfor), 0, Encoding.UTF8.GetByteCount(strInfor));
                    }
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
                    //Đặt tên file log
                    var sPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log", timeNow.ToString("yyyy"), timeNow.ToString("MM"));
                    var directoryInfo = new DirectoryInfo(sPath);
                    if (!directoryInfo.Exists)
                    {
                        //Neu khong ton tai thu muc ghi log 
                        directoryInfo.Create(); //Tao moi thu muc ghi log
                    }
                    //var filenameLog = AutoRenameFile(sPath, "Log-" + timeNow.ToString("yyyyMMdd")) + ".txt";

                    var fileLog = Path.Combine(sPath, "Log-" + timeNow.ToString("yyyyMMdd") + ".txt");

                    // Bắt đầu ghi log
                    var strInfor = string.Format("[{0}]\t{1:yyyy-MM-dd}\t{2:HH:mm:ss:fff}", errorType.ToUpper(), timeNow, timeNow);
                    //strInfor += "\t";

                    // Get stack trace for the exception with source file information
                    var st = new StackTrace(ex, true);

                    var frame = st.GetFrame(st.FrameCount - 1);
                    var methods = frame.GetMethod().DeclaringType.FullName;
                    var line = frame.GetFileLineNumber();

                    strInfor += "\t" + ex.Message + "\t" + methods + ">\t" + line;
                    if (ex.InnerException != null)
                    {
                        strInfor += "\t" + ex.InnerException.Message;
                    }

                    strInfor += Environment.NewLine;

                    await semaphore.WaitAsync();

                    using (var fileStream = new FileStream(fileLog, FileMode.Append, FileAccess.Write, FileShare.ReadWrite, 4096))
                    {
                        await fileStream.WriteAsync(Encoding.UTF8.GetBytes(strInfor), 0, Encoding.UTF8.GetByteCount(strInfor));
                    }
                }
                catch { }
                finally
                {
                    semaphore.Release();
                }
            });
        }

        private static string AutoRenameFile(string folderPath, string fileName)
        {
            try
            {
                var allFiles =
                    Directory.GetFiles(folderPath)
                        .Select(Path.GetFileNameWithoutExtension)
                        .ToArray();

                if (allFiles.Length == 0)
                {
                    return fileName;
                }
                FileInfo fileInfo = null;
                if (allFiles.Length == 1)
                    fileInfo = new FileInfo(folderPath + "\\" + fileName + ".html");
                else
                    fileInfo =
                        new FileInfo(folderPath + "\\" + String.Format("{0} ({1})", fileName, allFiles.Length - 1) +
                                     ".txt");
                if (fileInfo.Length >= 1048576)
                {
                    fileName = String.Format("{0} ({1})", fileName, allFiles.Length);
                }
                else if (allFiles.Length != 1)
                    fileName = String.Format("{0} ({1})", fileName, allFiles.Length - 1);
                return fileName;
            }
            catch (Exception)
            {
                return fileName;
            }
        }

        #endregion
    }
}
