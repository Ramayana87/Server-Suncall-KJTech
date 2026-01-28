using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using Newtonsoft.Json;
using System.Diagnostics;

namespace Server
{
    public partial class TestClientForm : Form
    {
        private List<GLogData> mockupData = new List<GLogData>();
        private string mockupDataFolder = "";

        public TestClientForm()
        {
            InitializeComponent();
        }

        private void TestClientForm_Load(object sender, EventArgs e)
        {
            // Set default values - get local IP like server does
            IPAddress[] localIP = Dns.GetHostAddresses(Dns.GetHostName());
            foreach (IPAddress address in localIP)
            {
                if (address.AddressFamily == AddressFamily.InterNetwork)
                {
                    txtServerIP.Text = address.ToString();
                    break;
                }
            }
            
            txtServerPort.Text = "9999";
            
            // Set default machine configuration
            txtMachineNumber.Text = "1";
            txtMachineIP.Text = "192.168.1.201";
            txtMachinePort.Text = "4370";
            
            // Find mockup data folder - try multiple locations
            string currentDir = AppDomain.CurrentDomain.BaseDirectory;
            
            // Try relative path from bin folder
            string projectRoot = Directory.GetParent(currentDir).Parent.Parent.FullName;
            mockupDataFolder = Path.Combine(projectRoot, "data mockup");
            
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
            
            if (Directory.Exists(mockupDataFolder))
            {
                lblStatus.Text = "Ready - Mockup data found";
                lblStatus.ForeColor = Color.Green;
            }
            else
            {
                lblStatus.Text = "Warning - Mockup data not found";
                lblStatus.ForeColor = Color.Orange;
            }

            // Set default date range (last 7 days)
            dtpFrom.Value = DateTime.Now.AddDays(-7);
            dtpTo.Value = DateTime.Now;
        }

