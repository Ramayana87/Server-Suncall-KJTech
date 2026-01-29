# Mockup Operations Documentation

## Overview
This document describes the new mockup operations added to the socket server that allow testing with mockup data instead of connecting to physical biometric devices.

## New Operations

### MOCKUP_GETLOGS
Returns attendance log data from mockup files instead of reading from biometric devices.

**Request Format:**
```
MOCKUP_GETLOGS|machineNumber|dummy_ip|dummy_port|fromDate|toDate
```

**Parameters:**
- `machineNumber`: Machine number (used to determine which mockup file to read: "may {machineNumber}.txt")
- `dummy_ip`: Not used (can be any value like "0.0.0.0")
- `dummy_port`: Not used (can be any value like "0")
- `fromDate`: (Optional) Filter logs from this date (format: "yyyy-MM-dd HH:mm:ss")
- `toDate`: (Optional) Filter logs to this date (format: "yyyy-MM-dd HH:mm:ss")

**Example:**
```
MOCKUP_GETLOGS|5|0.0.0.0|0|2024-09-01 00:00:00|2024-09-30 23:59:59
```

**Response:**
JSON array of GLogData objects, same format as GETLOGS operation.

### MOCKUP_GETUSERS
Returns distinct users from mockup files instead of reading from biometric devices.

**Request Format:**
```
MOCKUP_GETUSERS|machineNumber|dummy_ip|dummy_port
```

**Parameters:**
- `machineNumber`: Machine number (used to determine which mockup file to read: "may {machineNumber}.txt")
- `dummy_ip`: Not used (can be any value like "0.0.0.0")
- `dummy_port`: Not used (can be any value like "0")

**Example:**
```
MOCKUP_GETUSERS|5|0.0.0.0|0
```

**Response:**
JSON array of distinct GLogData objects (one per unique user), same format as GETUSERS operation.

## Mockup Data Files

The mockup operations read data from text files located in the "data mockup" folder at the root of the project.

**File Naming Convention:**
```
may {machineNumber}.txt
```

**Available Files:**
- may 5.txt (14 MB, ~189,000 records)
- may 6.txt (12 MB, ~170,000 records)
- may 7.txt (9.4 MB, ~133,000 records)
- may 8.txt (13 MB, ~177,000 records)

**Important:** Only machine numbers 5, 6, 7, and 8 have mockup data available. Requesting other machine numbers will return an empty result.

**File Format:**
Tab-separated values with the following columns:
```
[empty]	no	result	id	method	doormode	function	verification	time	captured
```

Example line:
```
	1	Granted	00001763	Finger	Close	NONE	by FP	2024-09-18 09:11:51	False
```

## TestClientForm New Feature

A new button "Test Server with Mockup Data" has been added to the TestClientForm in the "Mockup Data Test" group.

**Usage:**
1. Start the server
2. Open TestClientForm (Tools > Launch Test Client)
3. Enter Server IP and Port
4. Enter Machine Number (5, 6, 7, or 8 for available mockup files)
5. Optionally enable date range filter
6. Click "Test Server with Mockup Data"

The button will:
- Send a MOCKUP_GETLOGS request to the server
- Display the number of records received
- Save the response to a JSON file in the Log folder
- Show timing information

## Implementation Details

### Functions Added to Form1.cs

1. **GetMockupAttendanceData(int machineNumber, DateTime? fromDate, DateTime? toDate)**
   - Reads attendance logs from mockup file
   - Applies date filtering if specified
   - Returns filtered list of GLogData objects

2. **GetMockupDistinctUsers(int machineNumber)**
   - Reads distinct users from mockup file
   - Returns list of unique users (one GLogData per user)

### Key Features

- **Automatic file discovery**: The functions try multiple locations to find the "data mockup" folder
- **Date filtering**: Supports optional date range filtering just like the real operations
- **Error handling**: Gracefully handles missing files or folders with appropriate logging
- **Performance logging**: Tracks and reports execution time for each operation
- **Data validation**: Filters out invalid records (invalid dates, denied access, etc.)

## Benefits

1. **Testing without hardware**: Can test the server and client without physical biometric devices
2. **Repeatable tests**: Same mockup data can be used for consistent testing
3. **Development efficiency**: Faster development and debugging cycles
4. **Safe testing**: No risk of corrupting real device data during development

## Notes

- Mockup operations have the same response format as their real counterparts (GETLOGS, GETUSERS)
- The IP and port parameters in mockup operations are ignored but required for protocol consistency
- All existing operations continue to work as before (backward compatible)
