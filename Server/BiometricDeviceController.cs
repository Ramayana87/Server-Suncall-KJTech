using Apzon.Api.Controllers.Sales;
using Apzon.Commons;
using SAPbobsCOM;
using System;
using System.Data;
using System.Diagnostics;
using System.Net.Sockets;
using System.Net;
using System.Web.Http;
using Apzon.Entities.Models.HumanResources.Biometric;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Apzon.Api.Controllers.HumanResources.TimeSheeting
{
    public class BiometricDeviceController : BaseApiController
    {

        [HttpPost]
        public DataTable GetLogDataTable([FromBody] DataTable dtSearch)
        {
            try
            {
                if (null == dtSearch || dtSearch.Rows.Count == 0)
                {
                    return new DataTable();
                }
                string machineNumber = Function.ToString(dtSearch.Rows[0]["machineNumber"]);
                string ip = Function.ToString(dtSearch.Rows[0]["ip"]);
                int port = Function.ParseInt(dtSearch.Rows[0]["port"]);
                DateTime fromDate = Function.ParseDateTimes(dtSearch.Rows[0]["fromDate"]);
                DateTime toDate = Function.ParseDateTimes(dtSearch.Rows[0]["toDate"]);
                DataTable dt = UnitOfWork.BusinessMasterData.GetBPTableStructure("APZ_TBD1");
                dt.Columns.Add("EnrollName", typeof(string));
                Logging.Write(Logging.WATCH, new StackTrace(new StackFrame(0)).ToString().Substring(5, new StackTrace(new StackFrame(0)).ToString().Length - 5), "Start connect to socket server...");
                var client = new TcpClient();
                IPEndPoint IpEnd = new IPEndPoint(IPAddress.Parse(Global.BiometricServer), Global.BiometricPort);
                client.Connect(IpEnd);
                if (client.Connected)
                {

                    var STW = new StreamWriter(client.GetStream());
                    var STR = new StreamReader(client.GetStream());
                    STW.AutoFlush = true;
                    Logging.Write(Logging.WATCH, new StackTrace(new StackFrame(0)).ToString().Substring(5, new StackTrace(new StackFrame(0)).ToString().Length - 5), "Connected to socket server!");

                    // 2. send
                    STW.WriteLine(string.Format("{0}|{1}|{2}|{3}|{4}", machineNumber, ip, port, fromDate, toDate));

                    // 3. receive
                    while (client.Connected)
                    {
                        var recieve = STR.ReadLine();

                        if (recieve.ToUpper() == "EXIT")
                        {
                            STW.WriteLine("EXIT");
                            break;
                        }
                        else
                        {
                            Logging.Write(Logging.WATCH, new StackTrace(new StackFrame(0)).ToString().Substring(5, new StackTrace(new StackFrame(0)).ToString().Length - 5), "recieved data!");
                            var ListData = JsonConvert.DeserializeObject<List<GLogData>>(recieve);
                            foreach (var data in ListData)
                            {
                                DateTime inputDate = Function.ParseDateTimes(data.Time);
                                if (fromDate.Date <= inputDate.Date && inputDate.Date <= toDate.Date && data.Result.Equals("Granted"))
                                {
                                    var enrollName = "";
                                    var dr = dt.NewRow();
                                    dr["MachineNo"] = machineNumber;
                                    dr["EnrollNo"] = int.Parse(data.ID);
                                    dr["EnrollName"] = enrollName;
                                    dr["DateTimeRecord"] = inputDate.ToString();
                                    dr["Source"] = data.vDoorMode;
                                    dr["ClockDate"] = inputDate.Date;

                                    dt.Rows.Add(dr);
                                }
                            }
                        }
                    }
                    STW.Close();
                    STR.Close();
                    client.Close();
                    Logging.Write(Logging.WATCH, new StackTrace(new StackFrame(0)).ToString().Substring(5, new StackTrace(new StackFrame(0)).ToString().Length - 5), "Client disconnect!");
                    return dt;
                }
                else
                {
                    Logging.Write(Logging.WATCH, new StackTrace(new StackFrame(0)).ToString().Substring(5, new StackTrace(new StackFrame(0)).ToString().Length - 5), "Client did not connect!");
                }
            }
            catch (Exception ex)
            {
                Logging.Write(Logging.ERROR, new StackTrace(new StackFrame(0)).ToString().Substring(5, new StackTrace(new StackFrame(0)).ToString().Length - 5), ex.Message);
            }
            return new DataTable();

        }

        [HttpGet]
        public DataTable GetAllUserTable(int machineNumber, string ip, int port)
        {
            try
            {
                DataTable dt = UnitOfWork.BusinessMasterData.GetBPTableStructure("APZ_OTEM");
                Logging.Write(Logging.WATCH, new StackTrace(new StackFrame(0)).ToString().Substring(5, new StackTrace(new StackFrame(0)).ToString().Length - 5), "Start connect to socket server...");
                var client = new TcpClient();
                IPEndPoint IpEnd = new IPEndPoint(IPAddress.Parse(Global.BiometricServer), Global.BiometricPort);
                client.Connect(IpEnd);
                if (client.Connected)
                {

                    var STW = new StreamWriter(client.GetStream());
                    var STR = new StreamReader(client.GetStream());
                    STW.AutoFlush = true;
                    Logging.Write(Logging.WATCH, new StackTrace(new StackFrame(0)).ToString().Substring(5, new StackTrace(new StackFrame(0)).ToString().Length - 5), "Connected to socket server!");

                    // 2. send
                    STW.WriteLine(string.Format("{0}|{1}|{2}", machineNumber, ip, port));

                    // 3. receive
                    while (client.Connected)
                    {
                        var recieve = STR.ReadLine();

                        if (recieve.ToUpper() == "EXIT")
                        {
                            STW.WriteLine("EXIT");
                            break;
                        }
                        else
                        {
                            Logging.Write(Logging.WATCH, new StackTrace(new StackFrame(0)).ToString().Substring(5, new StackTrace(new StackFrame(0)).ToString().Length - 5), "recieved data!");
                            var glogList = JsonConvert.DeserializeObject<List<GLogData>>(recieve);
                            if (glogList.Count > 0)
                            {
                                var newLst = glogList.GroupBy(m => m.ID).Select(m => m.First()).Distinct().Where(m => m.Result.Equals("Granted") && m.Method.Equals("by FP")).ToList();
                                foreach (var item in newLst)
                                {
                                    var r = dt.NewRow();
                                    r["MachineNo"] = machineNumber;
                                    r["EnrollNo"] = item.ID;
                                    r["EnrollName"] = "";
                                    dt.Rows.Add(r);
                                }
                            }
                        }
                    }
                    STW.Close();
                    STR.Close();
                    client.Close();
                    Logging.Write(Logging.WATCH, new StackTrace(new StackFrame(0)).ToString().Substring(5, new StackTrace(new StackFrame(0)).ToString().Length - 5), "Client disconnect!");
                    return dt;
                }
                else
                {
                    Logging.Write(Logging.WATCH, new StackTrace(new StackFrame(0)).ToString().Substring(5, new StackTrace(new StackFrame(0)).ToString().Length - 5), "Client did not connect!");
                }
            }
            catch (Exception ex)
            {
                Logging.Write(Logging.ERROR, new StackTrace(new StackFrame(0)).ToString().Substring(5, new StackTrace(new StackFrame(0)).ToString().Length - 5), ex.Message);
            }
            return new DataTable();

        }

    }
}
