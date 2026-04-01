using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace crmHuman.Helpers
{
    public sealed class DirectAttendanceReader
    {
        private readonly IConfiguration _configuration;

        public DirectAttendanceReader(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public bool IsEnabled()
        {
            return IsDirectSqlEnabled() || IsDeviceCacheEnabled();
        }

        public string? GetConfigError()
        {
            if (IsDirectSqlEnabled())
            {
                var connectionString = _configuration.GetValue<string>("AttendanceMachine:DirectSqlConnectionString");
                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    return "Chua cau hinh AttendanceMachine:DirectSqlConnectionString.";
                }

                try
                {
                    using var connection = new SqlConnection(connectionString);
                    connection.Open();
                    return null;
                }
                catch (Exception ex)
                {
                    return $"Khong the ket noi nguon cham cong direct SQL: {ex.Message}";
                }
            }

            if (IsDeviceCacheEnabled())
            {
                var connectionString = GetMainConnectionString();
                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    return "Chua cau hinh ConnectionStrings:stringConnect7.";
                }

                try
                {
                    using var connection = new SqlConnection(connectionString);
                    connection.Open();

                    using var command = connection.CreateCommand();
                    command.CommandText = @"
SELECT CASE WHEN OBJECT_ID(N'dbo.AttendanceDeviceLogs', N'U') IS NULL THEN 0 ELSE 1 END;";
                    var exists = Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
                    return exists == 1
                        ? null
                        : "Chua co bang dbo.AttendanceDeviceLogs. Can chay migration moi nhat.";
                }
                catch (Exception ex)
                {
                    return $"Khong the doc cache lich su quet the: {ex.Message}";
                }
            }

            return null;
        }

        public List<AccessTableData> LoadTables(IEnumerable<AccessTableQuery> requestedTables, int maxRows)
        {
            return IsDeviceCacheEnabled()
                ? LoadDeviceCacheTables(requestedTables, maxRows)
                : LoadDirectSqlTables(requestedTables, maxRows);
        }

        public AttendanceSyncStatus GetSyncStatus()
        {
            var status = new AttendanceSyncStatus();
            var lookbackDays = Math.Max(_configuration.GetValue<int>("AttendanceMachine:RealtimeSyncLookbackDays"), 1);

            try
            {
                if (IsDeviceCacheEnabled())
                {
                    status.ModeText = "TCP/IP truc tiep tu may cham cong";
                    LoadDeviceCacheSyncStatus(status, lookbackDays);
                }
                else if (IsDirectSqlEnabled())
                {
                    status.ModeText = "Direct SQL";
                    LoadDirectSqlSyncStatus(status, lookbackDays);
                }
                else
                {
                    status.ModeText = "Chua bat dong bo";
                }
            }
            catch (Exception ex)
            {
                status.ErrorMessage = ex.Message;
            }

            if (!status.LastSyncedAt.HasValue)
            {
                status.LastSyncedAt = status.AttendanceLastUpdatedAt;
            }

            return status;
        }

        private List<AccessTableData> LoadDirectSqlTables(IEnumerable<AccessTableQuery> requestedTables, int maxRows)
        {
            var result = new List<AccessTableData>();
            var connectionString = _configuration.GetValue<string>("AttendanceMachine:DirectSqlConnectionString");
            var deviceIp = _configuration.GetValue<string>("AttendanceMachine:DeviceIp")?.Trim();

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                result.Add(new AccessTableData
                {
                    Name = "DirectSql",
                    Error = "Chua cau hinh AttendanceMachine:DirectSqlConnectionString."
                });
                return result;
            }

            using var connection = new SqlConnection(connectionString);
            connection.Open();

            foreach (var requested in requestedTables)
            {
                AccessTableData tableData = requested.Name.Equals("UserInfo", StringComparison.OrdinalIgnoreCase)
                    ? LoadUsers(connection, requested, deviceIp, maxRows)
                    : requested.Name.Equals("CheckInOut", StringComparison.OrdinalIgnoreCase)
                        ? LoadHistory(connection, requested, deviceIp, maxRows)
                        : new AccessTableData
                        {
                            Name = requested.Name,
                            Error = "Nguon direct SQL khong ho tro bang nay."
                        };

                if (ShouldFallbackToAllDevices(requested, tableData, deviceIp))
                {
                    tableData = requested.Name.Equals("UserInfo", StringComparison.OrdinalIgnoreCase)
                        ? LoadUsers(connection, requested, null, maxRows)
                        : requested.Name.Equals("CheckInOut", StringComparison.OrdinalIgnoreCase)
                            ? LoadHistory(connection, requested, null, maxRows)
                            : tableData;
                }

                result.Add(tableData);
            }

            return result;
        }

        private List<AccessTableData> LoadDeviceCacheTables(IEnumerable<AccessTableQuery> requestedTables, int maxRows)
        {
            var result = new List<AccessTableData>();
            var connectionString = GetMainConnectionString();
            var deviceIp = _configuration.GetValue<string>("AttendanceMachine:DeviceIp")?.Trim();

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                result.Add(new AccessTableData
                {
                    Name = "AttendanceDeviceLogs",
                    Error = "Chua cau hinh ConnectionStrings:stringConnect7."
                });
                return result;
            }

            using var connection = new SqlConnection(connectionString);
            connection.Open();

            foreach (var requested in requestedTables)
            {
                AccessTableData tableData = requested.Name.Equals("UserInfo", StringComparison.OrdinalIgnoreCase)
                    ? LoadDeviceCacheUsers(connection, requested, deviceIp, maxRows)
                    : requested.Name.Equals("CheckInOut", StringComparison.OrdinalIgnoreCase)
                        ? LoadDeviceCacheHistory(connection, requested, deviceIp, maxRows)
                        : new AccessTableData
                        {
                            Name = requested.Name,
                            Error = "Cache raw log tu may cham cong khong ho tro bang nay."
                        };

                if (ShouldFallbackToAllDevices(requested, tableData, deviceIp))
                {
                    tableData = requested.Name.Equals("UserInfo", StringComparison.OrdinalIgnoreCase)
                        ? LoadDeviceCacheUsers(connection, requested, null, maxRows)
                        : requested.Name.Equals("CheckInOut", StringComparison.OrdinalIgnoreCase)
                            ? LoadDeviceCacheHistory(connection, requested, null, maxRows)
                            : tableData;
                }

                result.Add(tableData);
            }

            return result;
        }

        private static AccessTableData LoadUsers(SqlConnection connection, AccessTableQuery requested, string? deviceIp, int maxRows)
        {
            var tableData = new AccessTableData
            {
                Name = requested.Name,
                Page = requested.Page,
                PageSize = requested.PageSize,
                Columns = new List<string> { "UserEnrollNumber", "UserFullName" },
                Headers = new List<string> { "UserEnrollNumber", "UserFullName" }
            };

            var filterValues = GetRequestedFilterValues(requested);
            using var command = connection.CreateCommand();
            var sql = new StringBuilder(@"
SELECT TOP (@MaxRows)
    DeviceUserId AS UserEnrollNumber,
    [Name] AS UserFullName
FROM dbo.DeviceUsers
WHERE (@DeviceIp IS NULL OR DeviceIp = @DeviceIp)");
            AppendInClause(sql, command, "DeviceUserId", filterValues, "userEnroll");
            sql.Append(@"
ORDER BY [Name], DeviceUserId;");
            command.CommandText = sql.ToString();
            command.Parameters.AddWithValue("@MaxRows", maxRows);
            command.Parameters.AddWithValue("@DeviceIp", (object?)deviceIp ?? DBNull.Value);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                tableData.Rows.Add(new List<string>
                {
                    reader["UserEnrollNumber"]?.ToString() ?? string.Empty,
                    reader["UserFullName"]?.ToString() ?? string.Empty
                });
            }

            tableData.TotalCount = tableData.Rows.Count;
            return tableData;
        }

        private static AccessTableData LoadHistory(SqlConnection connection, AccessTableQuery requested, string? deviceIp, int maxRows)
        {
            var tableData = new AccessTableData
            {
                Name = requested.Name,
                Page = requested.Page,
                PageSize = requested.PageSize,
                Columns = new List<string> { "UserEnrollNumber", "TimeStr", "MachineNo", "Source" },
                Headers = new List<string> { "Ma van tay", "Thoi gian", "May", "Nguon" }
            };

            var filterValues = GetRequestedFilterValues(requested);
            using var command = connection.CreateCommand();
            var sql = new StringBuilder(@"
SELECT TOP (@MaxRows)
    DeviceUserId AS UserEnrollNumber,
    RecordTime AS TimeStr,
    DeviceIp AS MachineNo,
    [Source]
FROM dbo.DeviceAttendanceLogs
WHERE (@DeviceIp IS NULL OR DeviceIp = @DeviceIp)");
            AppendInClause(sql, command, "DeviceUserId", filterValues, "historyEnroll");
            sql.Append(@"
ORDER BY RecordTime DESC;");
            command.CommandText = sql.ToString();
            command.Parameters.AddWithValue("@MaxRows", maxRows);
            command.Parameters.AddWithValue("@DeviceIp", (object?)deviceIp ?? DBNull.Value);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                tableData.Rows.Add(new List<string>
                {
                    reader["UserEnrollNumber"]?.ToString() ?? string.Empty,
                    FormatValue(reader["TimeStr"]),
                    reader["MachineNo"]?.ToString() ?? string.Empty,
                    reader["Source"]?.ToString() ?? string.Empty
                });
            }

            ApplyFilterAndPaging(tableData, requested);
            return tableData;
        }

        private static AccessTableData LoadDeviceCacheUsers(SqlConnection connection, AccessTableQuery requested, string? deviceIp, int maxRows)
        {
            var tableData = new AccessTableData
            {
                Name = requested.Name,
                Page = requested.Page,
                PageSize = requested.PageSize,
                Columns = new List<string> { "UserEnrollNumber", "UserFullName" },
                Headers = new List<string> { "UserEnrollNumber", "UserFullName" }
            };

            var filterValues = GetRequestedFilterValues(requested);
            using var command = connection.CreateCommand();
            var sql = new StringBuilder(@"
SELECT TOP (@MaxRows)
    q.UserEnrollNumber,
    MAX(q.UserFullName) AS UserFullName
FROM
(
    SELECT
        COALESCE(NULLIF(e.FingerprintCode, ''), NULLIF(l.NormalizedUserId, ''), l.DeviceUserId) AS UserEnrollNumber,
        COALESCE(NULLIF(e.FullName, ''), COALESCE(NULLIF(l.NormalizedUserId, ''), l.DeviceUserId)) AS UserFullName
    FROM dbo.AttendanceDeviceLogs l
    OUTER APPLY
    (
        SELECT TOP (1)
            emp.FingerprintCode,
            emp.FullName
        FROM dbo.Employees emp
        WHERE ISNULL(emp.Deleted, 0) = 0
          AND (emp.FingerprintCode = l.DeviceUserId OR emp.FingerprintCode = l.NormalizedUserId)
        ORDER BY CASE WHEN emp.FingerprintCode = l.DeviceUserId THEN 0 ELSE 1 END, emp.Id
    ) e
    WHERE (@DeviceIp IS NULL OR l.DeviceIp = @DeviceIp)
) q
WHERE q.UserEnrollNumber IS NOT NULL
  AND q.UserEnrollNumber <> ''");
            AppendInClause(sql, command, "q.UserEnrollNumber", filterValues, "cacheUserEnroll");
            sql.Append(@"
GROUP BY q.UserEnrollNumber
ORDER BY MAX(q.UserFullName), q.UserEnrollNumber;");
            command.CommandText = sql.ToString();
            command.Parameters.AddWithValue("@MaxRows", maxRows);
            command.Parameters.AddWithValue("@DeviceIp", (object?)deviceIp ?? DBNull.Value);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                tableData.Rows.Add(new List<string>
                {
                    reader["UserEnrollNumber"]?.ToString() ?? string.Empty,
                    reader["UserFullName"]?.ToString() ?? string.Empty
                });
            }

            tableData.TotalCount = tableData.Rows.Count;
            return tableData;
        }

        private static AccessTableData LoadDeviceCacheHistory(SqlConnection connection, AccessTableQuery requested, string? deviceIp, int maxRows)
        {
            var tableData = new AccessTableData
            {
                Name = requested.Name,
                Page = requested.Page,
                PageSize = requested.PageSize,
                Columns = new List<string> { "UserEnrollNumber", "TimeStr", "InOutMode", "MachineNo", "Source" },
                Headers = new List<string> { "Ma van tay", "Thoi gian", "Vao / Ra", "May", "Nguon" }
            };

            var filterValues = GetRequestedFilterValues(requested);
            using var command = connection.CreateCommand();
            var sql = new StringBuilder(@"
SELECT TOP (@MaxRows)
    COALESCE(NULLIF(e.FingerprintCode, ''), NULLIF(l.NormalizedUserId, ''), l.DeviceUserId) AS UserEnrollNumber,
    l.RecordTime AS TimeStr,
    l.InOutMode,
    l.DeviceIp AS MachineNo,
    l.Source
FROM dbo.AttendanceDeviceLogs l
OUTER APPLY
(
    SELECT TOP (1)
        emp.FingerprintCode
    FROM dbo.Employees emp
    WHERE ISNULL(emp.Deleted, 0) = 0
      AND (emp.FingerprintCode = l.DeviceUserId OR emp.FingerprintCode = l.NormalizedUserId)
    ORDER BY CASE WHEN emp.FingerprintCode = l.DeviceUserId THEN 0 ELSE 1 END, emp.Id
) e
WHERE (@DeviceIp IS NULL OR l.DeviceIp = @DeviceIp)");
            AppendInClause(
                sql,
                command,
                "COALESCE(NULLIF(e.FingerprintCode, ''), NULLIF(l.NormalizedUserId, ''), l.DeviceUserId)",
                filterValues,
                "cacheHistoryEnroll");
            sql.Append(@"
ORDER BY l.RecordTime DESC;");
            command.CommandText = sql.ToString();
            command.Parameters.AddWithValue("@MaxRows", maxRows);
            command.Parameters.AddWithValue("@DeviceIp", (object?)deviceIp ?? DBNull.Value);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                tableData.Rows.Add(new List<string>
                {
                    reader["UserEnrollNumber"]?.ToString() ?? string.Empty,
                    FormatValue(reader["TimeStr"]),
                    reader["InOutMode"]?.ToString() ?? string.Empty,
                    reader["MachineNo"]?.ToString() ?? string.Empty,
                    reader["Source"]?.ToString() ?? string.Empty
                });
            }

            ApplyFilterAndPaging(tableData, requested);
            return tableData;
        }

        private static void ApplyFilterAndPaging(AccessTableData tableData, AccessTableQuery requested)
        {
            var filtered = FilterRows(tableData.Rows, requested.SearchToken);
            filtered = ApplyColumnFilter(filtered, tableData.Columns, requested);

            tableData.TotalCount = filtered.Count;
            var pageSize = requested.PageSize <= 0 ? 50 : requested.PageSize;
            var page = requested.Page <= 0 ? 1 : requested.Page;
            var skip = (page - 1) * pageSize;
            tableData.Page = page;
            tableData.PageSize = pageSize;
            tableData.Rows = filtered.Skip(skip).Take(pageSize).ToList();
        }

        private static List<List<string>> ApplyColumnFilter(
            List<List<string>> rows,
            List<string> columns,
            AccessTableQuery requested)
        {
            if (rows.Count == 0 || string.IsNullOrWhiteSpace(requested.FilterColumn))
            {
                return rows;
            }

            var filterIndex = columns.FindIndex(c => c.Equals(requested.FilterColumn, StringComparison.OrdinalIgnoreCase));
            if (filterIndex < 0)
            {
                return rows;
            }

            var filterValues = GetRequestedFilterValues(requested);
            if (filterValues.Count == 0)
            {
                return rows;
            }

            var allowedValues = new HashSet<string>(filterValues, StringComparer.OrdinalIgnoreCase);
            return rows
                .Where(row => filterIndex < row.Count && allowedValues.Contains(row[filterIndex]))
                .ToList();
        }

        private static List<string> GetRequestedFilterValues(AccessTableQuery requested)
        {
            var values = requested.FilterValues?
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList() ?? new List<string>();

            if (values.Count == 0 && !string.IsNullOrWhiteSpace(requested.FilterValue))
            {
                values.Add(requested.FilterValue.Trim());
            }

            return values;
        }

        private static void AppendInClause(
            StringBuilder sql,
            SqlCommand command,
            string sqlExpression,
            IReadOnlyList<string> filterValues,
            string parameterPrefix)
        {
            if (filterValues == null || filterValues.Count == 0)
            {
                return;
            }

            sql.Append(" AND ").Append(sqlExpression).Append(" IN (");
            for (var i = 0; i < filterValues.Count; i++)
            {
                var parameterName = $"@{parameterPrefix}{i}";
                if (i > 0)
                {
                    sql.Append(", ");
                }

                sql.Append(parameterName);
                command.Parameters.AddWithValue(parameterName, filterValues[i]);
            }

            sql.Append(')');
        }

        private string? GetMainConnectionString()
        {
            var connectionString = _configuration.GetConnectionString("stringConnect7");
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                return connectionString.Trim();
            }

            connectionString = _configuration.GetConnectionString("stringConnect");
            return string.IsNullOrWhiteSpace(connectionString) ? null : connectionString.Trim();
        }

        private bool IsDirectSqlEnabled()
        {
            return _configuration.GetValue<bool>("AttendanceMachine:UseDirectSqlRealtime");
        }

        private bool IsDeviceCacheEnabled()
        {
            return _configuration.GetValue<bool>("AttendanceMachine:UseDirectDeviceRealtime");
        }

        private void LoadDeviceCacheSyncStatus(AttendanceSyncStatus status, int lookbackDays)
        {
            var connectionString = GetMainConnectionString();
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                status.ErrorMessage = "Chua cau hinh ConnectionStrings:stringConnect7.";
                return;
            }

            var deviceIp = _configuration.GetValue<string>("AttendanceMachine:DeviceIp")?.Trim();
            var fromDate = DateTime.Today.AddDays(1 - lookbackDays);

            using var connection = new SqlConnection(connectionString);
            connection.Open();

            LoadDeviceCacheSyncStatusCore(status, connection, deviceIp, fromDate);
            if (!status.LatestLogTime.HasValue && !string.IsNullOrWhiteSpace(deviceIp))
            {
                LoadDeviceCacheSyncStatusCore(status, connection, null, fromDate);
            }

            LoadAttendanceRecordStatus(status, connectionString, fromDate);
        }

        private void LoadDirectSqlSyncStatus(AttendanceSyncStatus status, int lookbackDays)
        {
            var connectionString = _configuration.GetValue<string>("AttendanceMachine:DirectSqlConnectionString");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                status.ErrorMessage = "Chua cau hinh AttendanceMachine:DirectSqlConnectionString.";
                return;
            }

            var deviceIp = _configuration.GetValue<string>("AttendanceMachine:DeviceIp")?.Trim();
            var fromDate = DateTime.Today.AddDays(1 - lookbackDays);

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                LoadDirectSqlSyncStatusCore(status, connection, deviceIp, fromDate);
                if (!status.LatestLogTime.HasValue && !string.IsNullOrWhiteSpace(deviceIp))
                {
                    LoadDirectSqlSyncStatusCore(status, connection, null, fromDate);
                }
            }

            LoadAttendanceRecordStatus(status, GetMainConnectionString(), fromDate);
        }

        private void LoadAttendanceRecordStatus(
            AttendanceSyncStatus status,
            string? connectionString,
            DateTime fromDate)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return;
            }

            using var connection = new SqlConnection(connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
SELECT
    MAX(a.UpdateAt) AS AttendanceLastUpdatedAt,
    MAX(CAST(a.WorkDate AS datetime)) AS AttendanceThroughDate
FROM dbo.AttendanceRecords a
WHERE a.WorkDate >= @FromDate;";
            command.Parameters.AddWithValue("@FromDate", fromDate.Date);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                status.AttendanceLastUpdatedAt = ReadNullableDateTime(reader, "AttendanceLastUpdatedAt");
                status.AttendanceThroughDate = ReadNullableDateTime(reader, "AttendanceThroughDate");
            }
        }

        private static DateTime? ReadNullableDateTime(SqlDataReader reader, string columnName)
        {
            var value = reader[columnName];
            if (value == null || value == DBNull.Value)
            {
                return null;
            }

            return value is DateTime dateTime
                ? dateTime
                : Convert.ToDateTime(value, CultureInfo.InvariantCulture);
        }

        private static bool ShouldFallbackToAllDevices(
            AccessTableQuery requested,
            AccessTableData tableData,
            string? deviceIp)
        {
            if (string.IsNullOrWhiteSpace(deviceIp))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(tableData.Error))
            {
                return false;
            }

            if (!requested.Name.Equals("UserInfo", StringComparison.OrdinalIgnoreCase) &&
                !requested.Name.Equals("CheckInOut", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return tableData.TotalCount <= 0 && tableData.Rows.Count == 0;
        }

        private static void LoadDeviceCacheSyncStatusCore(
            AttendanceSyncStatus status,
            SqlConnection connection,
            string? deviceIp,
            DateTime fromDate)
        {
            using var command = connection.CreateCommand();
            command.CommandText = @"
SELECT
    MAX(l.RecordTime) AS LatestLogTime,
    MAX(l.CreateAt) AS LastSyncedAt
FROM dbo.AttendanceDeviceLogs l
WHERE (@DeviceIp IS NULL OR l.DeviceIp = @DeviceIp)
  AND l.RecordTime >= @FromDate;";
            command.Parameters.AddWithValue("@DeviceIp", (object?)deviceIp ?? DBNull.Value);
            command.Parameters.AddWithValue("@FromDate", fromDate);

            using var reader = command.ExecuteReader();
            if (!reader.Read())
            {
                return;
            }

            status.LatestLogTime = ReadNullableDateTime(reader, "LatestLogTime");
            status.LastSyncedAt = ReadNullableDateTime(reader, "LastSyncedAt");
        }

        private static void LoadDirectSqlSyncStatusCore(
            AttendanceSyncStatus status,
            SqlConnection connection,
            string? deviceIp,
            DateTime fromDate)
        {
            using var command = connection.CreateCommand();
            command.CommandText = @"
SELECT
    MAX(l.RecordTime) AS LatestLogTime
FROM dbo.DeviceAttendanceLogs l
WHERE (@DeviceIp IS NULL OR l.DeviceIp = @DeviceIp)
  AND l.RecordTime >= @FromDate;";
            command.Parameters.AddWithValue("@DeviceIp", (object?)deviceIp ?? DBNull.Value);
            command.Parameters.AddWithValue("@FromDate", fromDate);

            using var reader = command.ExecuteReader();
            if (!reader.Read())
            {
                return;
            }

            status.LatestLogTime = ReadNullableDateTime(reader, "LatestLogTime");
        }

        private static string FormatValue(object? value)
        {
            if (value == null || value == DBNull.Value)
            {
                return string.Empty;
            }

            return value switch
            {
                DateTime dateTime => dateTime.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture),
                _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty
            };
        }

        private static List<List<string>> FilterRows(List<List<string>> rows, string? searchToken)
        {
            if (string.IsNullOrWhiteSpace(searchToken))
            {
                return rows;
            }

            var token = searchToken.Trim();
            return rows
                .Where(row => row.Any(cell =>
                    !string.IsNullOrWhiteSpace(cell) &&
                    cell.Contains(token, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }
    }
}
