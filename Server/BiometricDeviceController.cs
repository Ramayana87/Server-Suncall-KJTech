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

            string request = $"{machineNumber}|{ip}|{port}|{fromDate}|{toDate}";

            var response = SendRequestToServer(request);
            if (response == null)
            {
                return new DataTable();
            }

            var logDataList = JsonConvert.DeserializeObject<List<GLogData>>(response);
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

        [HttpGet]
        public DataTable GetAllUserTable(int machineNumber, string ip, int port)
        {
            DataTable dt = UnitOfWork.BusinessMasterData.GetBPTableStructure("APZ_OTEM");

            string request = $"{machineNumber}|{ip}|{port}";

            var response = SendRequestToServer(request);
            if (response == null)
            {
                return new DataTable();
            }

            var glogList = JsonConvert.DeserializeObject<List<GLogData>>(response);
            if (glogList.Count > 0)
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

                    if (!client.Connected)
                    {
                        Log(Logging.WATCH, "Client did not connect!", callerName);
                        return null;
                    }

                    Log(Logging.WATCH, "Connected to socket server!", callerName);

                    using (var writer = new StreamWriter(client.GetStream()) { AutoFlush = true })
                    using (var reader = new StreamReader(client.GetStream()))
                    {
                        // Send request
                        writer.WriteLine(request);

                        // Receive response
                        while (client.Connected)
                        {
                            var response = reader.ReadLine();

                            if (string.IsNullOrEmpty(response) || response.ToUpper() == "EXIT")
                            {
                                writer.WriteLine("EXIT");
                                break;
                            }

                            Log(Logging.WATCH, "Received data!", callerName);
                            Log(Logging.WATCH, "Client disconnect!", callerName);
                            return response;
                        }
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                Log(Logging.ERROR, ex.Message, callerName);
                return null;
            }
        }

        /// <summary>
        /// Helper method for consistent logging format.
        /// </summary>
        private static void Log(string level, string message, string callerName)
        {
            Logging.Write(level, callerName, message);
        }
    }
}