        private void btnLoadMockup_Click(object sender, EventArgs e)
        {
            try
            {
                Cursor = Cursors.WaitCursor;
                lblStatus.Text = "Loading mockup data...";
                lblStatus.ForeColor = Color.Blue;
                Application.DoEvents();

                mockupData.Clear();
                
                var stopwatch = Stopwatch.StartNew();
                
                if (!Directory.Exists(mockupDataFolder))
                {
                    MessageBox.Show($"Mockup data folder not found: {mockupDataFolder}", 
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var files = Directory.GetFiles(mockupDataFolder, "*.txt");
                int totalLines = 0;
                int validRecords = 0;
                int skippedRecords = 0;

                foreach (var file in files)
                {
                    var lines = File.ReadAllLines(file);
                    totalLines += lines.Length;

                    foreach (var line in lines)
                    {
                        var parts = line.Split('\t');
                        if (parts.Length >= 9)
                        {
                            try
                            {
                                var data = new GLogData();
                                
                                // Parse line format: [empty], no, result, id, method, doormode, function, verification, time, captured
                                // Note: First element is empty due to leading tab
                                if (int.TryParse(parts[1].Trim(), out int no))
                                    data.no = no;
                                
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
                                
                                if (data.vGranted == 1 && data.vEnrollNumber > 0)
                                {
                                    mockupData.Add(data);
                                    validRecords++;
                                }
                            }
                            catch (Exception ex)
                            {
                                skippedRecords++;
                                Logging.Write(Logging.ERROR, "LoadMockup", $"Skipped invalid line: {ex.Message}");
                            }
                        }
                    }
                }
                
                stopwatch.Stop();

                lblStatus.Text = $"Loaded {validRecords:N0} records from {files.Length} files in {stopwatch.ElapsedMilliseconds}ms";
                lblStatus.ForeColor = Color.Green;
                
                txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] Loaded mockup data:{Environment.NewLine}");
                txtLog.AppendText($"  - Total lines: {totalLines:N0}{Environment.NewLine}");
                txtLog.AppendText($"  - Valid records: {validRecords:N0}{Environment.NewLine}");
                txtLog.AppendText($"  - Skipped records: {skippedRecords:N0}{Environment.NewLine}");
                txtLog.AppendText($"  - Time taken: {stopwatch.ElapsedMilliseconds}ms{Environment.NewLine}{Environment.NewLine}");
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Error loading mockup data";
                lblStatus.ForeColor = Color.Red;
                MessageBox.Show($"Error loading mockup data: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void btnTestMockup_Click(object sender, EventArgs e)
        {
            try
            {
                if (mockupData.Count == 0)
                {
                    MessageBox.Show("Please load mockup data first", "Warning", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                Cursor = Cursors.WaitCursor;
                var stopwatch = Stopwatch.StartNew();

                // Filter mockup data based on date range
                DateTime fromDate = dtpFrom.Value.Date;
                DateTime toDate = dtpTo.Value.Date.AddDays(1).AddSeconds(-1);
                
                int invalidDates = 0;

                var filteredData = mockupData.Where(d =>
                {
                    try
                    {
                        DateTime recordDate = new DateTime(d.vYear, d.vMonth, d.vDay, 
                            d.vHour, d.vMinute, d.vSecond & 0xFF);
                        return recordDate >= fromDate && recordDate <= toDate;
                    }
                    catch
                    {
                        invalidDates++;
                        return false;
                    }
                }).ToList();

                stopwatch.Stop();

                txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] Mockup filter test:{Environment.NewLine}");
                txtLog.AppendText($"  - Date range: {fromDate:yyyy-MM-dd} to {toDate:yyyy-MM-dd}{Environment.NewLine}");
                txtLog.AppendText($"  - Total records: {mockupData.Count:N0}{Environment.NewLine}");
                txtLog.AppendText($"  - Filtered records: {filteredData.Count:N0}{Environment.NewLine}");
                txtLog.AppendText($"  - Invalid dates: {invalidDates:N0}{Environment.NewLine}");
                txtLog.AppendText($"  - Filter time: {stopwatch.ElapsedMilliseconds}ms{Environment.NewLine}{Environment.NewLine}");

                lblStatus.Text = $"Filtered: {filteredData.Count:N0} of {mockupData.Count:N0} records";
                lblStatus.ForeColor = Color.Green;

                // ghi v�o log
                string jsonData = JsonConvert.SerializeObject(filteredData);
                Logging.Write(Logging.WATCH, "Mockup filter test", jsonData);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error testing mockup filter: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void btnTestServer_Click(object sender, EventArgs e)
        {
            try
            {
                string serverIP = txtServerIP.Text.Trim();
                string serverPortStr = txtServerPort.Text.Trim();
                string machineNumberStr = txtMachineNumber.Text.Trim();
                string machineIP = txtMachineIP.Text.Trim();
                string machinePortStr = txtMachinePort.Text.Trim();
                
                // Validate inputs
                if (string.IsNullOrEmpty(serverIP))
                {
                    MessageBox.Show("Please enter server IP address", "Validation Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                
                if (!int.TryParse(serverPortStr, out int serverPort) || serverPort < 1 || serverPort > 65535)
                {
                    MessageBox.Show("Please enter a valid server port number (1-65535)", "Validation Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                
                if (!int.TryParse(machineNumberStr, out int machineNumber))
                {
                    MessageBox.Show("Please enter a valid machine number", "Validation Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                
                if (string.IsNullOrEmpty(machineIP))
                {
                    MessageBox.Show("Please enter machine IP address", "Validation Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                
                if (!int.TryParse(machinePortStr, out int machinePort) || machinePort < 1 || machinePort > 65535)
                {
                    MessageBox.Show("Please enter a valid machine port number (1-65535)", "Validation Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                
                Cursor = Cursors.WaitCursor;
                lblStatus.Text = "Connecting to server...";
                lblStatus.ForeColor = Color.Blue;
                Application.DoEvents();

                var stopwatch = Stopwatch.StartNew();

                // Connect to server with timeout
                using (TcpClient client = new TcpClient())
                {
                    // Set connection timeout
                    var result = client.BeginConnect(serverIP, serverPort, null, null);
                    var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(5));
                    
                    if (!success)
                    {
                        throw new Exception("Connection timeout - server may not be running");
                    }
                    
                    client.EndConnect(result);
                    
                    // Set read/write timeouts
                    // Increased read timeout to 120 seconds to handle large datasets from biometric devices
                    client.ReceiveTimeout = 120000; // 120 seconds (2 minutes)
                    client.SendTimeout = 10000; // 10 seconds
                    
                    using (StreamReader reader = new StreamReader(client.GetStream()))
                    using (StreamWriter writer = new StreamWriter(client.GetStream()) { AutoFlush = true })
                    {
                        // Build request: machineNumber|ip|port|fromDate|toDate
                        string fromDateStr = chkUseDateFilter.Checked ? dtpFrom.Value.ToString("yyyy-MM-dd HH:mm:ss") : "";
                        string toDateStr = chkUseDateFilter.Checked ? dtpTo.Value.AddDays(1).AddSeconds(-1).ToString("yyyy-MM-dd HH:mm:ss") : "";
                        
                        string request = $"{machineNumber}|{machineIP}|{machinePort}|{fromDateStr}|{toDateStr}";
                        
                        txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] Sending request: {request}{Environment.NewLine}");
                        writer.WriteLine(request);

                        // Read response using StringBuilder for efficiency
                        var responseBuilder = new System.Text.StringBuilder();
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            if (line == "EXIT") break;
                            responseBuilder.Append(line);
                        }
                        
                        string response = responseBuilder.ToString();
                        stopwatch.Stop();

                        if (response.StartsWith("ERROR"))
                        {
                            txtLog.AppendText($"  - Server error: {response}{Environment.NewLine}{Environment.NewLine}");
                            lblStatus.Text = "Server returned error";
                            lblStatus.ForeColor = Color.Red;
                        }
                        else
                        {
                            var data = JsonConvert.DeserializeObject<List<GLogData>>(response);
                            
                            txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] Server response:{Environment.NewLine}");
                            txtLog.AppendText($"  - Records received: {data.Count:N0}{Environment.NewLine}");
                            txtLog.AppendText($"  - Total time: {stopwatch.ElapsedMilliseconds}ms{Environment.NewLine}");
                            txtLog.AppendText($"  - Data size: {response.Length / 1024:N0} KB{Environment.NewLine}{Environment.NewLine}");

                            lblStatus.Text = $"Received {data.Count:N0} records in {stopwatch.ElapsedMilliseconds}ms";
                            lblStatus.ForeColor = Color.Green;
                            
                            // Save response to log file
                            try
                            {
                                string logFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log");
                                if (!Directory.Exists(logFolder))
                                {
                                    Directory.CreateDirectory(logFolder);
                                }
                                
                                string logFileName = Path.Combine(logFolder, $"ServerResponse_{DateTime.Now:yyyyMMdd_HHmmss}.json");
                                File.WriteAllText(logFileName, response);
                                
                                txtLog.AppendText($"  - Saved response to: {logFileName}{Environment.NewLine}{Environment.NewLine}");
                            }
                            catch (Exception logEx)
                            {
                                txtLog.AppendText($"  - Warning: Could not save log file: {logEx.Message}{Environment.NewLine}{Environment.NewLine}");
                            }
                        }
                    }
                }
            }
            catch (TimeoutException tex)
            {
                lblStatus.Text = "Connection timeout";
                lblStatus.ForeColor = Color.Red;
                txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] Timeout: {tex.Message}{Environment.NewLine}{Environment.NewLine}");
                MessageBox.Show($"Connection timeout. Please ensure:\n1. Server is running\n2. Server IP and Port are correct\n3. Firewall is not blocking the connection", "Connection Timeout",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (System.Net.Sockets.SocketException sex)
            {
                lblStatus.Text = "Connection failed";
                lblStatus.ForeColor = Color.Red;
                txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] Connection failed: {sex.Message}{Environment.NewLine}{Environment.NewLine}");
                MessageBox.Show($"Connection failed. Please ensure:\n1. Server is running\n2. Server IP ({txtServerIP.Text}) and Port ({txtServerPort.Text}) are correct\n\nError: {sex.Message}", "Connection Failed",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Connection error";
                lblStatus.ForeColor = Color.Red;
                txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] Error: {ex.Message}{Environment.NewLine}{Environment.NewLine}");
                MessageBox.Show($"Error connecting to server: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void btnClearLog_Click(object sender, EventArgs e)
        {
            txtLog.Clear();
        }

        private void btnTestServerMockup_Click(object sender, EventArgs e)
        {
            try
            {
                string serverIP = txtServerIP.Text.Trim();
                string serverPortStr = txtServerPort.Text.Trim();
                string machineNumberStr = txtMachineNumber.Text.Trim();

                // Validate inputs
                if (string.IsNullOrEmpty(serverIP))
                {
                    MessageBox.Show("Please enter server IP address", "Validation Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!int.TryParse(serverPortStr, out int serverPort) || serverPort < 1 || serverPort > 65535)
                {
                    MessageBox.Show("Please enter a valid server port number (1-65535)", "Validation Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!int.TryParse(machineNumberStr, out int machineNumber))
                {
                    MessageBox.Show("Please enter a valid machine number", "Validation Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                Cursor = Cursors.WaitCursor;
                lblStatus.Text = "Connecting to server with mockup data request...";
                lblStatus.ForeColor = Color.Blue;
                Application.DoEvents();

                var stopwatch = Stopwatch.StartNew();

                // Connect to server with timeout
                using (TcpClient client = new TcpClient())
                {
                    // Set connection timeout
                    var result = client.BeginConnect(serverIP, serverPort, null, null);
                    var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(5));
                    
                    if (!success)
                    {
                        throw new Exception("Connection timeout - server may not be running");
                    }
                    
                    client.EndConnect(result);
                    
                    // Set read/write timeouts
                    // Increased read timeout to 120 seconds to handle large mockup datasets (e.g., may 5.txt with ~189K records)
                    client.ReceiveTimeout = 120000; // 120 seconds (2 minutes)
                    client.SendTimeout = 10000; // 10 seconds

                    using (StreamReader reader = new StreamReader(client.GetStream()))
                    using (StreamWriter writer = new StreamWriter(client.GetStream()) { AutoFlush = true })
                    {
                        // Build request: MOCKUP_GETLOGS|machineNumber|dummy_ip|dummy_port|fromDate|toDate
                        string fromDateStr = chkUseDateFilter.Checked ? dtpFrom.Value.ToString("yyyy-MM-dd HH:mm:ss") : "";
                        string toDateStr = chkUseDateFilter.Checked ? dtpTo.Value.AddDays(1).AddSeconds(-1).ToString("yyyy-MM-dd HH:mm:ss") : "";

                        string request = $"MOCKUP_GETLOGS|{machineNumber}|0.0.0.0|0|{fromDateStr}|{toDateStr}";

                        txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] Sending mockup request: {request}{Environment.NewLine}");
                        writer.WriteLine(request);

                        // Read response using StringBuilder for efficiency
                        var responseBuilder = new System.Text.StringBuilder();
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            if (line == "EXIT") break;
                            responseBuilder.Append(line);
                        }

                        string response = responseBuilder.ToString();
                        stopwatch.Stop();

                        if (response.StartsWith("ERROR"))
                        {
                            txtLog.AppendText($"  - Server error: {response}{Environment.NewLine}{Environment.NewLine}");
                            lblStatus.Text = "Server returned error";
                            lblStatus.ForeColor = Color.Red;
                        }
                        else
                        {
                            var data = JsonConvert.DeserializeObject<List<GLogData>>(response);

                            txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] Server response (mockup data):{Environment.NewLine}");
                            txtLog.AppendText($"  - Records received: {data.Count:N0}{Environment.NewLine}");
                            txtLog.AppendText($"  - Total time: {stopwatch.ElapsedMilliseconds}ms{Environment.NewLine}");
                            txtLog.AppendText($"  - Data size: {response.Length / 1024:N0} KB{Environment.NewLine}");
                            
                            if (data.Count == 0)
                            {
                                txtLog.AppendText($"  - WARNING: No records returned. Check machine number (available: 5, 6, 7, 8){Environment.NewLine}{Environment.NewLine}");
                                lblStatus.Text = "No records received - check machine number";
                                lblStatus.ForeColor = Color.Orange;
                            }
                            else
                            {
                                txtLog.AppendText($"{Environment.NewLine}");
                                lblStatus.Text = $"Received {data.Count:N0} mockup records in {stopwatch.ElapsedMilliseconds}ms";
                                lblStatus.ForeColor = Color.Green;
                            }

                            // Save response to log file
                            try
                            {
                                string logFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log");
                                if (!Directory.Exists(logFolder))
                                {
                                    Directory.CreateDirectory(logFolder);
                                }

                                string logFileName = Path.Combine(logFolder, $"ServerMockupResponse_{DateTime.Now:yyyyMMdd_HHmmss}.json");
                                File.WriteAllText(logFileName, response);

                                txtLog.AppendText($"  - Saved response to: {logFileName}{Environment.NewLine}{Environment.NewLine}");
                            }
                            catch (Exception logEx)
                            {
                                txtLog.AppendText($"  - Warning: Could not save log file: {logEx.Message}{Environment.NewLine}{Environment.NewLine}");
                            }
                        }
                    }
                }
            }
            catch (TimeoutException tex)
            {
                lblStatus.Text = "Connection timeout";
                lblStatus.ForeColor = Color.Red;
                txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] Timeout: {tex.Message}{Environment.NewLine}{Environment.NewLine}");
                MessageBox.Show($"Connection timeout. Please ensure:\n1. Server is running\n2. Server IP and Port are correct\n3. Firewall is not blocking the connection", "Connection Timeout",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (System.Net.Sockets.SocketException sex)
            {
                lblStatus.Text = "Connection failed";
                lblStatus.ForeColor = Color.Red;
                txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] Connection failed: {sex.Message}{Environment.NewLine}{Environment.NewLine}");
                MessageBox.Show($"Connection failed. Please ensure:\n1. Server is running\n2. Server IP ({txtServerIP.Text}) and Port ({txtServerPort.Text}) are correct\n\nError: {sex.Message}", "Connection Failed",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Connection error";
                lblStatus.ForeColor = Color.Red;
                txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] Error: {ex.Message}{Environment.NewLine}{Environment.NewLine}");
                MessageBox.Show($"Error connecting to server: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }
    }
}
