using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Globalization;
using System.Linq;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using VS.Human.Business.Imp;
using VS.Human.Business.Helpers;
using VS.Human.Business.Model;
using VS.Human.Item;
using VS.Human.Rep;
using VS.Human.Rep.Model;

namespace VS.Human.Business
{
    public class AttendanceBusiness : BaseBusiness, IAttendanceBusiness
    {
        private static readonly string[] LogTableNameHints = new[]
        {
            "checkinout", "attlog", "attendance", "attend", "log", "record", "transaction"
        };

        private static readonly string[] LogTimeColumns = new[]
        {
            "checktime", "check_time", "logtime", "time", "datetime", "checkin", "checkinout", "worktime"
        };

        private static readonly string[] LogUserColumns = new[]
        {
            "userid", "user_id", "enrollnumber", "badgenumber", "cardno", "pin", "empid", "personid", "id"
        };

        private static readonly string[] UserTableNameHints = new[]
        {
            "user", "userinfo", "employee", "person", "staff"
        };

        private static readonly string[] UserIdColumns = new[]
        {
            "userid", "user_id", "id", "empid", "personid"
        };

        private static readonly string[] UserFingerprintColumns = new[]
        {
            "badgenumber", "enrollnumber", "cardno", "pin", "ssn", "empno"
        };

        private static readonly string[] UserNameColumns = new[]
        {
            "name", "username", "fullname", "hoten", "ten"
        };

        private static readonly Dictionary<string, string> HeaderAliases = new(StringComparer.OrdinalIgnoreCase)
        {
            { "manv", "FingerprintCode" },
            { "manhanvien", "FingerprintCode" },
            { "tennhanvien", "EmployeeName" },
            { "tennv", "EmployeeName" },
            { "phongban", "DepartmentName" },
            { "bophan", "DepartmentName" },
            { "chucvu", "PositionName" },
            { "vitri", "PositionName" },
            { "ngay", "WorkDate" },
            { "thu", "DayName" },
            { "vao", "CheckIn" },
            { "ra", "CheckOut" },
            { "cong", "WorkDay" },
            { "gio", "WorkHours" },
            { "cong+", "WorkDayPlus" },
            { "gio+", "WorkHoursPlus" },
            { "vaotre", "LateMinutes" },
            { "rasom", "EarlyMinutes" },
            { "tc1", "Shift1" },
            { "tc2", "Shift2" },
            { "tc3", "Shift3" },
            { "tenca", "ShiftName" },
            { "kihieu", "Symbol" },
            { "kihieu+", "SymbolPlus" },
            { "tonggio", "TotalHours" }
        };

        private readonly IConfiguration _configuration;

        public AttendanceBusiness(IUnitOfWork unitOfWork, IHttpContextAccessor contextAccessor, IConfiguration configuration)
            : base(unitOfWork, contextAccessor)
        {
            _configuration = configuration;
        }

        public async Task<BaseList> GetSummary(AttendanceRequest request)
        {
            return await _unitOfWork.AttendanceRep.GetSummary(request);
        }

        public async Task<List<AttendanceDetailModel>> GetDetails(int? employeeId, string? fingerprintCode, DateTime fromDate, DateTime toDate, int userId)
        {
            return await _unitOfWork.AttendanceRep.GetDetails(employeeId, fingerprintCode, fromDate, toDate, userId);
        }

