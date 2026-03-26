using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using VS.Human.Utility;

namespace crmHuman.Helpers
{
    public sealed class AccessTableData
    {
        public string Name { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
        public List<string> Columns { get; set; } = new List<string>();
        public List<string> Headers { get; set; } = new List<string>();
        public List<List<string>> Rows { get; set; } = new List<List<string>>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public string? Error { get; set; }
    }

    public sealed class AccessTableQuery
    {
        public string Name { get; set; } = string.Empty;
        public List<string> Columns { get; set; } = new List<string>();
        public Dictionary<string, string> ColumnLabels { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public string? SearchToken { get; set; }
        public string? FilterColumn { get; set; }
        public string? FilterValue { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    public sealed class AccessAttendanceReader
    {
        private readonly IConfiguration _configuration;

        public AccessAttendanceReader(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string? GetConfigError()
        {
            AttendanceMachinePathResolution? source;
            try
            {
                source = ResolveAccessSource();
            }
            catch (Exception ex)
            {
                return ex.Message;
            }

            var dbPath = source?.LocalPath;
            if (string.IsNullOrWhiteSpace(dbPath))
            {
                return "Ch\u01B0a c\u1EA5u h\u00ECnh \u0111\u01B0\u1EDDng d\u1EABn file ch\u1EA5m c\u00F4ng.";
            }

            if (!File.Exists(dbPath))
            {
                return $"Kh\u00F4ng t\u00ECm th\u1EA5y file ch\u1EA5m c\u00F4ng: {dbPath}";
            }

            return null;
        }

        [SupportedOSPlatform("windows")]
        public List<AccessTableData> LoadTables(IEnumerable<AccessTableQuery> requestedTables, int maxRows)
        {
            var result = new List<AccessTableData>();

            if (!OperatingSystem.IsWindows())
            {
                result.Add(new AccessTableData
                {
                    Name = "Access",
                    Headers = new List<string>(),
                    Error = "Ch\u1EC9 h\u1ED7 tr\u1EE3 \u0111\u1ECDc d\u1EEF li\u1EC7u Access tr\u00EAn Windows."
                });
                return result;
            }

            AttendanceMachinePathResolution? source;
            try
            {
                source = ResolveAccessSource();
            }
            catch (Exception ex)
            {
                result.Add(new AccessTableData
                {
                    Name = "Access",
                    Error = ex.Message
                });
                return result;
            }

            var dbPath = source?.LocalPath ?? string.Empty;
            if (string.IsNullOrWhiteSpace(dbPath))
            {
                result.Add(new AccessTableData
                {
                    Name = "Access",
                    Error = "Ch\u01B0a c\u1EA5u h\u00ECnh \u0111\u01B0\u1EDDng d\u1EABn file ch\u1EA5m c\u00F4ng."
                });
                return result;
            }

            var password = _configuration.GetValue<string>("AttendanceMachine:Password");
            var provider = _configuration.GetValue<string>("AttendanceMachine:Provider");
            var providers = new List<string>();

            if (!string.IsNullOrWhiteSpace(provider))
            {
                providers.Add(provider);
            }

            providers.Add("Microsoft.ACE.OLEDB.16.0");
            providers.Add("Microsoft.ACE.OLEDB.12.0");
            providers.Add("Microsoft.Jet.OLEDB.4.0");

            OleDbConnection? connection = null;
            string? lastError = null;

            foreach (var item in providers.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                try
                {
                    var builder = new OleDbConnectionStringBuilder
                    {
                        Provider = item,
                        DataSource = dbPath
                    };

                    if (!string.IsNullOrWhiteSpace(password))
                    {
                        builder["Jet OLEDB:Database Password"] = password;
                    }

                    builder["Persist Security Info"] = false;
                    var conn = new OleDbConnection(builder.ConnectionString);
                    conn.Open();
                    connection = conn;
                    break;
                }
                catch (Exception ex)
                {
                    lastError = ex.Message;
                }
            }

            if (connection == null)
            {
                result.Add(new AccessTableData
                {
                    Name = "Access",
                    Error = $"Kh\u00F4ng th\u1EC3 m\u1EDF file ch\u1EA5m c\u00F4ng: {lastError}"
                });
                return result;
            }

            using (connection)
            {
                var existingTables = GetTableNames(connection);
                var tableMap = existingTables.ToDictionary(t => t, t => t, StringComparer.OrdinalIgnoreCase);

                foreach (var requested in requestedTables)
                {
                    var tableData = new AccessTableData
                    {
                        Name = requested.Name,
                        Page = requested.Page,
                        PageSize = requested.PageSize
                    };

                    if (!tableMap.TryGetValue(requested.Name, out var tableName))
                    {
                        tableData.Error = "Kh\u00F4ng t\u00ECm th\u1EA5y b\u1EA3ng.";
                        result.Add(tableData);
                        continue;
                    }

                    var availableColumns = GetColumnNames(connection, tableName);
                    if (availableColumns.Count == 0)
                    {
                        tableData.Error = "Kh\u00F4ng c\u00F3 c\u1ED9t d\u1EEF li\u1EC7u.";
                        result.Add(tableData);
                        continue;
                    }

                    var selectedColumns = ResolveColumns(availableColumns, requested.Columns);
                    if (selectedColumns.Count == 0)
                    {
                        tableData.Error = "Kh\u00F4ng c\u00F3 c\u1ED9t ph\u00F9 h\u1EE3p.";
                        result.Add(tableData);
                        continue;
                    }

                    tableData.Columns = selectedColumns;
                    tableData.Headers = selectedColumns
                        .Select(col => requested.ColumnLabels.TryGetValue(col, out var label) ? label : col)
                        .ToList();

                    var selectColumns = string.Join(", ", selectedColumns.Select(c => $"[{c}]"));
                    var query = $"SELECT TOP {maxRows} {selectColumns} FROM [{tableName}]";

                    try
                    {
                        using var command = new OleDbCommand(query, connection);
                        using var reader = command.ExecuteReader();
                        if (reader == null)
                        {
                            tableData.Error = "Kh\u00F4ng \u0111\u1ECDc \u0111\u01B0\u1EE3c d\u1EEF li\u1EC7u.";
                            result.Add(tableData);
                            continue;
                        }

                        while (reader.Read())
                        {
                            var row = new List<string>();
                            for (var i = 0; i < selectedColumns.Count; i++)
                            {
                                row.Add(FormatValue(reader.GetValue(i)));
                            }
                            tableData.Rows.Add(row);
                        }
                    }
                    catch (Exception ex)
                    {
                        tableData.Error = ex.Message;
                    }

                    if (string.IsNullOrWhiteSpace(tableData.Error))
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

                    result.Add(tableData);
                }
            }

            return result;
        }

        private AttendanceMachinePathResolution? ResolveAccessSource()
        {
            return AttendanceMachinePathResolver.Resolve(
                _configuration.GetValue<string>("AttendanceMachine:DbUrl"),
                _configuration.GetValue<string>("AttendanceMachine:DbPath"));
        }

        [SupportedOSPlatform("windows")]
        private static List<string> GetTableNames(OleDbConnection connection)
        {
            var tables = new List<string>();
            var schema = connection.GetSchema("Tables");
            foreach (DataRow row in schema.Rows)
            {
                var name = row["TABLE_NAME"]?.ToString();
                var type = row["TABLE_TYPE"]?.ToString();
                if (string.IsNullOrWhiteSpace(name) || !string.Equals(type, "TABLE", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (name.StartsWith("MSys", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                tables.Add(name);
            }

            return tables;
        }

        [SupportedOSPlatform("windows")]
        private static List<string> GetColumnNames(OleDbConnection connection, string tableName)
        {
            var columns = new List<(int? ordinal, string name)>();
            var schema = connection.GetSchema("Columns", new[] { null, null, tableName, null });
            foreach (DataRow row in schema.Rows)
            {
                var name = row["COLUMN_NAME"]?.ToString();
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                var ordinalValue = row["ORDINAL_POSITION"];
                int? ordinal = null;
                if (ordinalValue != null && ordinalValue != DBNull.Value)
                {
                    ordinal = Convert.ToInt32(ordinalValue, CultureInfo.InvariantCulture);
                }

                columns.Add((ordinal, name));
            }

            return columns
                .OrderBy(c => c.ordinal ?? int.MaxValue)
                .Select(c => c.name)
                .ToList();
        }

        private static string FormatValue(object value)
        {
            if (value == null || value == DBNull.Value)
            {
                return string.Empty;
            }

            if (value is DateTime dateTime)
            {
                return dateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            }

            return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
        }

        private static List<string> ResolveColumns(List<string> availableColumns, List<string> preferredColumns)
        {
            if (preferredColumns == null || preferredColumns.Count == 0)
            {
                return availableColumns;
            }

            var result = new List<string>();
            foreach (var column in preferredColumns)
            {
                var match = availableColumns.FirstOrDefault(c => string.Equals(c, column, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrWhiteSpace(match))
                {
                    result.Add(match);
                }
            }

            return result;
        }

        private static List<List<string>> FilterRows(List<List<string>> rows, string? token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return rows;
            }

            var normalized = token.Trim();
            return rows
                .Where(row => row.Any(cell => cell != null &&
                    cell.Contains(normalized, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }
    }
}
