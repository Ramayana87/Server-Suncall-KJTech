using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
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
            // Set default values
            txtServerIP.Text = "127.0.0.1";
            txtServerPort.Text = "9999";
            
            // Find mockup data folder
            string currentDir = AppDomain.CurrentDomain.BaseDirectory;
            string projectRoot = Directory.GetParent(currentDir).Parent.Parent.FullName;
            mockupDataFolder = Path.Combine(projectRoot, "data mockup");
            
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

                foreach (var file in files)
                {
                    var lines = File.ReadAllLines(file);
                    totalLines += lines.Length;

                    foreach (var line in lines)
                    {
                        var parts = line.Split('\t');
                        if (parts.Length >= 8)
                        {
                            try
                            {
                                var data = new GLogData();
                                
                                // Parse line format: no, result, id, method, doormode, function, verification, time, captured
                                if (int.TryParse(parts[0].Trim(), out int no))
                                    data.no = no;
                                
                                string id = parts[2].Trim();
                                if (int.TryParse(id, out int enrollNum))
                                    data.vEnrollNumber = enrollNum;
                                
                                data.vGranted = parts[1].Trim() == "Granted" ? 1 : 0;
                                
                                // Parse date time
                                if (DateTime.TryParse(parts[7].Trim(), out DateTime recordTime))
                                {
                                    data.vYear = recordTime.Year;
                                    data.vMonth = recordTime.Month;
                                    data.vDay = recordTime.Day;
                                    data.vHour = recordTime.Hour;
                                    data.vMinute = recordTime.Minute;
                                    data.vSecond = recordTime.Second;
                                }
                                
                                if (data.vGranted == 1 && data.vEnrollNumber > 0)
                                {
                                    mockupData.Add(data);
                                    validRecords++;
                                }
                            }
                            catch
                            {
                                // Skip invalid lines
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
                        return false;
                    }
                }).ToList();

                stopwatch.Stop();

                txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] Mockup filter test:{Environment.NewLine}");
                txtLog.AppendText($"  - Date range: {fromDate:yyyy-MM-dd} to {toDate:yyyy-MM-dd}{Environment.NewLine}");
                txtLog.AppendText($"  - Total records: {mockupData.Count:N0}{Environment.NewLine}");
                txtLog.AppendText($"  - Filtered records: {filteredData.Count:N0}{Environment.NewLine}");
                txtLog.AppendText($"  - Filter time: {stopwatch.ElapsedMilliseconds}ms{Environment.NewLine}{Environment.NewLine}");

                lblStatus.Text = $"Filtered: {filteredData.Count:N0} of {mockupData.Count:N0} records";
                lblStatus.ForeColor = Color.Green;
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
                int serverPort = int.Parse(txtServerPort.Text.Trim());
                
                Cursor = Cursors.WaitCursor;
                lblStatus.Text = "Connecting to server...";
                lblStatus.ForeColor = Color.Blue;
                Application.DoEvents();

                var stopwatch = Stopwatch.StartNew();

                // Connect to server
                using (TcpClient client = new TcpClient())
                {
                    client.Connect(serverIP, serverPort);
                    
                    using (StreamReader reader = new StreamReader(client.GetStream()))
                    using (StreamWriter writer = new StreamWriter(client.GetStream()) { AutoFlush = true })
                    {
                        // Build request: machineNumber|ip|port|fromDate|toDate
                        string fromDateStr = chkUseDateFilter.Checked ? dtpFrom.Value.ToString("yyyy-MM-dd HH:mm:ss") : "";
                        string toDateStr = chkUseDateFilter.Checked ? dtpTo.Value.AddDays(1).AddSeconds(-1).ToString("yyyy-MM-dd HH:mm:ss") : "";
                        
                        string request = $"1|192.168.1.201|4370|{fromDateStr}|{toDateStr}";
                        
                        txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] Sending request: {request}{Environment.NewLine}");
                        writer.WriteLine(request);

                        // Read response
                        string response = "";
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            if (line == "EXIT") break;
                            response += line;
                        }

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
                        }
                    }
                }
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
    }
}