        public async Task<AttendanceImportResult> ImportAsync(IFormFile file, int userId)
        {
            var result = new AttendanceImportResult();
            if (file == null || file.Length == 0)
            {
                return ErrorResult(result, "File khong hop le");
            }

            try
            {
                using var stream = file.OpenReadStream();
                using var document = SpreadsheetDocument.Open(stream, false);
                var workbookPart = document.WorkbookPart;
                var worksheetPart = workbookPart?.WorksheetParts.FirstOrDefault();
                if (worksheetPart == null)
                {
                    return ErrorResult(result, "File Excel khong hop le");
                }

                var sheetData = worksheetPart.Worksheet.Elements<SheetData>().FirstOrDefault();
                if (sheetData == null)
                {
                    return ErrorResult(result, "Khong tim thay du lieu trong file");
                }

                var rows = sheetData.Elements<Row>().ToList();
                if (rows.Count == 0)
                {
                    return ErrorResult(result, "File Excel khong co du lieu");
                }

                var headerRowIndex = FindHeaderRow(rows, workbookPart);
                if (headerRowIndex < 0)
                {
                    return ErrorResult(result, "Khong tim thay dong tieu de hop le");
                }

                var headerMap = GetHeaderMap(rows[headerRowIndex], workbookPart);
                if (headerMap.Count == 0)
                {
                    return ErrorResult(result, "Khong nhan dien duoc cot du lieu");
                }

                var employeeCache = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                for (int i = headerRowIndex + 1; i < rows.Count; i++)
                {
                    var row = rows[i];
                    var values = ExcelHelper.GetRowValues(row, workbookPart).ToList();
                    if (values.All(string.IsNullOrWhiteSpace))
                    {
                        continue;
                    }

                    var fingerprintCode = GetValue(headerMap, values, "FingerprintCode");
                    if (string.IsNullOrWhiteSpace(fingerprintCode))
                    {
                        continue;
                    }

                    var workDateRaw = GetValue(headerMap, values, "WorkDate");
                    if (!ExcelHelper.TryParseDate(workDateRaw, out var workDate))
                    {
                        AddError(result, GetRowNumber(row, i), "Ngay khong hop le");
                        continue;
                    }

                    var record = new AttendanceRecord
                    {
                        FingerprintCode = fingerprintCode.Trim(),
                        EmployeeName = GetValue(headerMap, values, "EmployeeName"),
                        DepartmentName = GetValue(headerMap, values, "DepartmentName"),
                        PositionName = GetValue(headerMap, values, "PositionName"),
                        WorkDate = workDate.Date,
                        DayName = GetValue(headerMap, values, "DayName"),
                        CheckIn = ParseTime(GetValue(headerMap, values, "CheckIn")),
                        CheckOut = ParseTime(GetValue(headerMap, values, "CheckOut")),
                        WorkDay = ParseDecimal(GetValue(headerMap, values, "WorkDay")),
                        WorkHours = ParseDecimal(GetValue(headerMap, values, "WorkHours")),
                        WorkDayPlus = ParseDecimal(GetValue(headerMap, values, "WorkDayPlus")),
                        WorkHoursPlus = ParseDecimal(GetValue(headerMap, values, "WorkHoursPlus")),
                        LateMinutes = ParseInt(GetValue(headerMap, values, "LateMinutes")),
                        EarlyMinutes = ParseInt(GetValue(headerMap, values, "EarlyMinutes")),
                        Shift1 = ParseDecimal(GetValue(headerMap, values, "Shift1")),
                        Shift2 = ParseDecimal(GetValue(headerMap, values, "Shift2")),
                        Shift3 = ParseDecimal(GetValue(headerMap, values, "Shift3")),
                        ShiftName = GetValue(headerMap, values, "ShiftName"),
                        Symbol = GetValue(headerMap, values, "Symbol"),
                        SymbolPlus = GetValue(headerMap, values, "SymbolPlus"),
                        TotalHours = ParseDecimal(GetValue(headerMap, values, "TotalHours")),
                        SourceFile = file.FileName,
                        RowIndex = GetRowNumber(row, i)
                    };

                    if (!employeeCache.TryGetValue(record.FingerprintCode, out var employeeId))
                    {
                        var employee = await _unitOfWork.EmployeeRep.GetByFingerprintCode(record.FingerprintCode);
                        employeeId = employee != null && employee.Id > 0 ? employee.Id : 0;
                        employeeCache[record.FingerprintCode] = employeeId;
                    }

                    if (employeeId > 0)
                    {
                        record.EmployeeId = employeeId;
                    }

                    result.Total++;
                    var saved = await _unitOfWork.AttendanceRep.UpsertAsync(record, userId);
                    if (saved)
                    {
                        result.TotalSuccess++;
                    }
                    else
                    {
                        AddError(result, record.RowIndex ?? (i + 1), "Khong the luu du lieu");
                    }
                }
            }
            catch (Exception ex)
            {
                return ErrorResult(result, $"Loi doc file Excel: {ex.Message}");
            }

            return result;
        }

