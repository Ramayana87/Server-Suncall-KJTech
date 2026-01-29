using Apzon.Api.Controllers.Sales;
using Apzon.Commons;
using SAPbobsCOM;
using System;
using System.Data;
using System.Net.Sockets;
using System.Net;
using System.Web.Http;
using Apzon.Entities.Models.HumanResources.Biometric;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Apzon.Api.Controllers.HumanResources.TimeSheeting
{
    public class BiometricDeviceController : BaseApiController
    {
        [HttpPost]
        public DataTable GetLogDataTable([FromBody] DataTable dtSearch)
        {
            try
            {
                if (dtSearch == null || dtSearch.Rows.Count == 0)
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

                // Include GETLOGS operation type so server knows what data to retrieve
                string request = $"GETLOGS|{machineNumber}|{ip}|{port}|{fromDate}|{toDate}";

                var response = SendRequestToServer(request);
                if (response == null)
                {
                    return dt;
                }

                var logDataList = JsonConvert.DeserializeObject<List<GLogData>>(response);
                if (logDataList == null)
                {
                    return dt;
                }

                foreach (var data in logDataList)
                {
                    DateTime inputDate = Function.ParseDateTimes(data.Time);
                    if (fromDate.Date <= inputDate.Date && inputDate.Date <= toDate.Date && data.Result.Equals("Granted"))
                    {
                        var dr = dt.NewRow();
                        dr["MachineNo"] = machineNumber;
                        dr["EnrollNo"] = int.Parse(data.ID);
                        dr["EnrollName"] = "";
                        dr["DateTimeRecord"] = inputDate.ToString();
                        dr["Source"] = data.vDoorMode;
                        dr["ClockDate"] = inputDate.Date;
                        dt.Rows.Add(dr);
                    }
                }

                return dt;
            }
            catch (Exception ex)
            {
                Log(Logging.ERROR, ex.Message);
                return new DataTable();
            }
        }

        [HttpGet]
        public DataTable GetAllUserTable(int machineNumber, string ip, int port)
        {
            try
            {
                DataTable dt = UnitOfWork.BusinessMasterData.GetBPTableStructure("APZ_OTEM");

                // Include GETUSERS operation type so server knows what data to retrieve
                string request = $"GETUSERS|{machineNumber}|{ip}|{port}";

                var response = SendRequestToServer(request);
                if (response == null)
                {
                    return dt;
                }

                var glogList = JsonConvert.DeserializeObject<List<GLogData>>(response);
                if (glogList != null && glogList.Count > 0)
                {
                    var distinctUsers = glogList
                        .Where(m => m.Result.Equals("Granted") && m.Method.Equals("by FP"))
                        .GroupBy(m => m.ID)
                        .Select(m => m.First())
                        .ToList();

                    foreach (var item in distinctUsers)
                    {
                        var r = dt.NewRow();
                        r["MachineNo"] = machineNumber;
                        r["EnrollNo"] = item.ID;
                        r["EnrollName"] = "";
                        dt.Rows.Add(r);
                    }
                }

                return dt;
            }
            catch (Exception ex)
            {
                Log(Logging.ERROR, ex.Message);
                return new DataTable();
            }
        }

        /// <summary>
        /// Sends a request to the biometric socket server and returns the response data.
        /// </summary>
        /// <param name="request">The request string to send</param>
        /// <param name="callerName">Auto-populated caller method name for logging</param>
        /// <returns>The response data string, or null if connection failed</returns>
        private string SendRequestToServer(string request, [CallerMemberName] string callerName = "")
        {
            try
            {
                Log(Logging.WATCH, "Start connect to socket server...", callerName);

                var endpoint = new IPEndPoint(IPAddress.Parse(Global.BiometricServer), Global.BiometricPort);

                using (var client = new TcpClient())
                {
                    client.Connect(endpoint);
                    Log(Logging.WATCH, "Connected to socket server!", callerName);

                    using (var writer = new StreamWriter(client.GetStream()) { AutoFlush = true })
                    using (var reader = new StreamReader(client.GetStream()))
                    {
                        // Send request
                        writer.WriteLine(request);

                        // Receive single response
                        var response = reader.ReadLine();

                        if (string.IsNullOrEmpty(response) || response.ToUpper() == "EXIT")
                        {
                            writer.WriteLine("EXIT");
                            Log(Logging.WATCH, "Client disconnect!", callerName);
                            return null;
                        }

                        Log(Logging.WATCH, "Received data!", callerName);
                        writer.WriteLine("EXIT");
                        Log(Logging.WATCH, "Client disconnect!", callerName);
                        return response;
                    }
                }
            }
            catch (Exception ex)
            {
                Log(Logging.ERROR, ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Helper method for consistent logging format.
        /// </summary>
        private static void Log(string level, string message, [CallerMemberName] string callerName = "")
        {
            Logging.Write(level, callerName, message);
        }
    }
}
