using crmHuman.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading.Tasks;

namespace crmHuman.Pages.Attendance
{
    public class CheckInOutModel : BaseModel2
    {
        private readonly AccessAttendanceReader _accessReader;
        private readonly DirectAttendanceReader _directReader;

        public CheckInOutModel(IConfiguration configuration)
        {
            _accessReader = new AccessAttendanceReader(configuration);
            _directReader = new DirectAttendanceReader(configuration);
            KeyPage = "Attendance";
            TitlePage = "L\u1ECBch s\u1EED qu\u1EB9t th\u1EBB/\u0111i\u1EC3m danh";
        }

        public List<AccessTableData> Tables { get; private set; } = new List<AccessTableData>();
        public string? ErrorMessage { get; private set; }
        public string? SearchToken { get; private set; }
        public string? SelectedEmployee { get; private set; }
        public int PageSize { get; private set; } = 50;
        public List<EmployeeOption> Employees { get; private set; } = new List<EmployeeOption>();

        [SupportedOSPlatform("windows")]
        public Task<IActionResult> OnGetAsync()
        {
            if (!HttpContext.User.Identity.IsAuthenticated)
            {
                return Task.FromResult<IActionResult>(Redirect("/Login"));
            }

            GetInfoUser();
            if (!(Permision.View ?? false))
            {
                return Task.FromResult<IActionResult>(Page());
            }

            var useDirectSql = _directReader.IsEnabled();
            if (!useDirectSql)
            {
                ErrorMessage = "He thong chi dong bo nen tu file MDB vao SQL. Man hinh nay khong doc truc tiep Access de tranh loi OLEDB.";
                return Task.FromResult<IActionResult>(Page());
            }

            if (!useDirectSql && !OperatingSystem.IsWindows())
            {
                ErrorMessage = "Ch\u1EC9 h\u1ED7 tr\u1EE3 \u0111\u1ECDc d\u1EEF li\u1EC7u Access tr\u00EAn Windows.";
                return Task.FromResult<IActionResult>(Page());
            }

            SearchToken = Request.Query["q"];
            SelectedEmployee = Request.Query["emp"];
            PageSize = ParseInt(Request.Query["ps"], 50);

            ErrorMessage = useDirectSql
                ? _directReader.GetConfigError()
                : _accessReader.GetConfigError();
            if (string.IsNullOrWhiteSpace(ErrorMessage))
            {
                var userTable = (useDirectSql ? _directReader : null)?.LoadTables(new[]
                {
                    BuildUserQuery()
                }, 5000) ?? _accessReader.LoadTables(new[]
                {
                    BuildUserQuery()
                }, 5000);
                var userMap = BuildUserMap(userTable.FirstOrDefault());
                Employees = BuildEmployeeOptions(userMap);

                Tables = useDirectSql
                    ? _directReader.LoadTables(new[]
                    {
                        BuildQuery("CheckInOut", new[] { "UserEnrollNumber", "TimeStr", "MachineNo", "Source" })
                    }, 5000)
                    : _accessReader.LoadTables(new[]
                    {
                        BuildQuery("CheckInOut", new[] { "UserEnrollNumber", "TimeStr", "OriginType", "NewType", "MachineNo", "Source" }),
                        BuildQuery("DelInOut", new[] { "UserEnrollNumber", "TimeStr", "TimeType", "TimeSource", "MachineNo" })
                    }, 5000);
                AttendanceTableFormatter.NormalizeTables(Tables);
                ApplyUserNames(Tables, userMap);
                PrepareHistoryTables(Tables);
                if (!useDirectSql)
                {
                    Tables = MergeHistoryTables(Tables);
                }
                else if (Tables.Sum(table => table.TotalCount) == 0)
                {
                    ErrorMessage = "Ngu\u1ED3n direct SQL \u0111ang ho\u1EA1t \u0111\u1ED9ng, nh\u01B0ng ch\u01B0a c\u00F3 log qu\u1EB9t m\u1EDBi t\u1EEB m\u00E1y ch\u1EA5m c\u00F4ng. D\u1EEF li\u1EC7u s\u1EBD hi\u1EC3n th\u1ECB ngay sau l\u1EA7n ch\u1EA5m c\u00F4ng k\u1EBF ti\u1EBFp.";
                }
            }

            return Task.FromResult<IActionResult>(Page());
        }