        public async Task<AttendanceImportResult> SyncFromAccessAsync(DateTime fromDate, DateTime toDate, int userId)
        {
            var result = new AttendanceImportResult();
            var options = GetMachineOptions();

            if (string.IsNullOrWhiteSpace(options.DbPath))
            {
                return ErrorResult(result, "Chua cau hinh duong dan file cham cong");
            }

            if (!File.Exists(options.DbPath))
            {
                return ErrorResult(result, "Khong tim thay file cham cong");
            }

            if (fromDate == DateTime.MinValue || toDate == DateTime.MinValue)
            {
                fromDate = DateTime.Today.AddDays(-7);
                toDate = DateTime.Today;
            }

            if (fromDate > toDate)
            {
                var temp = fromDate;
                fromDate = toDate;
                toDate = temp;
            }

            if (!TryOpenAccessConnection(options, out var connection, out var error) || connection == null)
            {
                return ErrorResult(result, $"Khong the mo file cham cong: {error}");
            }

            using (connection)
            {
                var tables = GetTableNames(connection);
                if (tables.Count == 0)
                {
                    return ErrorResult(result, "Khong tim thay bang du lieu trong file");
                }

                var columnsByTable = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
                foreach (var table in tables)
                {
                    columnsByTable[table] = GetColumnNames(connection, table);
                }

                var logSchema = ResolveLogTableSchema(options, columnsByTable);
                if (logSchema == null)
                {
                    return ErrorResult(result, "Khong tim thay bang cham cong hop le");
                }

                var userSchema = ResolveUserTableSchema(options, columnsByTable);
                var userMap = LoadUserMap(connection, userSchema);
                var aggregates = new Dictionary<string, AttendanceAggregate>(StringComparer.OrdinalIgnoreCase);

                var fromDateOnly = fromDate.Date;
                var toExclusive = toDate.Date.AddDays(1);

                var logQuery = $"SELECT [{logSchema.UserIdColumn}], [{logSchema.TimeColumn}] FROM [{logSchema.TableName}] " +
                               $"WHERE [{logSchema.TimeColumn}] >= ? AND [{logSchema.TimeColumn}] < ? " +
                               $"ORDER BY [{logSchema.TimeColumn}]";

                using (var command = new OleDbCommand(logQuery, connection))
                {
                    command.Parameters.Add(new OleDbParameter { OleDbType = OleDbType.Date, Value = fromDateOnly });
                    command.Parameters.Add(new OleDbParameter { OleDbType = OleDbType.Date, Value = toExclusive });

                    using var reader = command.ExecuteReader();
                    if (reader == null)
                    {
                        return ErrorResult(result, "Khong doc duoc du lieu cham cong");
                    }

                    while (reader.Read())
                    {
                        var rawUserId = GetReaderValue(reader, 0);
                        if (string.IsNullOrWhiteSpace(rawUserId))
                        {
                            continue;
                        }

                        var normalizedUserId = NormalizeUserId(rawUserId);
                        if (!TryGetDateTime(reader.GetValue(1), out var checkTime))
                        {
                            continue;
                        }

                        if (checkTime < fromDateOnly || checkTime >= toExclusive)
                        {
                            continue;
                        }

                        var userInfo = FindUserInfo(userMap, rawUserId, normalizedUserId);
                        var fingerprint = userInfo?.FingerprintCode;
                        if (string.IsNullOrWhiteSpace(fingerprint))
                        {
                            fingerprint = rawUserId;
                        }

                        fingerprint = fingerprint.Trim();
                        if (string.IsNullOrWhiteSpace(fingerprint))
                        {
                            continue;
                        }

                        var workDate = checkTime.Date;
                        var key = fingerprint + "|" + workDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                        if (!aggregates.TryGetValue(key, out var aggregate))
                        {
                            aggregate = new AttendanceAggregate(fingerprint, workDate);
                            aggregates[key] = aggregate;
                        }

                        if (string.IsNullOrWhiteSpace(aggregate.EmployeeName))
                        {
                            aggregate.EmployeeName = userInfo?.Name;
                        }

                        aggregate.Update(checkTime.TimeOfDay);
                    }
                }

                var employeeCache = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                foreach (var aggregate in aggregates.Values)
                {
                    var record = BuildAttendanceRecord(aggregate, options.DbPath);

                    if (!string.IsNullOrWhiteSpace(record.FingerprintCode))
                    {
                        if (!employeeCache.TryGetValue(record.FingerprintCode, out var employeeId))
                        {
                            var employee = await _unitOfWork.EmployeeRep.GetByFingerprintCode(record.FingerprintCode);
                            employeeId = employee != null && employee.Id > 0 ? employee.Id : 0;
                            employeeCache[record.FingerprintCode] = employeeId;
                        }

                        if (employeeId > 0)
                        {
                            record.EmployeeId = employeeId;
                        }
                    }

                    result.Total++;
                    var saved = await _unitOfWork.AttendanceRep.UpsertAsync(record, userId);
                    if (saved)
                    {
                        result.TotalSuccess++;
                    }
                    else
                    {
                        AddError(result, result.Total, "Khong the luu du lieu");
                    }
                }
            }

            return result;
        }

