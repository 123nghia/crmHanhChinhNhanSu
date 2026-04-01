using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

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

            using var command = connection.CreateCommand();
            command.CommandText = @"
SELECT TOP (@MaxRows)
    DeviceUserId AS UserEnrollNumber,
    [Name] AS UserFullName
FROM dbo.DeviceUsers
WHERE (@DeviceIp IS NULL OR DeviceIp = @DeviceIp)
ORDER BY [Name], DeviceUserId;";
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

            using var command = connection.CreateCommand();
            command.CommandText = @"
SELECT TOP (@MaxRows)
    DeviceUserId AS UserEnrollNumber,
    RecordTime AS TimeStr,
    DeviceIp AS MachineNo,
    [Source]
FROM dbo.DeviceAttendanceLogs
WHERE (@DeviceIp IS NULL OR DeviceIp = @DeviceIp)
ORDER BY RecordTime DESC;";
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

            using var command = connection.CreateCommand();
            command.CommandText = @"
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
  AND q.UserEnrollNumber <> ''
GROUP BY q.UserEnrollNumber
ORDER BY MAX(q.UserFullName), q.UserEnrollNumber;";
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

            using var command = connection.CreateCommand();
            command.CommandText = @"
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
WHERE (@DeviceIp IS NULL OR l.DeviceIp = @DeviceIp)
ORDER BY l.RecordTime DESC;";
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
            if (!string.IsNullOrWhiteSpace(requested.FilterColumn) &&
                !string.IsNullOrWhiteSpace(requested.FilterValue))
            {
                var filterIndex = tableData.Columns.FindIndex(c => c.Equals(requested.FilterColumn, StringComparison.OrdinalIgnoreCase));
                if (filterIndex >= 0)
                {
                    var filterValue = requested.FilterValue.Trim();
                    filtered = filtered
                        .Where(row => filterIndex < row.Count &&
                                      string.Equals(row[filterIndex], filterValue, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }
            }

            tableData.TotalCount = filtered.Count;
            var pageSize = requested.PageSize <= 0 ? 50 : requested.PageSize;
            var page = requested.Page <= 0 ? 1 : requested.Page;
            var skip = (page - 1) * pageSize;
            tableData.Page = page;
            tableData.PageSize = pageSize;
            tableData.Rows = filtered.Skip(skip).Take(pageSize).ToList();
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
