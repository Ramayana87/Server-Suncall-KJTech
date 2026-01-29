using System;

namespace Server
{
    /// <summary>
    /// Model class for storing cache metadata
    /// </summary>
    public class CacheState
    {
        /// <summary>
        /// Last known record number in cache
        /// </summary>
        public int LastKnownRecordNo { get; set; }

        /// <summary>
        /// Last synchronization time
        /// </summary>
        public DateTime LastSyncTime { get; set; }

        /// <summary>
        /// Total number of records currently in cache
        /// </summary>
        public int TotalRecordsInCache { get; set; }

        /// <summary>
        /// Timestamp when cache was last saved
        /// </summary>
        public DateTime CachedAtTimestamp { get; set; }

        /// <summary>
        /// Latest date of records in cache
        /// </summary>
        public DateTime? LatestRecordDate { get; set; }

        /// <summary>
        /// Earliest date of records in cache
        /// </summary>
        public DateTime? EarliestRecordDate { get; set; }

        public CacheState()
        {
            LastKnownRecordNo = 0;
            LastSyncTime = DateTime.MinValue;
            TotalRecordsInCache = 0;
            CachedAtTimestamp = DateTime.MinValue;
            LatestRecordDate = null;
            EarliestRecordDate = null;
        }
    }
}
