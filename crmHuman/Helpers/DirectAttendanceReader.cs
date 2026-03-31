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
            return _configuration.GetValue<bool>("AttendanceMachine:UseDirectSqlRealtime");
        }

        public string? GetConfigError()
        {
            if (!IsEnabled())
            {
                return null;
            }

            var connectionString = _configuration.GetValue<string>("AttendanceMachine:DirectSqlConnectionString");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return "Chưa cấu hình AttendanceMachine:DirectSqlConnectionString.";
            }

            try
            {
                using var connection = new SqlConnection(connectionString);
                connection.Open();
                return null;
            }
            catch (Exception ex)
            {
                return $"Không thể kết nối nguồn chấm công direct SQL: {ex.Message}";
            }
        }

        public List<AccessTableData> LoadTables(IEnumerable<AccessTableQuery> requestedTables, int maxRows)
        {
            var result = new List<AccessTableData>();
            var connectionString = _configuration.GetValue<string>("AttendanceMachine:DirectSqlConnectionString");
            var deviceIp = _configuration.GetValue<string>("AttendanceMachine:DeviceIp")?.Trim();

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                result.Add(new AccessTableData
                {
                    Name = "DirectSql",
                    Error = "Chưa cấu hình AttendanceMachine:DirectSqlConnectionString."
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
                            Error = "Nguồn direct SQL không hỗ trợ bảng này."
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
                Headers = new List<string> { "Mã vân tay", "Thời gian", "Máy", "Nguồn" }
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
            return tableData;
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
