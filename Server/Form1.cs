using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using System.Diagnostics;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;
using System.Threading;
using System.Xml.Linq;

namespace Server
{
    public partial class Form1 : Form
    {
        private TcpClient client;
        public StreamReader STR;
        public StreamWriter STW;
        public string recieve;
        public String TextToSend;
        private Socket sock = null;
        const int MAX_CONNECTION = 10;
        static int _connectionsCount = 0;
        bool statusOpen = true;
        TcpListener listener;
        int machineNumber;
        string ip;
        int port;
        DateTime fromDate;
        DateTime toDate;
        public Form1()
        {
            InitializeComponent();
            IPAddress[] localIP = Dns.GetHostAddresses(Dns.GetHostName());

            foreach (IPAddress address in localIP)
            {
                if (address.AddressFamily == AddressFamily.InterNetwork)
                {
                    txtIP.Text = address.ToString();
                }
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                txtPort.Text = "9999";
            }
            catch (AxHost.InvalidActiveXStateException ex)
            {
                Logging.Write(Logging.ERROR, new StackTrace(new StackFrame(0)).ToString().Substring(5, new StackTrace(new StackFrame(0)).ToString().Length - 5), ex.Message);
            }
            catch (Exception ex)
            {
                Logging.Write(Logging.ERROR, new StackTrace(new StackFrame(0)).ToString().Substring(5, new StackTrace(new StackFrame(0)).ToString().Length - 5), ex.Message);
            }
        }

        private async void btnStart_ClickAsync(object sender, EventArgs e)
        {
            try
            {
                statusOpen = true;
                lblStatus.Text = "starting";
                listener = new TcpListener(IPAddress.Any, int.Parse(txtPort.Text));
                listener.Start();
                //cách 1
                while (statusOpen)
                {
                    var client = await listener.AcceptTcpClientAsync();
                    Thread t = new Thread((obj) =>
                    {
                        DoWork(client);
                    });
                    t.Start(client);
                }

                // cách 2
                //while (_connectionsCount < MAX_CONNECTION || MAX_CONNECTION == 0)
                //{
                //    _connectionsCount++;

                //    new Thread(DoWork2).Start();
                //}

                listener.Stop();
                txtLogs.Text += "Server disConnected\n";
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Error";
                txtLogs.Text += "Server could not connect\n>>>" + ex.Message.ToString() + "\n";
                Logging.Write(Logging.ERROR, new StackTrace(new StackFrame(0)).ToString().Substring(5, new StackTrace(new StackFrame(0)).ToString().Length - 5), ex.Message);
            }


        }

        public void DoWork(TcpClient client)
        {
            try
            {

                var STR = new StreamReader(client.GetStream());
                var STW = new StreamWriter(client.GetStream());
                STW.AutoFlush = true;

                while (client.Connected)
                {
                    //receive
                    recieve = STR.ReadLine();
                    if (string.IsNullOrEmpty(recieve) || recieve.ToUpper() == "EXIT")
                    {
                        break;
                    }
                    this.txtLogs.Invoke(new MethodInvoker(delegate ()
                    {
                        txtLogs.Text += "\n" + recieve + "\t";
                        lblStatus.Text = "connected";
                    }));
                    var lstParam = recieve.Split('|').ToList();
                    if (lstParam.Count > 2)
                    {
                        machineNumber = ParseInt(lstParam[0]);
                        ip = ToString(lstParam[1]);
                        port = ParseInt(lstParam[2]);
                    }
                    // Parse date range parameters if provided
                    if (lstParam.Count > 4)
                    {
                        fromDate = ParseDateTimes(lstParam[3]);
                        toDate = ParseDateTimes(lstParam[4]);
                    }
                    else
                    {
                        // Default: last 30 days to today
                        fromDate = DateTime.Now.AddDays(-30);
                        toDate = DateTime.Now;
                    }

                    //send
                    List<GLogData> table = getData(machineNumber, ip, port, fromDate, toDate);
                    var jsonData = JsonConvert.SerializeObject(table);
                    STW.WriteLine(jsonData);
                    STW.WriteLine("EXIT");
                    recieve = "";
                }
                STW.Close();
                STR.Close();
                client.Close();
            }
            catch (Exception ex)
            {
                Logging.Write(Logging.ERROR, new StackTrace(new StackFrame(0)).ToString().Substring(5, new StackTrace(new StackFrame(0)).ToString().Length - 5), ex.Message);
            }
        }