        public string BuildPageLink(string table, int page)
        {
            var query = Request.Query.ToDictionary(k => k.Key, v => v.Value.ToString(), StringComparer.OrdinalIgnoreCase);
            query["p_" + table] = page.ToString(CultureInfo.InvariantCulture);
            if (!string.IsNullOrWhiteSpace(SearchToken))
            {
                query["q"] = SearchToken;
            }
            if (!string.IsNullOrWhiteSpace(SelectedEmployee))
            {
                query["emp"] = SelectedEmployee;
            }
            query["ps"] = PageSize.ToString(CultureInfo.InvariantCulture);

            var queryString = string.Join("&", query.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
            return string.IsNullOrWhiteSpace(queryString) ? string.Empty : "?" + queryString;
        }

        private AccessTableQuery BuildQuery(string table, IEnumerable<string> columns)
        {
            var page = ParseInt(Request.Query["p_" + table], 1);
            return new AccessTableQuery
            {
                Name = table,
                Columns = columns.ToList(),
                ColumnLabels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["UserEnrollNumber"] = "M\u00E3 v\u00E2n tay",
                    ["TimeStr"] = "Th\u1EDDi gian",
                    ["TimeDate"] = "Th\u1EDDi gian",
                    ["InOutMode"] = "V\u00E0o / Ra",
                    ["OriginType"] = "V\u00E0o / Ra",
                    ["NewType"] = "V\u00E0o / Ra",
                    ["TimeType"] = "V\u00E0o / Ra",
                    ["MachineNo"] = "M\u00E1y",
                    ["Source"] = "Ngu\u1ED3n",
                    ["TimeSource"] = "Ngu\u1ED3n",
                    ["CheckIn"] = "Gi\u1EDD v\u00E0o",
                    ["CheckOut"] = "Gi\u1EDD ra",
                    ["Overday"] = "Qua ng\u00E0y"
                },
                SearchToken = SearchToken,
                FilterColumn = string.IsNullOrWhiteSpace(SelectedEmployee) ? null : "UserEnrollNumber",
                FilterValue = string.IsNullOrWhiteSpace(SelectedEmployee) ? null : SelectedEmployee,
                Page = page,
                PageSize = PageSize
            };
        }

        private static int ParseInt(string? raw, int defaultValue)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return defaultValue;
            }

