using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Server
{
    /// <summary>
    /// Controller for managing biometric device communication and data retrieval
    /// Separates device communication logic from UI layer for better maintainability
    /// </summary>
    public class BiometricDeviceController
    {
        private readonly object lockObject = new object();
        private bool isConnected = false;
        private int currentMachineNumber = -1;
        private DateTime lastConnectionTime = DateTime.MinValue;
        private const int CONNECTION_TIMEOUT_SECONDS = 30;

        /// <summary>
        /// Gets attendance data from a biometric device with optional date filtering
        /// </summary>
        /// <param name="machineNumber">Machine identifier</param>
        /// <param name="ip">Device IP address</param>
        /// <param name="port">Device port</param>
        /// <param name="fromDate">Optional start date for filtering</param>
        /// <param name="toDate">Optional end date for filtering</param>
        /// <returns>List of attendance log data</returns>
        public AttendanceDataResult GetAttendanceData(
            int machineNumber, 
            string ip, 
            int port, 
            DateTime? fromDate = null, 
            DateTime? toDate = null)
        {
            var result = new AttendanceDataResult();
            var stopwatch = Stopwatch.StartNew();

            lock (lockObject)
            {
                try
                {
                    // Connect to device
                    if (!ConnectToDevice(machineNumber, ip, port))
                    {
                        result.Success = false;
                        result.ErrorMessage = "Failed to connect to device";
                        Logging.Write(Logging.ERROR, "BiometricDeviceController", result.ErrorMessage);
                        return result;
                    }

                    // Read data from device
                    var readResult = ReadDeviceData(machineNumber, fromDate, toDate);
                    result.Data = readResult.Data;
                    result.TotalRecords = readResult.TotalRecords;
                    result.FilteredRecords = readResult.FilteredRecords;
                    result.InvalidRecords = readResult.InvalidRecords;
                    result.Success = true;

                    stopwatch.Stop();
                    result.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;

                    // Log success
                    string filterInfo = (fromDate.HasValue || toDate.HasValue)
                        ? $" (filtered {result.FilteredRecords}, invalid {result.InvalidRecords} from {result.TotalRecords} total)"
                        : $" (invalid {result.InvalidRecords} from {result.TotalRecords} total)";

                    Logging.Write(Logging.WATCH, "BiometricDeviceController",
                        $"Successfully retrieved {result.Data.Count} records in {result.ElapsedMilliseconds}ms{filterInfo}");
                }
                catch (Exception ex)
                {
                    result.Success = false;
                    result.ErrorMessage = ex.Message;
                    Logging.Write(Logging.ERROR, "BiometricDeviceController", $"Error: {ex.Message}");
                }
                finally
                {
                    // Always disconnect after operation
                    DisconnectFromDevice(machineNumber);
                }
            }

            return result;
        }

        /// <summary>
        /// Connects to the biometric device
        /// </summary>
        private bool ConnectToDevice(int machineNumber, string ip, int port)
        {
            try
            {
                // Validate input parameters
                if (string.IsNullOrWhiteSpace(ip))
                {
                    Logging.Write(Logging.ERROR, "BiometricDeviceController", "Invalid IP address");
                    return false;
                }

                if (port <= 0 || port > 65535)
                {
                    Logging.Write(Logging.ERROR, "BiometricDeviceController", $"Invalid port: {port}");
                    return false;
                }

                // Attempt connection
                bool connected = SFC3KPC1.ConnectTcpip(machineNumber, ip, port, 0);
                
                if (connected)
                {
                    isConnected = true;
                    currentMachineNumber = machineNumber;
                    lastConnectionTime = DateTime.Now;
                    Logging.Write(Logging.WATCH, "BiometricDeviceController", 
                        $"Connected to device {machineNumber} at {ip}:{port}");
                }
                else
                {
                    string errorMsg = GetErrorString();
                    Logging.Write(Logging.ERROR, "BiometricDeviceController", 
                        $"Connection failed: {errorMsg}");
                }

                return connected;
            }
            catch (Exception ex)
            {
                Logging.Write(Logging.ERROR, "BiometricDeviceController", 
                    $"Connection exception: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Reads attendance data from the connected device
        /// </summary>
        private ReadDataResult ReadDeviceData(int machineNumber, DateTime? fromDate, DateTime? toDate)
        {
            var result = new ReadDataResult();

            try
            {
                // Start reading attendance log data
                bool success = SFC3KPC1.StartReadGeneralLogData(machineNumber);
                string startMsg = GetErrorString();
                Logging.Write(Logging.WATCH, "BiometricDeviceController", $"Start reading: {startMsg}");

                if (!success)
                {
                    Logging.Write(Logging.ERROR, "BiometricDeviceController", "Failed to start reading data");
                    return result;
                }

                // Read the data buffer
                success = SFC3KPC1.ReadGeneralLogData(machineNumber);
                string readMsg = GetErrorString();
                Logging.Write(Logging.WATCH, "BiometricDeviceController", $"Read result: {readMsg}");

                if (!success)
                {
                    Logging.Write(Logging.ERROR, "BiometricDeviceController", "Failed to read data buffer");
                    return result;
                }

                // Process individual records
                int recordNumber = 1;
                while (true)
                {
                    GLogData data = new GLogData();
                    success = SFC3KPC1.GetGeneralLogData(
                        machineNumber,
                        ref data.vEnrollNumber, ref data.vGranted, ref data.vMethod,
                        ref data.vDoorMode, ref data.vFunNumber, ref data.vSensor,
                        ref data.vYear, ref data.vMonth, ref data.vDay,
                        ref data.vHour, ref data.vMinute, ref data.vSecond);

                    if (!success) break;

                    result.TotalRecords++;

                    // Validate record
                    if (!IsValidRecord(data))
                    {
                        result.InvalidRecords++;
                        continue;
                    }

                    // Apply date filter if specified
                    if (!PassesDateFilter(data, fromDate, toDate, out bool isInvalidDate))
                    {
                        if (isInvalidDate)
                        {
                            result.InvalidRecords++;
                        }
                        else
                        {
                            result.FilteredRecords++;
                        }
                        continue;
                    }

                    // Add valid record to result
                    data.no = recordNumber++;
                    result.Data.Add(data);
                }
            }
            catch (Exception ex)
            {
                Logging.Write(Logging.ERROR, "BiometricDeviceController", 
                    $"Error reading device data: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Validates if a record meets basic criteria
        /// </summary>
        private bool IsValidRecord(GLogData data)
        {
            // Check if enrollment number is valid
            if (data.vEnrollNumber <= 0)
                return false;

            // Check if access was granted
            if (data.vGranted != 1)
                return false;

            // Validate year is reasonable
            if (data.vYear < 2000 || data.vYear > DateTime.Now.Year + 1)
                return false;

            return true;
        }

        /// <summary>
        /// Checks if a record passes the date filter
        /// </summary>
        private bool PassesDateFilter(GLogData data, DateTime? fromDate, DateTime? toDate, out bool isInvalidDate)
        {
            isInvalidDate = false;

            // If no date filter is specified, record passes
            if (!fromDate.HasValue && !toDate.HasValue)
                return true;

            try
            {
                // Create DateTime from record data
                DateTime recordDate = new DateTime(
                    data.vYear, data.vMonth, data.vDay,
                    data.vHour, data.vMinute, data.vSecond & 0xFF);

                // Check against date range
                if (fromDate.HasValue && recordDate < fromDate.Value)
                    return false;

                if (toDate.HasValue && recordDate > toDate.Value)
                    return false;

                return true;
            }
            catch (Exception ex)
            {
                // Invalid date in record
                Logging.Write(Logging.WATCH, "BiometricDeviceController", 
                    $"Invalid date in record: {ex.Message}");
                isInvalidDate = true;
                return false;
            }
        }

        /// <summary>
        /// Disconnects from the device
        /// </summary>
        private void DisconnectFromDevice(int machineNumber)
        {
            try
            {
                if (isConnected)
                {
                    SFC3KPC1.Disconnect(machineNumber);
                    isConnected = false;
                    currentMachineNumber = -1;
                    Logging.Write(Logging.WATCH, "BiometricDeviceController", 
                        $"Disconnected from device {machineNumber}");
                }
            }
            catch (Exception ex)
            {
                Logging.Write(Logging.ERROR, "BiometricDeviceController", 
                    $"Error during disconnect: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the error string from the device SDK
        /// </summary>
        private string GetErrorString()
        {
            int vErrorCode = 0;
            SFC3KPC1.GetLastError(ref vErrorCode);

            switch (vErrorCode)
            {
                case 0: return "No Error";
                case 1: return "Can't open com port";
                case 2: return "Can't set com port";
                case 3: return "Error in creating socket";
                case 4: return "Error in setting socket option";
                case 5: return "Error in connecting";
                case 6: return "Error in reconnecting";
                case 7: return "The Password (or the machine ID) is incorrect";
                case 8: return "Error in allocating memory in socket dll";
                case 101: return "Can't send data to device";
                case 102: return "Can't read data from device";
                case 103: return "Error in parameter";
                case 104: return "Invalid Data";
                case 105: return "The scope of data is incorrect";
                case 501: return "Can't operate the device properly (or none data)";
                case 502: return "All data have been read";
                case 503: return "Double access to FP(or CARD) data";
                case 504: return "Error in allocating memory in device";
                case 505: return "Invalid ID";
                case 506: return "All fingerprints are enrolled at the terminal";
                case 507: return "Fingerprint is already enrolled for this position";
                case 508: return "Invalid step number";
                case 509: return "Fingerprint duplicated while enrolling image";
                case 510: return "An error occurred while enrolling Nth image";
                case 511: return "Could not load Fingerprint image file";
                case 512: return "An error occurred while enrolling fingerprint image";
                case 513: return "Cannot open background bitmap";
                case 514: return "Invalid background bitmap";
                case 515: return "Bitmap dimension is invalid";
                case 516: return "Bitmap color depth is incorrect. It should be 24bit";
                case 517: return "This bitmap is compressed format. It should be an uncompressed bitmap";
                default: return $"Unknown error code: {vErrorCode}";
            }
        }

        /// <summary>
        /// Gets the connection status
        /// </summary>
        public bool IsConnected => isConnected;

        /// <summary>
        /// Gets the current machine number if connected
        /// </summary>
        public int CurrentMachineNumber => currentMachineNumber;
    }

    /// <summary>
    /// Result object for attendance data retrieval
    /// </summary>
    public class AttendanceDataResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public List<GLogData> Data { get; set; } = new List<GLogData>();
        public int TotalRecords { get; set; }
        public int FilteredRecords { get; set; }
        public int InvalidRecords { get; set; }
        public long ElapsedMilliseconds { get; set; }
    }

    /// <summary>
    /// Internal result object for read operation
    /// </summary>
    internal class ReadDataResult
    {
        public List<GLogData> Data { get; set; } = new List<GLogData>();
        public int TotalRecords { get; set; }
        public int FilteredRecords { get; set; }
        public int InvalidRecords { get; set; }
    }
}