        public async void DoWork2()
        {
            try
            {
                var client = await listener.AcceptTcpClientAsync();
                var STR = new StreamReader(client.GetStream());
                var STW = new StreamWriter(client.GetStream());
                STW.AutoFlush = true;

                while (client.Connected)
                {
                    recieve = STR.ReadLine();
                    if (recieve.ToUpper() == "EXIT")
                    {
                        break;
                    }
                    this.txtLogs.Invoke(new MethodInvoker(delegate ()
                    {
                        txtLogs.AppendText("You:" + recieve + "\n");
                        lblStatus.Text = "connected";
                    }));
                    recieve = "";

                    //send
                    //DataTable table = getData();
                    //var jsonData = JsonConvert.SerializeObject(table);
                    //STW.WriteLine(jsonData);
                    STW.WriteLine("EXIT");
                }
                STW.Close();
                STR.Close();
                client.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            try
            {
                statusOpen = false;
                //TcpListener listener = new TcpListener(IPAddress.Any, int.Parse(txtPort.Text));
                //listener.Stop();
                //client = listener.AcceptTcpClient();
                //STR = new StreamReader(client.GetStream());
                //STW = new StreamWriter(client.GetStream());
                //STW.AutoFlush = false;

                //backgroundWorker1.CancelAsync();
                //backgroundWorker2.WorkerSupportsCancellation = false;
                //txtLogs.Text += "Server disConnected\n";
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Error";
                txtLogs.Text += "Server could not disconnect\n>>>" + ex.Message.ToString() + "\n";
                Logging.Write(Logging.ERROR, new StackTrace(new StackFrame(0)).ToString().Substring(5, new StackTrace(new StackFrame(0)).ToString().Length - 5), ex.Message);
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            backgroundWorker2.RunWorkerAsync();
            backgroundWorker2.WorkerSupportsCancellation = true;
        }

        private void btnSend_Click(object sender, EventArgs e)
        {

            if (txtMSG.Text != "")
            {
                TextToSend = txtMSG.Text;
                backgroundWorker2.RunWorkerAsync();
            }
            txtMSG.Text = "";
        }

        private void backgroundWorker2_DoWork(object sender, DoWorkEventArgs e)
        {
            if (client.Connected)
            {
                STW.WriteLine(TextToSend);
                this.txtLogs.Invoke(new MethodInvoker(delegate ()
                {
                    txtLogs.AppendText("Me:" + TextToSend + "\n");
                    lblStatus.Text = "connected - sent";
                }));
            }
            //backgroundWorker2.CancelAsync();
        }
        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            while (client.Connected)
            {
                try
                {
                    recieve = STR.ReadLine();
                    this.txtLogs.Invoke(new MethodInvoker(delegate ()
                    {
                        txtLogs.AppendText("You:" + recieve + "\n");
                        lblStatus.Text = "connected";
                    }));
                    recieve = "";

                }
                catch (Exception ex)
                {
                    lblStatus.Text = "Error";
                    txtLogs.Text += "Server could not connect\n>>>" + ex.Message.ToString() + "\n";
                }
            }
        }

        private List<GLogData> getData(int machineNumber, string ip, int port, DateTime fromDate, DateTime toDate)
        {
            try
            {
                List<GLogData> lstData = new List<GLogData>();
                if (SFC3KPC1.ConnectTcpip(machineNumber, ip, port, 0))
                {
                    bool bRet;
                    int i;

                    bRet = SFC3KPC1.StartReadGeneralLogData(machineNumber);
                    Logging.Write(Logging.WATCH, new StackTrace(new StackFrame(0)).ToString().Substring(5, new StackTrace(new StackFrame(0)).ToString().Length - 5), GetErrorString());
                    Logging.Write(Logging.WATCH, new StackTrace(new StackFrame(0)).ToString().Substring(5, new StackTrace(new StackFrame(0)).ToString().Length - 5), "Reading...");

                    bRet = SFC3KPC1.ReadGeneralLogData(machineNumber);
                    Logging.Write(Logging.WATCH, new StackTrace(new StackFrame(0)).ToString().Substring(5, new StackTrace(new StackFrame(0)).ToString().Length - 5), GetErrorString());
                    if (bRet)
                    {
                        Logging.Write(Logging.WATCH, new StackTrace(new StackFrame(0)).ToString().Substring(5, new StackTrace(new StackFrame(0)).ToString().Length - 5), "Getting...");
                        i = 1;
                        int skippedCount = 0;
                        int totalProcessed = 0;
                        
                        // Pre-calculate year range for faster filtering
                        int minYear = fromDate.Year;
                        int maxYear = toDate.Year;
                        
                        while (true)
                        {
                            GLogData data = new GLogData();
                            bRet = SFC3KPC1.GetGeneralLogData(machineNumber,
                                ref data.vEnrollNumber,
                                ref data.vGranted,
                                ref data.vMethod,
                                ref data.vDoorMode,
                                ref data.vFunNumber,
                                ref data.vSensor,
                                ref data.vYear, ref data.vMonth, ref data.vDay, ref data.vHour, ref data.vMinute, ref data.vSecond);
                            if (!bRet) break;
                            
                            totalProcessed++;
                            
                            // Fast validation checks - fail fast
                            if (data.vEnrollNumber <= 0 || data.vGranted != 1)
                            {
                                skippedCount++;
                                continue;
                            }
                            
                            // Year range check - quick filter before creating DateTime
                            if (data.vYear < minYear || data.vYear > maxYear || data.vYear < 2024)
                            {
                                skippedCount++;
                                continue;
                            }
                            
                            // Create DateTime only for records that passed initial filters
                            try
                            {
                                DateTime inputDate = new DateTime(data.vYear, data.vMonth, data.vDay, data.vHour, data.vMinute, data.vSecond & 0xFF);
                                
                                // Date range filtering
                                if (inputDate < fromDate || inputDate > toDate)
                                {
                                    skippedCount++;
                                    continue;
                                }
                                
                                data.no = i;
                                lstData.Add(data);
                                i++;
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                                // Invalid date, skip this record
                                skippedCount++;
                                continue;
                            }
                        }
                        Logging.Write(Logging.WATCH, new StackTrace(new StackFrame(0)).ToString().Substring(5, new StackTrace(new StackFrame(0)).ToString().Length - 5), 
                            $"Read GLogData OK - Total: {totalProcessed}, Filtered: {lstData.Count}, Skipped: {skippedCount}");
                    }
                    SFC3KPC1.Disconnect(machineNumber);
                }
                return lstData;
            }
            catch (Exception ex)
            {
                Logging.Write(Logging.ERROR, new StackTrace(new StackFrame(0)).ToString().Substring(5, new StackTrace(new StackFrame(0)).ToString().Length - 5), ex.Message);
                return new List<GLogData>();
            }
        }

        public static DateTime ParseDateTimes(object obj)
        {
            if (obj is DateTime d) return d;

            try
            {
                if (obj == null) return DateTime.Now;
                if (obj != null && Convert.ToDateTime(obj) == DateTime.MinValue) return DateTime.Now;
                if (obj != null && DateTime.Parse(obj.ToString()).Year == 1899) return DateTime.Now;
                if (obj != null && DateTime.Parse(obj.ToString()).Year == 1900) return DateTime.Now;
                if (DateTime.TryParse(obj.ToString(), out DateTime result))
                    return result;
                return DateTime.Now;
            }
            catch (Exception)
            {
                return DateTime.Now;
            }
        }

        public static string ToString(object obj)
        {
            try
            {
                if (obj == null) return string.Empty;
                return obj.ToString().Trim();
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        public static int ParseInt(object obj)
        {
            if (obj is int) return (int)obj;

            try
            {
                if (obj == null) return 0;
                if (int.TryParse(obj.ToString(), out int result))
                    return result;
                return 0;
            }
            catch (Exception)
            {
                return 0;
            }
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