        private static string GetValue(Dictionary<string, int> headerMap, List<string> values, string key)
        {
            if (headerMap.TryGetValue(key, out var idx) && idx >= 0 && idx < values.Count)
            {
                return values[idx]?.Trim() ?? string.Empty;
            }

            return string.Empty;
        }

        private static int FindHeaderRow(List<Row> rows, WorkbookPart workbookPart)
        {
            for (int i = 0; i < rows.Count; i++)
            {
                var values = ExcelHelper.GetRowValues(rows[i], workbookPart)
                    .Select(NormalizeHeader)
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .ToList();

                if ((values.Contains("manv") || values.Contains("manhanvien")) && (values.Contains("tennhanvien") || values.Contains("tennv")))
                {
                    return i;
                }
            }

            return -1;
        }

        private static Dictionary<string, int> GetHeaderMap(Row headerRow, WorkbookPart workbookPart)
        {
            var values = ExcelHelper.GetRowValues(headerRow, workbookPart).ToList();
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < values.Count; i++)
            {
                var normalized = NormalizeHeader(values[i]);
                if (string.IsNullOrWhiteSpace(normalized))
                {
                    continue;
                }

                if (HeaderAliases.TryGetValue(normalized, out var key))
                {
                    if (!map.ContainsKey(key))
                    {
                        map[key] = i;
                    }
                }
            }

            return map;
        }

        private static string NormalizeHeader(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return string.Empty;
            }

            var text = RemoveDiacritics(raw.Trim().ToLowerInvariant());
            text = Regex.Replace(text, @"\s+", "");
            text = text.Replace(":", string.Empty)
                       .Replace(".", string.Empty);

