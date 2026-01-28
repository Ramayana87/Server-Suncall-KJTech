using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ClientExample
{
    /// <summary>
    /// Example client showing how to connect to the optimized attendance socket server
    /// Ví dụ client kết nối đến server socket chấm công đã được tối ưu
    /// </summary>
    public class AttendanceClient
    {
        private string serverIP;
        private int serverPort;

        public AttendanceClient(string serverIP, int serverPort)
        {
            this.serverIP = serverIP;
            this.serverPort = serverPort;
        }

        /// <summary>
        /// Get attendance data for a specific date range (OPTIMIZED VERSION)
        /// Lấy dữ liệu chấm công theo khoảng thời gian (PHIÊN BẢN TỐI ƯU)
        /// </summary>
        public async Task<List<GLogData>> GetAttendanceDataAsync(int machineNumber, string deviceIP, int devicePort, DateTime fromDate, DateTime toDate)
        {
            return await Task.Run(() =>
            {
                TcpClient client = null;
                try
                {
                    // Connect to server
                    client = new TcpClient();
                    client.Connect(serverIP, serverPort);

                    var reader = new StreamReader(client.GetStream());
                    var writer = new StreamWriter(client.GetStream());
                    writer.AutoFlush = true;

                    // NEW FORMAT: Include date range parameters for server-side filtering
                    // FORMAT MỚI: Bao gồm tham số khoảng thời gian để server lọc dữ liệu
                    string request = $"{machineNumber}|{deviceIP}|{devicePort}|{fromDate:yyyy-MM-dd HH:mm:ss}|{toDate:yyyy-MM-dd HH:mm:ss}";
                    
                    Console.WriteLine($"Sending request: {request}");
                    writer.WriteLine(request);

                    // Read response
                    string jsonData = reader.ReadLine();
                    string exitSignal = reader.ReadLine(); // Should be "EXIT"

                    if (string.IsNullOrEmpty(jsonData) || exitSignal != "EXIT")
                    {
                        throw new Exception("Invalid response from server");
                    }

                    // Deserialize JSON response
                    var data = JsonConvert.DeserializeObject<List<GLogData>>(jsonData);
                    Console.WriteLine($"Received {data?.Count ?? 0} records");

                    return data ?? new List<GLogData>();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    return new List<GLogData>();
                }
                finally
                {
                    client?.Close();
                }
            });
        }

        /// <summary>
        /// Get attendance data for a specific date range (synchronous version)
        /// Lấy dữ liệu chấm công theo khoảng thời gian (phiên bản đồng bộ)
        /// </summary>
        public List<GLogData> GetAttendanceData(int machineNumber, string deviceIP, int devicePort, DateTime fromDate, DateTime toDate)
        {
            return GetAttendanceDataAsync(machineNumber, deviceIP, devicePort, fromDate, toDate).Result;
        }
        /// Lấy dữ liệu chấm công N ngày gần nhất (phương thức tiện lợi)
        /// </summary>
        public List<GLogData> GetRecentAttendanceData(int machineNumber, string deviceIP, int devicePort, int lastNDays = 7)
        {
            DateTime toDate = DateTime.Now;
            DateTime fromDate = toDate.AddDays(-lastNDays);
            return GetAttendanceData(machineNumber, deviceIP, devicePort, fromDate, toDate);
        }

        /// <summary>
        /// Get attendance data in chunks for large date ranges (prevents timeout)
        /// Lấy dữ liệu chấm công theo từng phần cho khoảng thời gian lớn (tránh timeout)
        /// </summary>
        public List<GLogData> GetAttendanceDataInChunks(int machineNumber, string deviceIP, int devicePort, 
            DateTime fromDate, DateTime toDate, int chunkDays = 30)
        {
            List<GLogData> allData = new List<GLogData>();
            DateTime currentDate = fromDate;

            while (currentDate < toDate)
            {
                DateTime chunkEnd = currentDate.AddDays(chunkDays);
                if (chunkEnd > toDate) chunkEnd = toDate;

                Console.WriteLine($"Fetching data from {currentDate:yyyy-MM-dd} to {chunkEnd:yyyy-MM-dd}");
                var chunkData = GetAttendanceData(machineNumber, deviceIP, devicePort, currentDate, chunkEnd);
                allData.AddRange(chunkData);

                currentDate = chunkEnd.AddDays(1);
            }

            Console.WriteLine($"Total records fetched: {allData.Count}");
            return allData;
        }
    }

    /// <summary>
    /// GLogData class matching the server's data structure
    /// Class GLogData khớp với cấu trúc dữ liệu từ server
    /// </summary>
    public class GLogData
    {
        public int no { get; set; }
        public int vEnrollNumber { get; set; }
        public int vGranted { get; set; }
        public int vMethod { get; set; }
        public int vDoorMode { get; set; }
        public int vFunNumber { get; set; }
        public int vSensor { get; set; }
        public int vYear { get; set; }
        public int vMonth { get; set; }
        public int vDay { get; set; }
        public int vHour { get; set; }
        public int vMinute { get; set; }
        public int vSecond { get; set; }
        public string userName { get; set; }

        public string Time
        {
            get { return $"{vYear:D4}-{vMonth:D2}-{vDay:D2} {vHour:D2}:{vMinute:D2}:{(vSecond & 0xFF):D2}"; }
        }

        public string ID
        {
            get { return vEnrollNumber == -1 ? "NONE" : $"{vEnrollNumber:D8}"; }
        }
    }

    /// <summary>
    /// Example usage / Ví dụ sử dụng
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            // Server configuration
            string serverIP = "192.168.1.100";  // Your server IP / IP server của bạn
            int serverPort = 9999;               // Your server port / Port server của bạn

            // Device configuration
            int machineNumber = 1;
            string deviceIP = "192.168.1.201";  // Attendance device IP / IP máy chấm công
            int devicePort = 4370;               // Attendance device port / Port máy chấm công

            var client = new AttendanceClient(serverIP, serverPort);

            // Example 1: Get last 7 days of attendance data
            // Ví dụ 1: Lấy dữ liệu chấm công 7 ngày gần nhất
            Console.WriteLine("\n=== Example 1: Last 7 days ===");
            var recentData = client.GetRecentAttendanceData(machineNumber, deviceIP, devicePort, 7);
            foreach (var record in recentData.Take(5)) // Show first 5 records
            {
                Console.WriteLine($"{record.ID} - {record.Time}");
            }

            // Example 2: Get data for specific date range
            // Ví dụ 2: Lấy dữ liệu cho khoảng thời gian cụ thể
            Console.WriteLine("\n=== Example 2: Specific date range ===");
            DateTime from = new DateTime(2024, 1, 1);
            DateTime to = new DateTime(2024, 1, 31);
            var monthData = client.GetAttendanceData(machineNumber, deviceIP, devicePort, from, to);
            Console.WriteLine($"Total records in January 2024: {monthData.Count}");

            // Example 3: Get large date range in chunks (prevents timeout)
            // Ví dụ 3: Lấy dữ liệu khoảng thời gian lớn theo từng phần (tránh timeout)
            Console.WriteLine("\n=== Example 3: Large date range (chunked) ===");
            DateTime yearStart = new DateTime(2024, 1, 1);
            DateTime yearEnd = new DateTime(2024, 12, 31);
            var yearData = client.GetAttendanceDataInChunks(machineNumber, deviceIP, devicePort, yearStart, yearEnd, 30);
            Console.WriteLine($"Total records in 2024: {yearData.Count}");

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}