            return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
                ? value
                : defaultValue;
        }

        private static AccessTableQuery BuildUserQuery()
        {
            return new AccessTableQuery
            {
                Name = "UserInfo",
                Columns = new List<string> { "UserEnrollNumber", "UserFullName" }
            };
        }

        private static Dictionary<string, string> BuildUserMap(AccessTableData? table)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (table == null || table.Rows.Count == 0 || table.Columns.Count == 0)
            {
                return map;
            }

            var idIndex = table.Columns.FindIndex(c => c.Equals("UserEnrollNumber", StringComparison.OrdinalIgnoreCase));
            var nameIndex = table.Columns.FindIndex(c => c.Equals("UserFullName", StringComparison.OrdinalIgnoreCase));
            if (idIndex < 0 || nameIndex < 0)
            {
                return map;
            }

            foreach (var row in table.Rows)
            {
                if (idIndex >= row.Count || nameIndex >= row.Count)
                {
                    continue;
                }

                var id = row[idIndex]?.Trim();
                var name = row[nameIndex]?.Trim();
                if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                if (!map.ContainsKey(id))
                {
                    map[id] = name;
                }
            }

            return map;
        }

        private static List<EmployeeOption> BuildEmployeeOptions(Dictionary<string, string> userMap)
        {
            if (userMap == null || userMap.Count == 0)
            {
                return new List<EmployeeOption>();
            }

            return userMap
                .OrderBy(pair => pair.Value, StringComparer.OrdinalIgnoreCase)
                .ThenBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                .Select(pair => new EmployeeOption
                {
                    Id = pair.Key,
                    Name = pair.Value
                })
                .ToList();
        }

        private static void ApplyUserNames(List<AccessTableData> tables, Dictionary<string, string> userMap)
        {
            if (tables == null || tables.Count == 0 || userMap.Count == 0)
            {
                return;
            }

            foreach (var table in tables)
            {
                var idIndex = table.Columns.FindIndex(c => c.Equals("UserEnrollNumber", StringComparison.OrdinalIgnoreCase));
                if (idIndex < 0)
                {
                    continue;
                }

                table.Columns.Insert(idIndex + 1, "UserFullName");
                table.Headers.Insert(idIndex + 1, "T\u00EAn nh\u00E2n vi\u00EAn");

                for (var i = 0; i < table.Rows.Count; i++)
                {
                    var row = table.Rows[i];
                    var id = idIndex < row.Count ? row[idIndex]?.Trim() : string.Empty;
                    var name = (!string.IsNullOrWhiteSpace(id) && userMap.TryGetValue(id, out var value)) ? value : string.Empty;
                    row.Insert(idIndex + 1, name);
                }
            }
        }

        private static void PrepareHistoryTables(List<AccessTableData> tables)
        {
            if (tables == null || tables.Count == 0)
            {
                return;
            }

            foreach (var table in tables)
            {
                NormalizeTimeColumn(table);
                AddInOutTextColumn(table);
                KeepHistoryColumns(table);
            }
        }

        private static void NormalizeTimeColumn(AccessTableData table)
        {
            var timeIndex = table.Columns.FindIndex(c => c.Equals("TimeStr", StringComparison.OrdinalIgnoreCase));
            if (timeIndex >= 0)
            {
                return;
            }

            var timeDateIndex = table.Columns.FindIndex(c => c.Equals("TimeDate", StringComparison.OrdinalIgnoreCase));
            if (timeDateIndex < 0)
            {
                return;
            }

            table.Columns[timeDateIndex] = "TimeStr";
            if (table.Headers.Count > timeDateIndex)
            {
                table.Headers[timeDateIndex] = "Th\u1EDDi gian";
            }
        }

        private static void AddInOutTextColumn(AccessTableData table)
        {
            var inOutIndex = table.Columns.FindIndex(c => c.Equals("InOutText", StringComparison.OrdinalIgnoreCase));
            if (inOutIndex >= 0)
            {
                return;
            }

            var sourceColumns = new[] { "InOutMode", "OriginType", "NewType", "TimeType" };
            var sourceIndexes = new List<int>();
            foreach (var column in sourceColumns)
            {
                var index = table.Columns.FindIndex(c => c.Equals(column, StringComparison.OrdinalIgnoreCase));
                if (index >= 0)
                {
                    sourceIndexes.Add(index);
                }
            }

            if (sourceIndexes.Count == 0)
            {
                return;
            }

            var timeIndex = table.Columns.FindIndex(c => c.Equals("TimeStr", StringComparison.OrdinalIgnoreCase));
            if (timeIndex < 0)
            {
                timeIndex = table.Columns.Count - 1;
            }

            var insertIndex = timeIndex + 1;
            table.Columns.Insert(insertIndex, "InOutText");
            table.Headers.Insert(insertIndex, "V\u00E0o / Ra");

            for (var i = 0; i < table.Rows.Count; i++)
            {
                var row = table.Rows[i];
                var raw = string.Empty;
                foreach (var sourceIndex in sourceIndexes)
                {
                    if (sourceIndex < row.Count && !string.IsNullOrWhiteSpace(row[sourceIndex]))
                    {
                        raw = row[sourceIndex];
                        break;
                    }
                }

                var normalized = NormalizeInOut(raw);
                row.Insert(insertIndex, normalized);
            }

            var removeIndexes = new List<int>();
            foreach (var column in sourceColumns)
            {
                var index = table.Columns.FindIndex(c => c.Equals(column, StringComparison.OrdinalIgnoreCase));
                if (index >= 0)
                {
                    removeIndexes.Add(index);
                }
            }

            removeIndexes.Sort();
            for (var i = removeIndexes.Count - 1; i >= 0; i--)
            {
                var sourceIndex = removeIndexes[i];
                if (sourceIndex >= table.Columns.Count)
                {
                    continue;
                }

                table.Columns.RemoveAt(sourceIndex);
                if (table.Headers.Count > sourceIndex)
                {
                    table.Headers.RemoveAt(sourceIndex);
                }

                for (var rowIndex = 0; rowIndex < table.Rows.Count; rowIndex++)
                {
                    var row = table.Rows[rowIndex];
                    if (sourceIndex < row.Count)
                    {
                        row.RemoveAt(sourceIndex);
                    }
                }
            }
        }

        private static string NormalizeInOut(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return string.Empty;
            }

            var value = raw.Trim();
            if (value.Equals("I", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("IN", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("0", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("2", StringComparison.OrdinalIgnoreCase))
            {
                return "V\u00E0o";
            }

            if (value.Equals("O", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("OUT", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("1", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("3", StringComparison.OrdinalIgnoreCase))
            {
                return "Ra";
            }

            if (value.Contains("V\u00E0o", StringComparison.OrdinalIgnoreCase))
            {
                return "V\u00E0o";
            }

            if (value.Contains("Ra", StringComparison.OrdinalIgnoreCase))
            {
                return "Ra";
            }

            return value;
        }

        private static void KeepHistoryColumns(AccessTableData table)
        {
            var desired = new[] { "UserEnrollNumber", "UserFullName", "TimeStr", "InOutText" };
            var desiredSet = new HashSet<string>(desired, StringComparer.OrdinalIgnoreCase);
            var indexes = new List<int>();
            for (var i = 0; i < table.Columns.Count; i++)
            {
                if (desiredSet.Contains(table.Columns[i]))
                {
                    indexes.Add(i);
                }
            }

            var columns = new List<string>();
            var headers = new List<string>();
            foreach (var key in desired)
            {
                var index = table.Columns.FindIndex(c => c.Equals(key, StringComparison.OrdinalIgnoreCase));
                if (index >= 0)
                {
                    columns.Add(table.Columns[index]);
                    headers.Add(table.Headers.Count > index ? table.Headers[index] : key);
                }
            }

            if (columns.Count == 0)
            {
                return;
            }

            var rows = new List<List<string>>();
            foreach (var row in table.Rows)
            {
                var newRow = new List<string>();
                foreach (var key in desired)
                {
                    var index = table.Columns.FindIndex(c => c.Equals(key, StringComparison.OrdinalIgnoreCase));
                    if (index >= 0 && index < row.Count)
                    {
                        newRow.Add(row[index]);
                    }
                    else
                    {
                        newRow.Add(string.Empty);
                    }
                }

                rows.Add(newRow);
            }

            table.Columns = desired.ToList();
            table.Headers = new[] { "M\u00E3 v\u00E2n tay", "T\u00EAn nh\u00E2n vi\u00EAn", "Th\u1EDDi gian", "V\u00E0o / Ra" }.ToList();
            table.Rows = rows;
        }

        private static List<AccessTableData> MergeHistoryTables(List<AccessTableData> tables)
        {
            if (tables == null || tables.Count == 0)
            {
                return new List<AccessTableData>();
            }

            var validTables = tables.Where(t => t.Columns.Count > 0).ToList();
            if (validTables.Count == 0)
            {
                return tables;
            }

            var preferredOrder = new[]
            {
                "UserEnrollNumber",
                "UserFullName",
                "TimeStr",
                "InOutText"
            };

            var columnSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var table in validTables)
            {
                foreach (var col in table.Columns)
                {
                    columnSet.Add(col);
                }
            }

            var columns = new List<string>();
            foreach (var col in preferredOrder)
            {
                if (columnSet.Contains(col))
                {
                    columns.Add(col);
                }
            }

            foreach (var col in columnSet)
            {
                if (!columns.Contains(col, StringComparer.OrdinalIgnoreCase))
                {
                    columns.Add(col);
                }
            }

            var headerMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var table in validTables)
            {
                for (var i = 0; i < table.Columns.Count; i++)
                {
                    if (i >= table.Headers.Count)
                    {
                        continue;
                    }

                    var columnName = table.Columns[i];
                    if (!headerMap.ContainsKey(columnName))
                    {
                        headerMap[columnName] = table.Headers[i];
                    }
                }
            }

            var headers = columns
                .Select(col => headerMap.TryGetValue(col, out var label) ? label : col)
                .ToList();

            var rows = new List<List<string>>();
            var totalCount = 0;

            foreach (var table in validTables)
            {
                totalCount += table.TotalCount;
                var columnIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                for (var i = 0; i < table.Columns.Count; i++)
                {
                    columnIndex[table.Columns[i]] = i;
                }

                foreach (var row in table.Rows)
                {
                    var mergedRow = new List<string>(columns.Count);
                    foreach (var col in columns)
                    {
                        if (columnIndex.TryGetValue(col, out var index) && index < row.Count)
                        {
                            mergedRow.Add(row[index]);
                        }
                        else
                        {
                            mergedRow.Add(string.Empty);
                        }
                    }

                    rows.Add(mergedRow);
                }
            }

            var timeIndex = columns.FindIndex(c => c.Equals("TimeStr", StringComparison.OrdinalIgnoreCase));
            if (timeIndex >= 0)
            {
                rows = rows
                    .OrderByDescending(row =>
                    {
                        if (timeIndex >= row.Count)
                        {
                            return DateTime.MinValue;
                        }

                        var raw = row[timeIndex];
                        return DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var time)
                            ? time
                            : DateTime.MinValue;
                    })
                    .ToList();
            }

            var merged = new AccessTableData
            {
                Name = "AttendanceHistory",
                DisplayName = "L\u1ECBch s\u1EED ch\u1EA5m c\u00F4ng",
                Columns = columns,
                Headers = headers,
                Rows = rows,
                TotalCount = totalCount,
                Page = 1,
                PageSize = rows.Count
            };

            return new List<AccessTableData> { merged };
        }

        public sealed class EmployeeOption
        {
            public string Id { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
        }

    }
}
