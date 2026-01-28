using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.IO;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Threading;

namespace Server
{
    public partial class Form1 : Form
    {
        private const int MAX_CONNECTION = 10;
        private bool statusOpen = true;
        private TcpListener listener;

        public Form1()
        {
            InitializeComponent();
            IPAddress[] localIP = Dns.GetHostAddresses(Dns.GetHostName());

            foreach (IPAddress address in localIP)
            {
                if (address.AddressFamily == AddressFamily.InterNetwork)
                {
                    txtIP.Text = address.ToString();
                    break;
                }
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                txtPort.Text = "9999";
                UpdateStatus("Ready", Color.Gray);
            }
            catch (Exception ex)
            {
                Logging.Write(Logging.ERROR, "Form1_Load", ex.Message);
                UpdateStatus("Error", Color.Red);
            }
        }

        private async void btnStart_ClickAsync(object sender, EventArgs e)
        {
            try
            {
                if (!int.TryParse(txtPort.Text, out int portNumber) || portNumber <= 0 || portNumber > 65535)
                {
                    MessageBox.Show("Please enter a valid port number (1-65535)", "Invalid Port", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                statusOpen = true;
                UpdateStatus("Starting...", Color.Orange);
                btnStart.Enabled = false;
                btnStop.Enabled = true;

                listener = new TcpListener(IPAddress.Any, portNumber);
                listener.Start();

                UpdateStatus("Running", Color.Green);
                AppendLog("Server started successfully");

                while (statusOpen)
                {
                    var client = await listener.AcceptTcpClientAsync();
                    Thread t = new Thread(() => HandleClient(client))
                    {
                        IsBackground = true
                    };
                    t.Start();
                }

                listener.Stop();
                AppendLog("Server disconnected");
                UpdateStatus("Stopped", Color.Gray);
            }
            catch (Exception ex)
            {
                UpdateStatus("Error", Color.Red);
                AppendLog($"Server error: {ex.Message}");
                Logging.Write(Logging.ERROR, "btnStart_ClickAsync", ex.Message);
            }
            finally
            {
                btnStart.Enabled = true;
                btnStop.Enabled = false;
            }
        }

        private void HandleClient(TcpClient client)
        {
            StreamReader reader = null;
            StreamWriter writer = null;

            try
            {
                reader = new StreamReader(client.GetStream());
                writer = new StreamWriter(client.GetStream()) { AutoFlush = true };

                while (client.Connected)
                {
                    string received = reader.ReadLine();
                    
                    if (string.IsNullOrEmpty(received) || received.Equals("EXIT", StringComparison.OrdinalIgnoreCase))
                    {
                        break;
                    }

                    AppendLog($"Received: {received}");
                    UpdateStatus("Connected", Color.Green);

                    var parameters = received.Split('|').ToList();
                    if (parameters.Count >= 3)
                    {
                        int machineNumber = ParseInt(parameters[0]);
                        string ip = ToString(parameters[1]);
                        int port = ParseInt(parameters[2]);

                        List<GLogData> logData = GetAttendanceData(machineNumber, ip, port);
                        string jsonData = JsonConvert.SerializeObject(logData);
                        
                        writer.WriteLine(jsonData);
                        writer.WriteLine("EXIT");

                        AppendLog($"Sent {logData.Count} records");
                    }
                    else
                    {
                        AppendLog("Invalid request format");
                        writer.WriteLine("ERROR: Invalid format");
                        writer.WriteLine("EXIT");
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Write(Logging.ERROR, "HandleClient", ex.Message);
                AppendLog($"Client error: {ex.Message}");
            }
            finally
            {
                reader?.Close();
                writer?.Close();
                client?.Close();
            }
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            try
            {
                statusOpen = false;
                listener?.Stop();
                UpdateStatus("Stopped", Color.Gray);
                AppendLog("Server stopped");
                btnStart.Enabled = true;
                btnStop.Enabled = false;
            }
            catch (Exception ex)
            {
                UpdateStatus("Error", Color.Red);
                AppendLog($"Stop error: {ex.Message}");
                Logging.Write(Logging.ERROR, "btnStop_Click", ex.Message);
            }
        }

        private void UpdateStatus(string status, Color color)
        {
            if (lblStatus.InvokeRequired)
            {
                lblStatus.Invoke(new Action(() =>
                {
                    lblStatus.Text = status;
                    lblStatus.ForeColor = color;
                }));
            }
            else
            {
                lblStatus.Text = status;
                lblStatus.ForeColor = color;
            }
        }

        private void AppendLog(string message)
        {
            if (txtLogs.InvokeRequired)
            {
                txtLogs.Invoke(new Action(() =>
                {
                    txtLogs.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
                }));
            }
            else
            {
                txtLogs.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
            }
        }

        private List<GLogData> GetAttendanceData(int machineNumber, string ip, int port)
        {
            List<GLogData> logDataList = new List<GLogData>();

            try
            {
                if (!SFC3KPC1.ConnectTcpip(machineNumber, ip, port, 0))
                {
                    Logging.Write(Logging.ERROR, "GetAttendanceData", "Failed to connect to device");
                    return logDataList;
                }

                bool success = SFC3KPC1.StartReadGeneralLogData(machineNumber);
                Logging.Write(Logging.WATCH, "GetAttendanceData", $"Start reading: {GetErrorString()}");

                success = SFC3KPC1.ReadGeneralLogData(machineNumber);
                Logging.Write(Logging.WATCH, "GetAttendanceData", $"Read result: {GetErrorString()}");

                if (success)
                {
                    int recordNumber = 1;
                    while (true)
                    {
                        GLogData data = new GLogData();
                        success = SFC3KPC1.GetGeneralLogData(machineNumber,
                            ref data.vEnrollNumber, ref data.vGranted, ref data.vMethod,
                            ref data.vDoorMode, ref data.vFunNumber, ref data.vSensor,
                            ref data.vYear, ref data.vMonth, ref data.vDay,
                            ref data.vHour, ref data.vMinute, ref data.vSecond);

                        if (!success) break;

                        // Filter invalid records
                        if (data.EnrollNumber <= 0 || data.vGranted != 1 || data.vYear < 2024)
                        {
                            continue;
                        }

                        data.no = recordNumber++;
                        logDataList.Add(data);
                    }

                    Logging.Write(Logging.WATCH, "GetAttendanceData", $"Successfully read {logDataList.Count} records");
                }

                SFC3KPC1.Disconnect(machineNumber);
            }
            catch (Exception ex)
            {
                Logging.Write(Logging.ERROR, "GetAttendanceData", ex.Message);
            }

            return logDataList;
        }

        public static DateTime ParseDateTimes(object obj)
        {
            if (obj is DateTime dateTime) 
                return dateTime;

            try
            {
                if (obj == null || obj.ToString() == string.Empty)
                    return DateTime.Now;

                if (DateTime.TryParse(obj.ToString(), out DateTime result))
                {
                    // Filter out invalid years
                    if (result.Year < 1900 || result.Year > DateTime.Now.Year + 1)
                        return DateTime.Now;
                    
                    return result;
                }

                return DateTime.Now;
            }
            catch
            {
                return DateTime.Now;
            }
        }

        public static string ToString(object obj)
        {
            return obj?.ToString()?.Trim() ?? string.Empty;
        }

        public static int ParseInt(object obj)
        {
            if (obj is int intValue) 
                return intValue;

            return int.TryParse(obj?.ToString(), out int result) ? result : 0;
        }

        public string GetErrorString()
        {
            int vErrorCode = 0;
            SFC3KPC1.GetLastError(ref vErrorCode);

            switch (vErrorCode)
            {
                case 0:
                    return "No Error";
                case 1:
                    return "Can 't open com port";
                case 2:
                    return "Can 't set com port";
                case 3:
                    return "Error in creating socket";
                case 4:
                    return "Error in setting socket option";
                case 5:
                    return "Error in connecting";
                case 6:
                    return "Error in reconnecting";
                case 7:
                    return "The Password (or the machine ID) is incorrect";
                case 8:
                    return "Error in allocating memory in socket dll";
                case 101:
                    return "Can 't send data to device";
                case 102:
                    return "Can 't read data from device";
                case 103:
                    return "Error in parameter";
                case 104:
                    return "Invalid Data";
                case 105:
                    return "The scope of data is incorrect";
                case 501:
                    return "Can't operate the device properly (or none data)";
                case 502:
                    return "All data have been read";
                case 503:
                    return "Double access to FP(or CARD) data";
                case 504:
                    return "Error in allocating memory in device";
                case 505: //SFC3KPCERR_ENROLL_IMAGE_INVALID_ID:
                    return "Invalid ID";
                case 506:    //SFC3KPCERR_ENROLL_IMAGE_OVER_PER_TERMINAL
                    return "All fingerprints are enrolled at the terminal";
                case 507:    //SFC3KPCERR_ENROLL_IMAGE_ALREADY_ENROLLED
                    return "Fingerprint is already enrolled for this position";
                case 508:    //SFC3KPCERR_ENROLL_IMAGE_INVALID_NTH
                    return "Invalid step number";
                case 509:    //SFC3KPCERR_ENROLL_IMAGE_DUPLICATE
                    return "Fingerprint duplicated while enrolling image";
                case 510:    //SFC3KPCERR_ENROLL_IMAGE_NTH_ERROR
                    return "An error occurred while enrolling Nth image";
                case 511:    //SFC3KPCERR_ENROLL_IMAGE_CANT_LOAD
                    return "Could not load Fingerprint image file";
                case 512:    //SFC3KPCERR_ENROLL_IMAGE_ERROR
                    return "An error occurred while enrolling fingerprint image";
                case 513:    //SFC3KPCERR_BGIMAGE_CANT_OPEN
                    return "Cannot open background bitmap";
                case 514:    //SFC3KPCERR_BGIMAGE_INVALID_BITMAP
                    return "Invalid background bitmap";
                case 515:    //SFC3KPCERR_BGIMAGE_DIMENSION
                    return "Bitmap dimension is invalid";
                case 516:    //SFC3KPCERR_BGIMAGE_COLOR_DEPTH
                    return "Bitmap color depth is incorrect. It should be 24bit";
                case 517:    //SFC3KPCERR_BGIMAGE_COMPRESSED
                    return "This bitmap is compressed format. It should be an uncompressed bitmap";
                default:
                    return "Unknown";
            }
        }
    }

    public class GLogData
    {
        public int no = 0;
        public int vEnrollNumber = 0, vGranted = 0, vMethod = 0, vDoorMode = 0;
        public int vFunNumber = 0, vSensor = 0, vYear = 0, vMonth = 0, vDay = 0;
        public int vHour = 0, vMinute = 0, vSecond = 0;
        public string userName;

        public int No
        {
            get { return no; }
        }

        public string Result
        {
            get { return vGranted == 1 ? "Granted" : "Denied"; }
        }

        public string Username
        {
            get { return userName; }
        }

        public string ID
        {
            get { return vEnrollNumber == -1 ? "NONE" : String.Format("{0:D8}", vEnrollNumber); }
        }

        public int EnrollNumber
        {
            get { return vEnrollNumber; }
        }

        public string DoorMode
        {
            get
            {
                switch (vDoorMode)
                {
                    case 0: return "Any";
                    case 1: return "Finger";
                    case 2: return "CD or FP";
                    case 3: return "ID&FP or CD";
                    case 4: return "ID&FP or ID&CD";
                    case 5: return "ID&FP or CD&FP";
                    case 6: return "Open";
                    case 7: return "Close";
                    case 8: return "Card";
                    case 9: return "ID or FP";
                    case 10: return "ID or CD";
                    case 11: return "ID&CD";
                    case 12: return "CD&FP";
                    case 13: return "ID&FP";
                    case 14: return "ID&CD&FP";
                    default: return "Unknown";
                }
            }
        }

        public string Sensor
        {
            get
            {
                return vSensor == 1 ? "Open" : "Close";
            }
        }

        public string Function
        {
            get
            {
                if (vFunNumber == 40)
                    return "NONE";
                else
                    return "F" + (vFunNumber / 10 + 1) + "-" + (vFunNumber % 10);
            }
        }

        public string Method
        {
            get
            {
                string sMethod = "";
                int vmmode = vMethod & (Constants.GLOG_BY_ID | Constants.GLOG_BY_CD | Constants.GLOG_BY_FP);
                switch (vmmode)
                {
                    case 0: sMethod = "by CD2"; break;
                    case 1: sMethod = "by ID"; break;
                    case 2: sMethod = "by CD"; break;
                    case 3: sMethod = "by ID&CD"; break;
                    case 4: sMethod = "by FP"; break;
                    case 5: sMethod = "by ID&FP"; break;
                    case 6: sMethod = "by CD&FP"; break;
                    case 7: sMethod = "by ID&CD&FP"; break;
                }

                if ((vMethod & Constants.GLOG_BY_DURESS_BIT) == Constants.GLOG_BY_DURESS_BIT) sMethod = "[DURESS]";
                if ((vMethod & Constants.GLOG_BY_LIMITTIME) == Constants.GLOG_BY_LIMITTIME) sMethod = sMethod + " [LT]";
                if ((vMethod & Constants.GLOG_BY_ANTIPASS) == Constants.GLOG_BY_ANTIPASS) sMethod = sMethod + " [AP]";
                if ((vMethod & Constants.GLOG_BY_TIMEZONE) == Constants.GLOG_BY_TIMEZONE) sMethod = sMethod + " [TZ]";
                if ((vMethod & Constants.GLOG_BY_AREA) == Constants.GLOG_BY_AREA) sMethod = sMethod + " [FACE]";

                return sMethod;
            }
        }

        public string Time
        {
            get { return String.Format("{0:D4}-{1:D2}-{2:D2} {3:D2}:{4:D2}:{5:D2}", vYear, vMonth, vDay, vHour, vMinute, vSecond & 0xFF); }
        }

        public bool CapturedPhoto
        {
            get { return ((vSecond >> 8) & 0xFF) == 1; }
        }
    }
}
