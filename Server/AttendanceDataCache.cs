using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace Server
{
    /// <summary>
    /// Cache manager for attendance data with incremental updates
    /// </summary>
    public class AttendanceDataCache
    {
        private readonly string _cacheDirectory;
        private readonly int _machineNumber;
        private readonly string _cacheFilePath;
        private readonly string _stateFilePath;

        private List<GLogData> _cachedData;
        private CacheState _cacheState;
        private readonly object _cacheLock = new object();

        public AttendanceDataCache(int machineNumber, string baseCacheDirectory = null)
        {
            _machineNumber = machineNumber;
            
            // Set cache directory relative to app directory
            if (string.IsNullOrEmpty(baseCacheDirectory))
            {
                baseCacheDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "cache");
            }
            
            _cacheDirectory = baseCacheDirectory;
            _cacheFilePath = Path.Combine(_cacheDirectory, $"cache_machine_{machineNumber}.json");
            _stateFilePath = Path.Combine(_cacheDirectory, $"cache_machine_{machineNumber}.state");

            _cachedData = new List<GLogData>();
            _cacheState = new CacheState();

            // Ensure cache directory exists
            if (!Directory.Exists(_cacheDirectory))
            {
                Directory.CreateDirectory(_cacheDirectory);
                Logging.Write(Logging.WATCH, "AttendanceDataCache", $"Created cache directory: {_cacheDirectory}");
            }

            // Try to load existing cache
            LoadCache();
        }

        /// <summary>
        /// Get attendance data with caching support
        /// </summary>
        public List<GLogData> GetAttendanceDataWithCache(
            Func<DateTime?, DateTime?, List<GLogData>> fetchFromDevice,
            DateTime? fromDate,
            DateTime? toDate)
        {
            lock (_cacheLock)
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                // Check if we need to fetch new data
                if (NeedToFetchNewData(fromDate, toDate))
                {
                    Logging.Write(Logging.WATCH, "AttendanceDataCache", 
                        $"🔄 Cache MISS: Fetching new data from machine {_machineNumber}...");

                    // Fetch new data from device
                    var newData = fetchFromDevice(fromDate, toDate);

                    if (newData != null && newData.Count > 0)
                    {
                        // Merge and deduplicate
                        var beforeCount = _cachedData.Count;
                        MergeAndDeduplicate(newData);
                        var afterCount = _cachedData.Count;
                        var addedCount = afterCount - beforeCount;

                        // Update cache state
                        UpdateCacheState();

                        // Save cache
                        SaveCache();

                        // Filter and return requested range
                        var filteredData = FilterByDateRange(_cachedData, fromDate, toDate);
                        
                        stopwatch.Stop();
                        Logging.Write(Logging.WATCH, "AttendanceDataCache", 
                            $"✅ Synced: {addedCount} new records, Total cache: {_cacheState.TotalRecordsInCache}, Returned: {filteredData.Count} ({stopwatch.ElapsedMilliseconds}ms)");

                        return filteredData;
                    }
                    else
                    {
                        // No new data, return from cache
                        var filteredData = FilterByDateRange(_cachedData, fromDate, toDate);
                        stopwatch.Stop();
                        Logging.Write(Logging.WATCH, "AttendanceDataCache",
                            $"✅ No new data from device. Returned {filteredData.Count} from cache ({stopwatch.ElapsedMilliseconds}ms)");
                        return filteredData;
                    }
                }
                else
                {
                    // Cache hit - return from cache
                    var filteredData = FilterByDateRange(_cachedData, fromDate, toDate);
                    stopwatch.Stop();
                    Logging.Write(Logging.WATCH, "AttendanceDataCache",
                        $"✅ Cache HIT: Returned {filteredData.Count} records from cache ({stopwatch.ElapsedMilliseconds}ms)");
                    return filteredData;
                }
            }
        }

        /// <summary>
        /// Check if we need to fetch new data from the device
        /// </summary>
        private bool NeedToFetchNewData(DateTime? fromDate, DateTime? toDate)
        {
            // If cache is empty, always fetch
            if (_cachedData.Count == 0)
            {
                return true;
            }

            // If cache is older than 24 hours, fetch
            if ((DateTime.Now - _cacheState.LastSyncTime).TotalHours > 24)
            {
                return true;
            }

            // If requested date range goes beyond cached data, fetch
            if (toDate.HasValue && _cacheState.LatestRecordDate.HasValue)
            {
                if (toDate.Value.Date > _cacheState.LatestRecordDate.Value.Date)
                {
                    return true;
                }
            }

            // If requested date range starts before cached data, fetch
            if (fromDate.HasValue && _cacheState.EarliestRecordDate.HasValue)
            {
                if (fromDate.Value.Date < _cacheState.EarliestRecordDate.Value.Date)
                {
                    return true;
                }
            }

            // If no date filter specified and cache is recent, no need to fetch
            if (!fromDate.HasValue && !toDate.HasValue)
            {
                // Still fetch if cache is older than 1 hour for "all data" requests
                return (DateTime.Now - _cacheState.LastSyncTime).TotalHours > 1;
            }

            return false;
        }

        /// <summary>
        /// Merge new data with cache and remove duplicates
        /// </summary>
        private void MergeAndDeduplicate(List<GLogData> newData)
        {
            // Create a set of existing keys for fast lookup
            var existingKeys = new HashSet<string>(
                _cachedData.Select(d => GetDeduplicationKey(d))
            );

            // Add only new unique records
            int duplicatesSkipped = 0;
            foreach (var record in newData)
            {
                string key = GetDeduplicationKey(record);
                if (!existingKeys.Contains(key))
                {
                    _cachedData.Add(record);
                    existingKeys.Add(key);
                }
                else
                {
                    duplicatesSkipped++;
                }
            }

            if (duplicatesSkipped > 0)
            {
                Logging.Write(Logging.WATCH, "AttendanceDataCache",
                    $"Skipped {duplicatesSkipped} duplicate records");
            }

            // Sort by time for efficient querying
            _cachedData = _cachedData.OrderBy(d => d.vYear)
                .ThenBy(d => d.vMonth)
                .ThenBy(d => d.vDay)
                .ThenBy(d => d.vHour)
                .ThenBy(d => d.vMinute)
                .ThenBy(d => d.vSecond)
                .ToList();
        }

        /// <summary>
        /// Generate deduplication key based on EnrollNumber and Time
        /// </summary>
        private string GetDeduplicationKey(GLogData data)
        {
            return $"{data.vEnrollNumber}_{data.Time}";
        }

        /// <summary>
        /// Filter cached data by date range
        /// </summary>
        private List<GLogData> FilterByDateRange(List<GLogData> data, DateTime? fromDate, DateTime? toDate)
        {
            if (!fromDate.HasValue && !toDate.HasValue)
            {
                return new List<GLogData>(data);
            }

            return data.Where(d =>
            {
                try
                {
                    var recordDate = new DateTime(d.vYear, d.vMonth, d.vDay, d.vHour, d.vMinute, d.vSecond & 0xFF);

                    bool passesFrom = !fromDate.HasValue || recordDate >= fromDate.Value;
                    bool passesTo = !toDate.HasValue || recordDate <= toDate.Value;

                    return passesFrom && passesTo;
                }
                catch
                {
                    return false;
                }
            }).ToList();
        }

        /// <summary>
        /// Update cache state metadata
        /// </summary>
        private void UpdateCacheState()
        {
            _cacheState.TotalRecordsInCache = _cachedData.Count;
            _cacheState.LastSyncTime = DateTime.Now;
            _cacheState.CachedAtTimestamp = DateTime.Now;

            if (_cachedData.Count > 0)
            {
                _cacheState.LastKnownRecordNo = _cachedData.Max(d => d.no);

                // Calculate date range
                try
                {
                    var dates = _cachedData.Select(d =>
                    {
                        try
                        {
                            return new DateTime(d.vYear, d.vMonth, d.vDay, d.vHour, d.vMinute, d.vSecond & 0xFF);
                        }
                        catch
                        {
                            return DateTime.MinValue;
                        }
                    }).Where(dt => dt != DateTime.MinValue).ToList();

                    if (dates.Count > 0)
                    {
                        _cacheState.EarliestRecordDate = dates.Min();
                        _cacheState.LatestRecordDate = dates.Max();
                    }
                }
                catch (Exception ex)
                {
                    Logging.Write(Logging.ERROR, "AttendanceDataCache", $"Error updating date range: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Save cache to file
        /// </summary>
        private void SaveCache()
        {
            try
            {
                // Backup before saving
                BackupCache();

                // Save data
                var json = JsonConvert.SerializeObject(_cachedData, Formatting.Indented);
                File.WriteAllText(_cacheFilePath, json);

                // Save state
                var stateJson = JsonConvert.SerializeObject(_cacheState, Formatting.Indented);
                File.WriteAllText(_stateFilePath, stateJson);

                Logging.Write(Logging.WATCH, "AttendanceDataCache", 
                    $"Cache saved: {_cachedData.Count} records");
            }
            catch (Exception ex)
            {
                Logging.Write(Logging.ERROR, "AttendanceDataCache", $"Failed to save cache: {ex.Message}");
            }
        }

        /// <summary>
        /// Load cache from file
        /// </summary>
        private void LoadCache()
        {
            try
            {
                if (File.Exists(_cacheFilePath) && File.Exists(_stateFilePath))
                {
                    // Load data
                    var json = File.ReadAllText(_cacheFilePath);
                    _cachedData = JsonConvert.DeserializeObject<List<GLogData>>(json) ?? new List<GLogData>();

                    // Load state
                    var stateJson = File.ReadAllText(_stateFilePath);
                    _cacheState = JsonConvert.DeserializeObject<CacheState>(stateJson) ?? new CacheState();

                    // Verify integrity
                    if (VerifyCacheIntegrity())
                    {
                        Logging.Write(Logging.WATCH, "AttendanceDataCache",
                            $"Cache loaded: {_cachedData.Count} records, Last sync: {_cacheState.LastSyncTime}");
                    }
                    else
                    {
                        Logging.Write(Logging.ERROR, "AttendanceDataCache", "Cache integrity check failed, clearing cache");
                        ClearCache();
                    }
                }
                else
                {
                    Logging.Write(Logging.WATCH, "AttendanceDataCache", "No existing cache found, starting fresh");
                }
            }
            catch (Exception ex)
            {
                Logging.Write(Logging.ERROR, "AttendanceDataCache", $"Failed to load cache: {ex.Message}");
                _cachedData = new List<GLogData>();
                _cacheState = new CacheState();
            }
        }

        /// <summary>
        /// Verify cache integrity
        /// </summary>
        private bool VerifyCacheIntegrity()
        {
            // Check if record count matches
            if (_cachedData.Count != _cacheState.TotalRecordsInCache)
            {
                Logging.Write(Logging.ERROR, "AttendanceDataCache",
                    $"Integrity check failed: Count mismatch (data: {_cachedData.Count}, state: {_cacheState.TotalRecordsInCache})");
                return false;
            }

            // Check for invalid records (only records from 2025 onwards are valid)
            int invalidCount = _cachedData.Count(d => d.vYear < 2025 || d.vYear > DateTime.Now.Year + 1);
            if (invalidCount > 0)
            {
                Logging.Write(Logging.ERROR, "AttendanceDataCache",
                    $"Integrity check warning: {invalidCount} records with invalid dates");
            }

            return true;
        }

        /// <summary>
        /// Create backup of cache
        /// </summary>
        public void BackupCache()
        {
            try
            {
                if (!File.Exists(_cacheFilePath))
                {
                    return;
                }

                var backupDir = Path.Combine(_cacheDirectory, "backup");
                if (!Directory.Exists(backupDir))
                {
                    Directory.CreateDirectory(backupDir);
                }

                var backupFileName = $"cache_machine_{_machineNumber}_{DateTime.Now:yyyyMMdd}.json";
                var backupFilePath = Path.Combine(backupDir, backupFileName);

                // Only create backup once per day
                if (!File.Exists(backupFilePath))
                {
                    File.Copy(_cacheFilePath, backupFilePath);
                    Logging.Write(Logging.WATCH, "AttendanceDataCache", $"Backup created: {backupFileName}");

                    // Clean old backups (keep last 7 days)
                    CleanOldBackups(backupDir, 7);
                }
            }
            catch (Exception ex)
            {
                Logging.Write(Logging.ERROR, "AttendanceDataCache", $"Failed to backup cache: {ex.Message}");
            }
        }

        /// <summary>
        /// Clean old backup files
        /// </summary>
        private void CleanOldBackups(string backupDir, int daysToKeep)
        {
            try
            {
                var files = Directory.GetFiles(backupDir, $"cache_machine_{_machineNumber}_*.json");
                var cutoffDate = DateTime.Now.AddDays(-daysToKeep);

                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.CreationTime < cutoffDate)
                    {
                        File.Delete(file);
                        Logging.Write(Logging.WATCH, "AttendanceDataCache", $"Deleted old backup: {fileInfo.Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Write(Logging.ERROR, "AttendanceDataCache", $"Failed to clean old backups: {ex.Message}");
            }
        }

        /// <summary>
        /// Clear cache
        /// </summary>
        public void ClearCache()
        {
            lock (_cacheLock)
            {
                _cachedData.Clear();
                _cacheState = new CacheState();

                try
                {
                    if (File.Exists(_cacheFilePath))
                    {
                        File.Delete(_cacheFilePath);
                    }
                    if (File.Exists(_stateFilePath))
                    {
                        File.Delete(_stateFilePath);
                    }

                    Logging.Write(Logging.WATCH, "AttendanceDataCache", "Cache cleared");
                }
                catch (Exception ex)
                {
                    Logging.Write(Logging.ERROR, "AttendanceDataCache", $"Failed to clear cache files: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Get cache statistics
        /// </summary>
        public string GetCacheStats()
        {
            lock (_cacheLock)
            {
                string earliestDate = _cacheState.EarliestRecordDate.HasValue 
                    ? _cacheState.EarliestRecordDate.Value.ToString("yyyy-MM-dd") 
                    : "N/A";
                string latestDate = _cacheState.LatestRecordDate.HasValue 
                    ? _cacheState.LatestRecordDate.Value.ToString("yyyy-MM-dd") 
                    : "N/A";

                return $"Machine {_machineNumber}: {_cacheState.TotalRecordsInCache} records, " +
                       $"Last sync: {_cacheState.LastSyncTime:yyyy-MM-dd HH:mm:ss}, " +
                       $"Date range: {earliestDate} to {latestDate}";
            }
        }
    }
}