            return text;
        }

        private static string RemoveDiacritics(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            var normalized = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();

            foreach (var ch in normalized)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (category != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(ch);
                }
            }

            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        private static decimal? ParseDecimal(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            raw = raw.Trim();
            if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
            {
                return value;
            }

            if (decimal.TryParse(raw, NumberStyles.Any, new CultureInfo("vi-VN"), out value))
            {
                return value;
            }

            var sanitized = raw.Replace(",", ".");
            if (decimal.TryParse(sanitized, NumberStyles.Any, CultureInfo.InvariantCulture, out value))
            {
                return value;
            }

            return null;
        }

        private static int? ParseInt(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            raw = raw.Trim();
            if (int.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
            {
                return value;
            }

            if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var decimalValue))
            {
                return (int)Math.Round(decimalValue);
            }

            if (int.TryParse(raw, NumberStyles.Any, new CultureInfo("vi-VN"), out value))
            {
                return value;
            }

            if (decimal.TryParse(raw, NumberStyles.Any, new CultureInfo("vi-VN"), out decimalValue))
            {
                return (int)Math.Round(decimalValue);
            }

            return null;
        }

        private static TimeSpan? ParseTime(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            raw = raw.Trim();
            if (raw == "0" || raw == "0.0" || raw == "0.00")
            {
                return null;
            }

            if (ExcelHelper.TryParseDate(raw, out var dateTime))
            {
                return dateTime.TimeOfDay;
            }

            if (TimeSpan.TryParse(raw, CultureInfo.InvariantCulture, out var time))
            {
                return time;
            }

            if (DateTime.TryParse(raw, out dateTime))
            {
                return dateTime.TimeOfDay;
            }

            return null;
        }

        private static int GetRowNumber(Row row, int rowIndex)
        {
            if (row.RowIndex != null)
            {
                return (int)row.RowIndex.Value;
            }

            return rowIndex + 1;
        }

        private static AttendanceImportResult ErrorResult(AttendanceImportResult result, string message)
        {
            result.Errors.Add(new AttendanceImportError { Row = 0, Content = message });
            result.TotalError++;
            return result;
        }

        private static void AddError(AttendanceImportResult result, int row, string message)
        {
            result.TotalError++;
            result.Errors.Add(new AttendanceImportError { Row = row, Content = message });
        }

        private AttendanceMachineOptions GetMachineOptions()
        {
            var options = new AttendanceMachineOptions();
            _configuration.GetSection("AttendanceMachine").Bind(options);
            return options;
        }

        private static bool TryOpenAccessConnection(AttendanceMachineOptions options, out OleDbConnection? connection, out string error)
        {
            connection = null;
            error = string.Empty;

            var providers = new List<string>();
            if (!string.IsNullOrWhiteSpace(options.Provider))
            {
                providers.Add(options.Provider);
            }
            else
            {
                providers.Add("Microsoft.ACE.OLEDB.16.0");
                providers.Add("Microsoft.ACE.OLEDB.12.0");
                providers.Add("Microsoft.Jet.OLEDB.4.0");
            }

            foreach (var provider in providers)
            {
                try
                {
                    var connectionString = BuildConnectionString(provider, options.DbPath, options.Password);
                    var candidate = new OleDbConnection(connectionString);
                    candidate.Open();
                    connection = candidate;
                    return true;
                }
                catch (Exception ex)
                {
                    error = ex.Message;
                }
            }

            return false;
        }

        private static string BuildConnectionString(string provider, string dbPath, string? password)
        {
            var builder = new OleDbConnectionStringBuilder
            {
                Provider = provider,
                DataSource = dbPath
            };

            if (!string.IsNullOrWhiteSpace(password))
            {
                builder["Jet OLEDB:Database Password"] = password;
            }

            builder["Persist Security Info"] = false;
            return builder.ConnectionString;
        }

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

        private static List<string> GetColumnNames(OleDbConnection connection, string tableName)
        {
            var columns = new List<string>();
            var schema = connection.GetSchema("Columns", new[] { null, null, tableName, null });
            foreach (DataRow row in schema.Rows)
            {
                var name = row["COLUMN_NAME"]?.ToString();
                if (!string.IsNullOrWhiteSpace(name))
                {
                    columns.Add(name);
                }
            }

            return columns;
        }

        private static LogTableSchema? ResolveLogTableSchema(AttendanceMachineOptions options, Dictionary<string, List<string>> columnsByTable)
        {
            if (!string.IsNullOrWhiteSpace(options.LogTable))
            {
                if (!columnsByTable.TryGetValue(options.LogTable, out var columns))
                {
                    return null;
                }

                var userIdColumn = ResolveColumn(columns, options.LogUserIdColumn, LogUserColumns);
                var timeColumn = ResolveColumn(columns, options.LogTimeColumn, LogTimeColumns);
                if (string.IsNullOrWhiteSpace(userIdColumn) || string.IsNullOrWhiteSpace(timeColumn))
                {
                    return null;
                }

                return new LogTableSchema(options.LogTable, userIdColumn, timeColumn);
            }

            LogTableSchema? best = null;
            var bestScore = -1;
            foreach (var kvp in columnsByTable)
            {
                var tableName = kvp.Key;
                var columns = kvp.Value;

                var userIdColumn = ResolveColumn(columns, null, LogUserColumns);
                var timeColumn = ResolveColumn(columns, null, LogTimeColumns);

                if (string.IsNullOrWhiteSpace(userIdColumn) || string.IsNullOrWhiteSpace(timeColumn))
                {
                    continue;
                }

                var score = 0;
                if (ContainsNameHint(tableName, LogTableNameHints))
                {
                    score += 5;
                }

                score += 5;
                score += 3;

                if (score > bestScore)
                {
                    bestScore = score;
                    best = new LogTableSchema(tableName, userIdColumn, timeColumn);
                }
            }

            return best;
        }

        private static UserTableSchema? ResolveUserTableSchema(AttendanceMachineOptions options, Dictionary<string, List<string>> columnsByTable)
        {
            if (!string.IsNullOrWhiteSpace(options.UserTable))
            {
                if (!columnsByTable.TryGetValue(options.UserTable, out var columns))
                {
                    return null;
                }

                var userIdColumn = ResolveColumn(columns, options.UserIdColumn, UserIdColumns);
                if (string.IsNullOrWhiteSpace(userIdColumn))
                {
                    return null;
                }

                var fingerprintColumn = ResolveColumn(columns, options.UserFingerprintColumn, UserFingerprintColumns);
                var nameColumn = ResolveColumn(columns, options.UserNameColumn, UserNameColumns);
                if (string.IsNullOrWhiteSpace(fingerprintColumn) && string.IsNullOrWhiteSpace(nameColumn))
                {
                    return null;
                }

                return new UserTableSchema(options.UserTable, userIdColumn, fingerprintColumn, nameColumn);
            }

            UserTableSchema? best = null;
            var bestScore = -1;

            foreach (var kvp in columnsByTable)
            {
                var tableName = kvp.Key;
                var columns = kvp.Value;

                var userIdColumn = ResolveColumn(columns, null, UserIdColumns);
                if (string.IsNullOrWhiteSpace(userIdColumn))
                {
                    continue;
                }

                var fingerprintColumn = ResolveColumn(columns, null, UserFingerprintColumns);
                var nameColumn = ResolveColumn(columns, null, UserNameColumns);

                if (string.IsNullOrWhiteSpace(fingerprintColumn) && string.IsNullOrWhiteSpace(nameColumn))
                {
                    continue;
                }

                var score = 0;
                if (ContainsNameHint(tableName, UserTableNameHints))
                {
                    score += 5;
                }

                if (!string.IsNullOrWhiteSpace(fingerprintColumn))
                {
                    score += 3;
                }

                if (!string.IsNullOrWhiteSpace(nameColumn))
                {
                    score += 2;
                }

                score += 2;

                if (score > bestScore)
                {
                    bestScore = score;
                    best = new UserTableSchema(tableName, userIdColumn, fingerprintColumn, nameColumn);
                }
            }

            return best;
        }

        private static string? ResolveColumn(List<string> columns, string? explicitColumn, string[] candidates)
        {
            if (!string.IsNullOrWhiteSpace(explicitColumn))
            {
                var explicitMatch = columns.FirstOrDefault(c => string.Equals(c, explicitColumn, StringComparison.OrdinalIgnoreCase));
                return explicitMatch;
            }

            foreach (var candidate in candidates)
            {
                var match = columns.FirstOrDefault(c => string.Equals(c, candidate, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrWhiteSpace(match))
                {
                    return match;
                }
            }

            return null;
        }

        private static bool ContainsNameHint(string tableName, string[] hints)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                return false;
            }

            var normalized = tableName.ToLowerInvariant();
            return hints.Any(hint => normalized.Contains(hint));
        }

        private static Dictionary<string, MachineUserInfo> LoadUserMap(OleDbConnection connection, UserTableSchema? schema)
        {
            var map = new Dictionary<string, MachineUserInfo>(StringComparer.OrdinalIgnoreCase);
            if (schema == null)
            {
                return map;
            }

            var selectColumns = new List<string> { schema.UserIdColumn };
            if (!string.IsNullOrWhiteSpace(schema.FingerprintColumn))
            {
                selectColumns.Add(schema.FingerprintColumn);
            }

            if (!string.IsNullOrWhiteSpace(schema.NameColumn))
            {
                selectColumns.Add(schema.NameColumn);
            }

            var selectList = string.Join(", ", selectColumns.Select(col => $"[{col}]"));
            var query = $"SELECT {selectList} FROM [{schema.TableName}]";

            using var command = new OleDbCommand(query, connection);
            using var reader = command.ExecuteReader();
            if (reader == null)
            {
                return map;
            }

            while (reader.Read())
            {
                var rawUserId = GetReaderValue(reader, 0);
                if (string.IsNullOrWhiteSpace(rawUserId))
                {
                    continue;
                }

                var normalized = NormalizeUserId(rawUserId);
                var index = 1;
                string? fingerprint = null;
                string? name = null;

                if (!string.IsNullOrWhiteSpace(schema.FingerprintColumn))
                {
                    fingerprint = GetReaderValue(reader, index++);
                }

                if (!string.IsNullOrWhiteSpace(schema.NameColumn))
                {
                    name = GetReaderValue(reader, index++);
                }

                var info = new MachineUserInfo
                {
                    FingerprintCode = string.IsNullOrWhiteSpace(fingerprint) ? null : fingerprint.Trim(),
                    Name = string.IsNullOrWhiteSpace(name) ? null : name.Trim()
                };

                AddUserMap(map, rawUserId, normalized, info);
            }

            return map;
        }

        private static void AddUserMap(Dictionary<string, MachineUserInfo> map, string rawUserId, string normalized, MachineUserInfo info)
        {
            if (!map.ContainsKey(rawUserId))
            {
                map[rawUserId] = info;
            }

            if (!string.Equals(rawUserId, normalized, StringComparison.OrdinalIgnoreCase) && !map.ContainsKey(normalized))
            {
                map[normalized] = info;
            }
        }

        private static MachineUserInfo? FindUserInfo(Dictionary<string, MachineUserInfo> map, string rawUserId, string normalized)
        {
            if (map.TryGetValue(rawUserId, out var info))
            {
                return info;
            }

            if (!string.IsNullOrWhiteSpace(normalized) && map.TryGetValue(normalized, out info))
            {
                return info;
            }

            return null;
        }

        private static AttendanceRecord BuildAttendanceRecord(AttendanceAggregate aggregate, string sourcePath)
        {
            var checkIn = aggregate.FirstTime;
            var checkOut = aggregate.PunchCount > 1 ? aggregate.LastTime : null;
            decimal? workHours = null;

            if (checkIn.HasValue && checkOut.HasValue)
            {
                var duration = checkOut.Value - checkIn.Value;
                if (duration < TimeSpan.Zero)
                {
                    duration = duration.Add(TimeSpan.FromDays(1));
                }

                workHours = Math.Round((decimal)duration.TotalHours, 2);
            }

            var record = new AttendanceRecord
            {
                FingerprintCode = aggregate.FingerprintCode,
                EmployeeName = aggregate.EmployeeName,
                WorkDate = aggregate.WorkDate,
                DayName = GetDayName(aggregate.WorkDate),
                CheckIn = checkIn,
                CheckOut = checkOut,
                WorkDay = aggregate.PunchCount > 0 ? 1 : 0,
                WorkHours = workHours,
                TotalHours = workHours,
                SourceFile = string.IsNullOrWhiteSpace(sourcePath) ? null : Path.GetFileName(sourcePath)
            };

            return record;
        }

        private static string NormalizeUserId(string rawUserId)
        {
            if (string.IsNullOrWhiteSpace(rawUserId))
            {
                return string.Empty;
            }

            var trimmed = rawUserId.Trim();
            var normalized = trimmed.TrimStart('0');
            return string.IsNullOrWhiteSpace(normalized) ? "0" : normalized;
        }

        private static string? GetReaderValue(IDataRecord record, int index)
        {
            if (record == null || index < 0 || index >= record.FieldCount)
            {
                return null;
            }

            var value = record.GetValue(index);
            if (value == null || value == DBNull.Value)
            {
                return null;
            }

            return Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        private static bool TryGetDateTime(object value, out DateTime dateTime)
        {
            dateTime = default;
            if (value == null || value == DBNull.Value)
            {
                return false;
            }

            if (value is DateTime dt)
            {
                dateTime = dt;
                return true;
            }

            if (value is double oaDate)
            {
                dateTime = DateTime.FromOADate(oaDate);
                return true;
            }

            var text = Convert.ToString(value, CultureInfo.InvariantCulture);
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out dateTime))
            {
                return true;
            }

            if (DateTime.TryParse(text, new CultureInfo("vi-VN"), DateTimeStyles.AssumeLocal, out dateTime))
            {
                return true;
            }

            return false;
        }

        private static string GetDayName(DateTime workDate)
        {
            return workDate.DayOfWeek switch
            {
                DayOfWeek.Monday => "Thu 2",
                DayOfWeek.Tuesday => "Thu 3",
                DayOfWeek.Wednesday => "Thu 4",
                DayOfWeek.Thursday => "Thu 5",
                DayOfWeek.Friday => "Thu 6",
                DayOfWeek.Saturday => "Thu 7",
                _ => "CN"
            };
        }

        private sealed class LogTableSchema
        {
            public LogTableSchema(string tableName, string userIdColumn, string timeColumn)
            {
                TableName = tableName;
                UserIdColumn = userIdColumn;
                TimeColumn = timeColumn;
            }

            public string TableName { get; }
            public string UserIdColumn { get; }
            public string TimeColumn { get; }
        }

        private sealed class UserTableSchema
        {
            public UserTableSchema(string tableName, string userIdColumn, string? fingerprintColumn, string? nameColumn)
            {
                TableName = tableName;
                UserIdColumn = userIdColumn;
                FingerprintColumn = fingerprintColumn;
                NameColumn = nameColumn;
            }

            public string TableName { get; }
            public string UserIdColumn { get; }
            public string? FingerprintColumn { get; }
            public string? NameColumn { get; }
        }

        private sealed class MachineUserInfo
        {
            public string? FingerprintCode { get; set; }
            public string? Name { get; set; }
        }

        private sealed class AttendanceAggregate
        {
            public AttendanceAggregate(string fingerprintCode, DateTime workDate)
            {
                FingerprintCode = fingerprintCode;
                WorkDate = workDate.Date;
            }

            public string FingerprintCode { get; }
            public string? EmployeeName { get; set; }
            public DateTime WorkDate { get; }
            public TimeSpan? FirstTime { get; private set; }
            public TimeSpan? LastTime { get; private set; }
            public int PunchCount { get; private set; }

            public void Update(TimeSpan time)
            {
                if (!FirstTime.HasValue || time < FirstTime.Value)
                {
                    FirstTime = time;
                }

                if (!LastTime.HasValue || time > LastTime.Value)
                {
                    LastTime = time;
                }

                PunchCount++;
            }
        }
    }
}
