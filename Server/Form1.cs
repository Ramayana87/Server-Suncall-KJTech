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

                // Add menu to launch test client (only if not already added)
                if (this.MainMenuStrip == null)
                {
                    var menuStrip = new MenuStrip();
                    var toolsMenu = new ToolStripMenuItem("Tools");
                    var testClientMenuItem = new ToolStripMenuItem("Launch Test Client", null, LaunchTestClient);
                    toolsMenu.DropDownItems.Add(testClientMenuItem);
                    menuStrip.Items.Add(toolsMenu);
                    this.MainMenuStrip = menuStrip;
                    this.Controls.Add(menuStrip);
                }
            }
            catch (Exception ex)
            {
                Logging.Write(Logging.ERROR, "Form1_Load", ex.Message);
                UpdateStatus("Error", Color.Red);
            }
        }

        private void LaunchTestClient(object sender, EventArgs e)
        {
            try
            {
                var testForm = new TestClientForm();
                testForm.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error launching test client: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                Logging.Write(Logging.ERROR, "LaunchTestClient", ex.Message);
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
            }
            catch (ObjectDisposedException)
            {
                // Expected when listener is stopped intentionally
                AppendLog("Server stopped");
            }
            catch (Exception ex)
            {
                UpdateStatus("Error", Color.Red);
                AppendLog($"Server error: {ex.Message}");
                Logging.Write(Logging.ERROR, "btnStart_ClickAsync", ex.Message);
            }
            finally
            {
                if (listener != null)
                {
                    listener.Stop();
                    listener = null;
                }

                // Only reset buttons if we're actually stopping
                if (!statusOpen)
                {
                    UpdateStatus("Stopped", Color.Gray);
                    btnStart.Enabled = true;
                    btnStop.Enabled = false;
                }
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

                    // Check if first parameter is an operation type (GETLOGS, GETUSERS, MOCKUP_GETLOGS, MOCKUP_GETUSERS)
                    string operation = "GETLOGS"; // default operation for backward compatibility
                    int paramOffset = 0;

                    if (parameters.Count > 0)
                    {
                        string firstParam = SafeToString(parameters[0]).ToUpper();
                        if (firstParam == "GETLOGS" || firstParam == "GETUSERS" || 
                            firstParam == "MOCKUP_GETLOGS" || firstParam == "MOCKUP_GETUSERS")
                        {
                            operation = firstParam;
                            paramOffset = 1;
                        }
                    }

                    if (parameters.Count >= 3 + paramOffset)
                    {
                        int machineNumber = ParseInt(parameters[0 + paramOffset]);
                        string ip = SafeToString(parameters[1 + paramOffset]);
                        int port = ParseInt(parameters[2 + paramOffset]);

                        var stopwatch = Stopwatch.StartNew();

                        if (operation == "GETUSERS")
                        {
                            // Get distinct users from biometric device
                            List<GLogData> users = GetDistinctUsers(machineNumber, ip, port);
                            stopwatch.Stop();

                            string jsonData = JsonConvert.SerializeObject(users);
                            writer.WriteLine(jsonData);
                            writer.WriteLine("EXIT");

                            AppendLog($"[GETUSERS] Sent {users.Count} users in {stopwatch.ElapsedMilliseconds}ms");
                        }
                        else if (operation == "MOCKUP_GETUSERS")
                        {
                            // Get distinct users from mockup data file
                            List<GLogData> users = GetMockupDistinctUsers(machineNumber);
                            stopwatch.Stop();

                            string jsonData = JsonConvert.SerializeObject(users);
                            writer.WriteLine(jsonData);
                            writer.WriteLine("EXIT");

                            AppendLog($"[MOCKUP_GETUSERS] Sent {users.Count} users in {stopwatch.ElapsedMilliseconds}ms");
                        }
                        else if (operation == "MOCKUP_GETLOGS")
                        {
                            // Optional date filtering parameters
                            DateTime? fromDate = null;
                            DateTime? toDate = null;

                            if (parameters.Count >= 4 + paramOffset && !string.IsNullOrEmpty(SafeToString(parameters[3 + paramOffset])))
                            {
                                if (DateTime.TryParse(parameters[3 + paramOffset], out DateTime parsedFrom))
                                    fromDate = parsedFrom;
                            }

                            if (parameters.Count >= 5 + paramOffset && !string.IsNullOrEmpty(SafeToString(parameters[4 + paramOffset])))
                            {
                                if (DateTime.TryParse(parameters[4 + paramOffset], out DateTime parsedTo))
                                    toDate = parsedTo;
                            }

                            List<GLogData> logData = GetMockupAttendanceData(machineNumber, fromDate, toDate);
                            stopwatch.Stop();

                            string jsonData = JsonConvert.SerializeObject(logData);

                            writer.WriteLine(jsonData);
                            writer.WriteLine("EXIT");

                            AppendLog($"[MOCKUP_GETLOGS] Sent {logData.Count} records in {stopwatch.ElapsedMilliseconds}ms");
                        }
                        else // GETLOGS
                        {
                            // Optional date filtering parameters
                            DateTime? fromDate = null;
                            DateTime? toDate = null;

                            if (parameters.Count >= 4 + paramOffset && !string.IsNullOrEmpty(SafeToString(parameters[3 + paramOffset])))
                            {
                                if (DateTime.TryParse(parameters[3 + paramOffset], out DateTime parsedFrom))
                                    fromDate = parsedFrom;
                            }

                            if (parameters.Count >= 5 + paramOffset && !string.IsNullOrEmpty(SafeToString(parameters[4 + paramOffset])))
                            {
                                if (DateTime.TryParse(parameters[4 + paramOffset], out DateTime parsedTo))
                                    toDate = parsedTo;
                            }

                            List<GLogData> logData = GetAttendanceData(machineNumber, ip, port, fromDate, toDate);
                            stopwatch.Stop();

                            string jsonData = JsonConvert.SerializeObject(logData);

                            writer.WriteLine(jsonData);
                            writer.WriteLine("EXIT");

                            AppendLog($"Sent {logData.Count} records in {stopwatch.ElapsedMilliseconds}ms");

                        }
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

                if (listener != null)
                {
                    listener.Stop();
                    listener = null;
                }

                UpdateStatus("Stopped", Color.Gray);
                AppendLog("Server stopped by user");
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

        private List<GLogData> GetAttendanceData(int machineNumber, string ip, int port, DateTime? fromDate = null, DateTime? toDate = null)
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
                    int totalRecords = 0;
                    int filteredRecords = 0;
                    int invalidRecords = 0;

                    while (true)
                    {
                        GLogData data = new GLogData();
                        success = SFC3KPC1.GetGeneralLogData(machineNumber,
                            ref data.vEnrollNumber, ref data.vGranted, ref data.vMethod,
                            ref data.vDoorMode, ref data.vFunNumber, ref data.vSensor,
                            ref data.vYear, ref data.vMonth, ref data.vDay,
                            ref data.vHour, ref data.vMinute, ref data.vSecond);

                        if (!success) break;

                        totalRecords++;

                        // Filter invalid records (validate year is reasonable)
                        if (data.EnrollNumber <= 0 || data.vGranted != 1 ||
                            data.vYear < 2000 || data.vYear > DateTime.Now.Year + 1)
                        {
                            invalidRecords++;
                            continue;
                        }

                        // Apply date range filter if specified
                        bool passesDateFilter = true;
                        if (fromDate.HasValue || toDate.HasValue)
                        {
                            try
                            {
                                DateTime recordDate = new DateTime(data.vYear, data.vMonth, data.vDay,
                                    data.vHour, data.vMinute, data.vSecond & 0xFF);

                                if (fromDate.HasValue && recordDate < fromDate.Value)
                                {
                                    passesDateFilter = false;
                                }
                                else if (toDate.HasValue && recordDate > toDate.Value)
                                {
                                    passesDateFilter = false;
                                }

                                if (!passesDateFilter)
                                {
                                    filteredRecords++;
                                    continue;
                                }
                            }
                            catch (Exception ex)
                            {
                                // Skip records with invalid dates
                                Logging.Write(Logging.WATCH, "GetAttendanceData", $"Invalid date in record: {ex.Message}");
                                invalidRecords++;
                                continue;
                            }
                        }

                        data.no = recordNumber++;
                        logDataList.Add(data);
                    }

                    string filterInfo = (fromDate.HasValue || toDate.HasValue)
                        ? $" (filtered {filteredRecords}, invalid {invalidRecords} from {totalRecords} total)"
                        : $" (invalid {invalidRecords} from {totalRecords} total)";
                    Logging.Write(Logging.WATCH, "GetAttendanceData",
                        $"Successfully read {logDataList.Count} records{filterInfo}");
                }

                SFC3KPC1.Disconnect(machineNumber);
            }
            catch (Exception ex)
            {
                Logging.Write(Logging.ERROR, "GetAttendanceData", ex.Message);
            }

            return logDataList;
        }

        private List<GLogData> GetDistinctUsers(int machineNumber, string ip, int port)
        {
            List<GLogData> userList = new List<GLogData>();

            try
            {
                if (!SFC3KPC1.ConnectTcpip(machineNumber, ip, port, 0))
                {
                    Logging.Write(Logging.ERROR, "GetDistinctUsers", "Failed to connect to device");
                    return userList;
                }

                try
                {
                    bool success = SFC3KPC1.StartReadGeneralLogData(machineNumber);
                    Logging.Write(Logging.WATCH, "GetDistinctUsers", $"Start reading: {GetErrorString()}");

                    success = SFC3KPC1.ReadGeneralLogData(machineNumber);
                    Logging.Write(Logging.WATCH, "GetDistinctUsers", $"Read result: {GetErrorString()}");

                    if (success)
                    {
                        HashSet<int> uniqueUsers = new HashSet<int>();

                        while (true)
                        {
                            GLogData data = new GLogData();
                            success = SFC3KPC1.GetGeneralLogData(machineNumber,
                                ref data.vEnrollNumber, ref data.vGranted, ref data.vMethod,
                                ref data.vDoorMode, ref data.vFunNumber, ref data.vSensor,
                                ref data.vYear, ref data.vMonth, ref data.vDay,
                                ref data.vHour, ref data.vMinute, ref data.vSecond);

                            if (!success) break;

                            // Only include granted users with fingerprint method
                            if (data.EnrollNumber > 0 && data.vGranted == 1)
                            {
                                // Check if it's fingerprint authentication
                                int vmmode = data.vMethod & (Constants.GLOG_BY_ID | Constants.GLOG_BY_CD | Constants.GLOG_BY_FP);
                                bool isFingerprintAuth = (vmmode & Constants.GLOG_BY_FP) == Constants.GLOG_BY_FP;

                                if (isFingerprintAuth && !uniqueUsers.Contains(data.EnrollNumber))
                                {
                                    uniqueUsers.Add(data.EnrollNumber);
                                    userList.Add(data);
                                }
                            }
                        }

                        Logging.Write(Logging.WATCH, "GetDistinctUsers",
                            $"Successfully read {userList.Count} distinct users");
                    }
                }
                finally
                {
                    SFC3KPC1.Disconnect(machineNumber);
                }
            }
            catch (Exception ex)
            {
                Logging.Write(Logging.ERROR, "GetDistinctUsers", ex.Message);
            }

            return userList;
        }

        private List<GLogData> GetMockupAttendanceData(int machineNumber, DateTime? fromDate = null, DateTime? toDate = null)
        {
            List<GLogData> logDataList = new List<GLogData>();

            try
            {
                // Find mockup data folder
                string currentDir = AppDomain.CurrentDomain.BaseDirectory;
                string projectRoot = Directory.GetParent(currentDir).Parent.Parent.FullName;
                string mockupDataFolder = Path.Combine(projectRoot, "data mockup");

                // If not found, try current directory
                if (!Directory.Exists(mockupDataFolder))
                {
                    mockupDataFolder = Path.Combine(currentDir, "data mockup");
                }

                // If still not found, try parent directory
                if (!Directory.Exists(mockupDataFolder))
                {
                    mockupDataFolder = Path.Combine(Directory.GetParent(currentDir).FullName, "data mockup");
                }

                if (!Directory.Exists(mockupDataFolder))
                {
                    Logging.Write(Logging.ERROR, "GetMockupAttendanceData", $"Mockup data folder not found");
                    return logDataList;
                }

                string fileName = $"may {machineNumber}.txt";
                string filePath = Path.Combine(mockupDataFolder, fileName);

                if (!File.Exists(filePath))
                {
                    Logging.Write(Logging.ERROR, "GetMockupAttendanceData", $"Mockup file not found: {fileName}");
                    return logDataList;
                }

                var lines = File.ReadAllLines(filePath);
                int recordNumber = 1;
                int totalRecords = 0;
                int filteredRecords = 0;
                int invalidRecords = 0;

                foreach (var line in lines)
                {
                    totalRecords++;
                    var parts = line.Split('\t');
                    if (parts.Length >= 9)
                    {
                        try
                        {
                            var data = new GLogData();

                            // Parse line format: [empty], no, result, id, method, doormode, function, verification, time, captured
                            // Note: First element is empty due to leading tab
                            if (int.TryParse(parts[1].Trim(), out int no))
                                data.no = recordNumber;

                            string id = parts[3].Trim();
                            if (int.TryParse(id, out int enrollNum))
                                data.vEnrollNumber = enrollNum;

                            data.vGranted = parts[2].Trim() == "Granted" ? 1 : 0;

                            // Parse date time
                            if (DateTime.TryParse(parts[8].Trim(), out DateTime recordTime))
                            {
                                data.vYear = recordTime.Year;
                                data.vMonth = recordTime.Month;
                                data.vDay = recordTime.Day;
                                data.vHour = recordTime.Hour;
                                data.vMinute = recordTime.Minute;
                                data.vSecond = recordTime.Second & 0xFF;
                            }

                            // Filter invalid records (validate year is reasonable)
                            if (data.EnrollNumber <= 0 || data.vGranted != 1 ||
                                data.vYear < 2000 || data.vYear > DateTime.Now.Year + 1)
                            {
                                invalidRecords++;
                                continue;
                            }

                            // Apply date range filter if specified
                            bool passesDateFilter = true;
                            if (fromDate.HasValue || toDate.HasValue)
                            {
                                try
                                {
                                    DateTime recordDate = new DateTime(data.vYear, data.vMonth, data.vDay,
                                        data.vHour, data.vMinute, data.vSecond & 0xFF);

                                    if (fromDate.HasValue && recordDate < fromDate.Value)
                                    {
                                        passesDateFilter = false;
                                    }
                                    else if (toDate.HasValue && recordDate > toDate.Value)
                                    {
                                        passesDateFilter = false;
                                    }

                                    if (!passesDateFilter)
                                    {
                                        filteredRecords++;
                                        continue;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    // Skip records with invalid dates
                                    Logging.Write(Logging.WATCH, "GetMockupAttendanceData", $"Invalid date in record: {ex.Message}");
                                    invalidRecords++;
                                    continue;
                                }
                            }

                            recordNumber++;
                            logDataList.Add(data);
                        }
                        catch (Exception ex)
                        {
                            invalidRecords++;
                            Logging.Write(Logging.ERROR, "GetMockupAttendanceData", $"Skipped invalid line: {ex.Message}");
                        }
                    }
                }

                string filterInfo = (fromDate.HasValue || toDate.HasValue)
                    ? $" (filtered {filteredRecords}, invalid {invalidRecords} from {totalRecords} total)"
                    : $" (invalid {invalidRecords} from {totalRecords} total)";
                Logging.Write(Logging.WATCH, "GetMockupAttendanceData",
                    $"Successfully read {logDataList.Count} records from {fileName}{filterInfo}");
            }
            catch (Exception ex)
            {
                Logging.Write(Logging.ERROR, "GetMockupAttendanceData", ex.Message);
            }

            return logDataList;
        }

        private List<GLogData> GetMockupDistinctUsers(int machineNumber)
        {
            List<GLogData> userList = new List<GLogData>();

            try
            {
                // Find mockup data folder
                string currentDir = AppDomain.CurrentDomain.BaseDirectory;
                string projectRoot = Directory.GetParent(currentDir).Parent.Parent.FullName;
                string mockupDataFolder = Path.Combine(projectRoot, "data mockup");

                // If not found, try current directory
                if (!Directory.Exists(mockupDataFolder))
                {
                    mockupDataFolder = Path.Combine(currentDir, "data mockup");
                }

                // If still not found, try parent directory
                if (!Directory.Exists(mockupDataFolder))
                {
                    mockupDataFolder = Path.Combine(Directory.GetParent(currentDir).FullName, "data mockup");
                }

                if (!Directory.Exists(mockupDataFolder))
                {
                    Logging.Write(Logging.ERROR, "GetMockupDistinctUsers", $"Mockup data folder not found");
                    return userList;
                }

                string fileName = $"may {machineNumber}.txt";
                string filePath = Path.Combine(mockupDataFolder, fileName);

                if (!File.Exists(filePath))
                {
                    Logging.Write(Logging.ERROR, "GetMockupDistinctUsers", $"Mockup file not found: {fileName}");
                    return userList;
                }

                var lines = File.ReadAllLines(filePath);
                HashSet<int> uniqueUsers = new HashSet<int>();

                foreach (var line in lines)
                {
                    var parts = line.Split('\t');
                    if (parts.Length >= 9)
                    {
                        try
                        {
                            var data = new GLogData();

                            // Parse line format: [empty], no, result, id, method, doormode, function, verification, time, captured
                            string id = parts[3].Trim();
                            if (int.TryParse(id, out int enrollNum))
                                data.vEnrollNumber = enrollNum;

                            data.vGranted = parts[2].Trim() == "Granted" ? 1 : 0;

                            // Parse date time
                            if (DateTime.TryParse(parts[8].Trim(), out DateTime recordTime))
                            {
                                data.vYear = recordTime.Year;
                                data.vMonth = recordTime.Month;
                                data.vDay = recordTime.Day;
                                data.vHour = recordTime.Hour;
                                data.vMinute = recordTime.Minute;
                                data.vSecond = recordTime.Second & 0xFF;
                            }

                            // Only include granted users with valid enroll numbers
                            if (data.EnrollNumber > 0 && data.vGranted == 1)
                            {
                                if (!uniqueUsers.Contains(data.EnrollNumber))
                                {
                                    uniqueUsers.Add(data.EnrollNumber);
                                    userList.Add(data);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Logging.Write(Logging.ERROR, "GetMockupDistinctUsers", $"Skipped invalid line: {ex.Message}");
                        }
                    }
                }

                Logging.Write(Logging.WATCH, "GetMockupDistinctUsers",
                    $"Successfully read {userList.Count} distinct users from {fileName}");
            }
            catch (Exception ex)
            {
                Logging.Write(Logging.ERROR, "GetMockupDistinctUsers", ex.Message);
            }

            return userList;
        }

        public static string SafeToString(object obj)
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
                    return "Can't open com port";
                case 2:
                    return "Can't set com port";
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
                    return "Can't send data to device";
                case 102:
                    return "Can't read data from device";
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
