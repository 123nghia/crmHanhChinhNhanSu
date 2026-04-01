using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.IO;
using System.Runtime.Versioning;
using System.Text;
using System.Text.RegularExpressions;
using VS.Human.Business.Imp;
using VS.Human.Business.Helpers;
using VS.Human.Business.Model;
using VS.Human.Item;
using VS.Human.Rep;
using VS.Human.Rep.Model;
using VS.Human.Utility;

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

        private static readonly string[] ScheduleIdColumns = new[]
        {
            "schid", "scheduleid", "schedule_id", "sch_id"
        };

        private static readonly string[] ScheduleNameColumns = new[]
        {
            "schname", "schedulename", "name"
        };

        private static readonly string[] ScheduleAbsentSatColumns = new[]
        {
            "isabsentsat", "absentsat", "isoffsat", "offsat"
        };

        private static readonly string[] ScheduleAbsentSunColumns = new[]
        {
            "isabsentsun", "absentsun", "isoffsun", "offsun"
        };

        private static readonly string[] ShiftIdColumns = new[]
        {
            "shiftid", "shift_id", "id"
        };

        private static readonly string[] ShiftCodeColumns = new[]
        {
            "shiftcode", "code"
        };

        private static readonly string[] ShiftNameColumns = new[]
        {
            "shiftname", "name"
        };

        private static readonly string[] ShiftInTimeColumns = new[]
        {
            "onduty", "on_duty",
            "intime", "starttime", "begintime", "fromtime", "timein",
            "ontimein", "ontime_in"
        };

        private static readonly string[] ShiftOutTimeColumns = new[]
        {
            "offduty", "off_duty",
            "outtime", "endtime", "totime", "timeout",
            "ontimeout", "ontime_out"
        };

        private static readonly string[] ShiftLateMinutesColumns = new[]
        {
            "lateminutes", "late_minutes", "lategrace", "late_grace"
        };

        private static readonly string[] ShiftEarlyMinutesColumns = new[]
        {
            "earlyminutes", "early_minutes", "earlygrace", "early_grace"
        };

        private static readonly string[] WeekScheduleDayColumns = new[]
        {
            "dayid", "day_id", "dayofweek", "day"
        };

        private static readonly string[] ScheduleMonthColumns = new[]
        {
            "monthid", "month_id", "month"
        };

        private static readonly string[] TempScheduleUserColumns = new[]
        {
            "userenrollnumber", "userid", "user_id", "enrollnumber", "badgenumber"
        };

        private static readonly string[] TempScheduleStartColumns = new[]
        {
            "bdate", "startdate", "begindate", "fromdate", "sdate"
        };

        private static readonly string[] TempScheduleEndColumns = new[]
        {
            "edate", "enddate", "todate"
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
                    return ErrorResult(result, "File Excel không hợp lệ.");
                }

                var sheetData = worksheetPart.Worksheet.Elements<SheetData>().FirstOrDefault();
                if (sheetData == null)
                {
                    return ErrorResult(result, "Không tìm thấy dữ liệu trong file.");
                }

                var rows = sheetData.Elements<Row>().ToList();
                if (rows.Count == 0)
                {
                    return ErrorResult(result, "File Excel không có dữ liệu.");
                }

                var headerRowIndex = FindHeaderRow(rows, workbookPart);
                if (headerRowIndex < 0)
                {
                    return ErrorResult(result, "Không tìm thấy dòng tiêu đề hợp lệ.");
                }

                var headerMap = GetHeaderMap(rows[headerRowIndex], workbookPart);
                if (headerMap.Count == 0)
                {
                    return ErrorResult(result, "Không nhận diện được cột dữ liệu.");
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
                        AddError(result, GetRowNumber(row, i), "Ngày không hợp lệ.");
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
                        AddError(result, record.RowIndex ?? (i + 1), "Không thể lưu dữ liệu.");
                    }
                }
            }
            catch (Exception ex)
            {
                return ErrorResult(result, $"Lỗi đọc file Excel: {ex.Message}");
            }

            return result;
        }

        [SupportedOSPlatform("windows")]
        public async Task<AttendanceImportResult> SyncFromAccessAsync(DateTime fromDate, DateTime toDate, int userId)
        {
            if (!OperatingSystem.IsWindows())
            {
                var notSupported = new AttendanceImportResult();
                return ErrorResult(notSupported, "Chức năng đồng bộ Access chỉ hỗ trợ trên Windows.");
            }

            var result = new AttendanceImportResult();
            var options = GetMachineOptions();
            if (!string.IsNullOrWhiteSpace(options.DbSourceError))
            {
                return ErrorResult(result, options.DbSourceError);
            }

            if (string.IsNullOrWhiteSpace(options.DbPath))
            {
                return ErrorResult(result, "Chưa cấu hình đường dẫn file chấm công.");
            }

            if (!File.Exists(options.DbPath))
            {
                return ErrorResult(result, "Không tìm thấy file chấm công.");
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
                return ErrorResult(result, $"Không thể mở file chấm công: {error}");
            }

            using (connection)
            {
                var tables = GetTableNames(connection);
                if (tables.Count == 0)
                {
                    return ErrorResult(result, "Không tìm thấy bảng dữ liệu trong file.");
                }

                var columnsByTable = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
                foreach (var table in tables)
                {
                    columnsByTable[table] = GetColumnNames(connection, table);
                }

                var logSchema = ResolveLogTableSchema(options, columnsByTable);
                if (logSchema == null)
                {
                    return ErrorResult(result, "Không tìm thấy bảng chấm công hợp lệ.");
                }

                var userSchema = ResolveUserTableSchema(options, columnsByTable);
                var userMap = LoadUserMap(connection, userSchema);
                var scheduleContext = LoadScheduleContext(connection, columnsByTable);
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
                        return ErrorResult(result, "Không đọc được dữ liệu chấm công.");
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
                    var scheduleInfo = scheduleContext.GetScheduleInfo(aggregate.UserInfo, aggregate.WorkDate);
                    var (lateMinutes, earlyMinutes) = CalculateLateEarly(aggregate, scheduleInfo.Shift);

                    var record = BuildAttendanceRecord(
                        aggregate,
                        options.DbSourceName ?? options.DbConfiguredSource ?? options.DbPath);
                    record.LateMinutes = lateMinutes;
                    record.EarlyMinutes = earlyMinutes;
                    record.ShiftName = scheduleInfo.ShiftCode;
                    record.Symbol = scheduleInfo.WeekendSymbol;

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
                        AddError(result, result.Total, "Không thể lưu dữ liệu.");
                    }
                }
            }

            return result;
        }

        public async Task<AttendanceImportResult> SyncFromDirectSqlAsync(DateTime fromDate, DateTime toDate, int userId)
        {
            var result = new AttendanceImportResult();
            var options = GetMachineOptions();

            if (!options.UseDirectSqlRealtime || string.IsNullOrWhiteSpace(options.DirectSqlConnectionString))
            {
                return ErrorResult(result, "Chưa cấu hình nguồn chấm công direct SQL.");
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

            var sourceName = GetDirectSqlSourceName(options.DirectSqlConnectionString);
            var userMap = new Dictionary<string, MachineUserInfo>(StringComparer.OrdinalIgnoreCase);
            var aggregates = new Dictionary<string, AttendanceAggregate>(StringComparer.OrdinalIgnoreCase);
            var fromDateOnly = fromDate.Date;
            var toExclusive = toDate.Date.AddDays(1);

            try
            {
                using var connection = new SqlConnection(options.DirectSqlConnectionString);
                await connection.OpenAsync();

                using (var userCommand = new SqlCommand(@"
SELECT DeviceUserId, [Name]
FROM dbo.DeviceUsers
WHERE (@DeviceIp IS NULL OR DeviceIp = @DeviceIp);", connection))
                {
                    userCommand.Parameters.Add("@DeviceIp", SqlDbType.NVarChar, 50).Value =
                        string.IsNullOrWhiteSpace(options.DeviceIp)
                            ? DBNull.Value
                            : options.DeviceIp;

                    using var reader = await userCommand.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        var rawUserId = reader["DeviceUserId"]?.ToString()?.Trim();
                        if (string.IsNullOrWhiteSpace(rawUserId))
                        {
                            continue;
                        }

                        var info = new MachineUserInfo
                        {
                            UserId = rawUserId,
                            FingerprintCode = rawUserId,
                            Name = reader["Name"]?.ToString()?.Trim()
                        };

                        AddUserMap(userMap, rawUserId, NormalizeUserId(rawUserId), info);
                    }
                }

                using (var logCommand = new SqlCommand(@"
SELECT DeviceUserId, RecordTime
FROM dbo.DeviceAttendanceLogs
WHERE (@DeviceIp IS NULL OR DeviceIp = @DeviceIp)
  AND RecordTime >= @FromDate
  AND RecordTime < @ToDate
ORDER BY RecordTime;", connection))
                {
                    logCommand.Parameters.Add("@DeviceIp", SqlDbType.NVarChar, 50).Value =
                        string.IsNullOrWhiteSpace(options.DeviceIp)
                            ? DBNull.Value
                            : options.DeviceIp;
                    logCommand.Parameters.Add("@FromDate", SqlDbType.DateTime2).Value = fromDateOnly;
                    logCommand.Parameters.Add("@ToDate", SqlDbType.DateTime2).Value = toExclusive;

                    using var reader = await logCommand.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        var rawUserId = reader["DeviceUserId"]?.ToString();
                        if (string.IsNullOrWhiteSpace(rawUserId))
                        {
                            continue;
                        }

                        if (reader["RecordTime"] is not DateTime checkTime)
                        {
                            continue;
                        }

                        var normalizedUserId = NormalizeUserId(rawUserId);
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

                        if (aggregate.UserInfo == null && userInfo != null)
                        {
                            aggregate.UserInfo = userInfo;
                        }

                        aggregate.Update(checkTime.TimeOfDay);
                    }
                }
            }
            catch (Exception ex)
            {
                return ErrorResult(result, $"Không thể đọc dữ liệu direct SQL: {ex.Message}");
            }

            var employeeCache = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var aggregate in aggregates.Values)
            {
                var record = BuildAttendanceRecord(aggregate, sourceName);
                record.SourceFile = sourceName;

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
                    AddError(result, result.Total, "Không thể lưu dữ liệu.");
                }
            }

            return result;
        }

        public async Task<AttendanceImportResult> SyncFromDeviceAsync(DateTime fromDate, DateTime toDate, int userId)
        {
            var result = new AttendanceImportResult();
            var options = GetMachineOptions();

            if (!options.UseDirectDeviceRealtime || string.IsNullOrWhiteSpace(options.DeviceIp))
            {
                return ErrorResult(result, "ChÆ°a cáº¥u hÃ¬nh nguá»“n cháº¥m cÃ´ng káº¿t ná»‘i trá»±c tiáº¿p tá»« mÃ¡y.");
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

            var aggregates = new Dictionary<string, AttendanceAggregate>(StringComparer.OrdinalIgnoreCase);
            var rawLogs = new List<AttendanceDeviceRawLog>();
            var fromDateOnly = fromDate.Date;
            var toExclusive = toDate.Date.AddDays(1);

            try
            {
                await using var client = new AttendanceDeviceTcpClient(
                    options.DeviceIp,
                    options.DevicePort,
                    TimeSpan.FromSeconds(options.DeviceTimeoutSeconds));

                await client.ReadAttendanceLogsAsync(log =>
                {
                    if (string.IsNullOrWhiteSpace(log.UserId) ||
                        log.RecordTime < fromDateOnly ||
                        log.RecordTime >= toExclusive)
                    {
                        return;
                    }

                    var rawUserId = log.UserId.Trim();
                    rawLogs.Add(new AttendanceDeviceRawLog
                    {
                        DeviceIp = options.DeviceIp ?? string.Empty,
                        DevicePort = options.DevicePort,
                        DeviceUserId = rawUserId,
                        NormalizedUserId = NormalizeUserId(rawUserId),
                        RecordTime = log.RecordTime,
                        InOutMode = NormalizeDeviceInOutMode(log.State),
                        Source = "DIRECT_DEVICE"
                    });

                    AggregateAttendanceLog(aggregates, rawUserId, log.RecordTime, null);
                });
            }
            catch (Exception ex)
            {
                return ErrorResult(result, $"KhÃ´ng thá»ƒ Ä‘á»c dá»¯ liá»‡u trá»±c tiáº¿p tá»« mÃ¡y cháº¥m cÃ´ng: {ex.Message}");
            }

            try
            {
                await RefreshDeviceRawLogsAsync(rawLogs, options, fromDateOnly, toExclusive);
            }
            catch (Exception ex)
            {
                return ErrorResult(result, $"Khong the luu lich su quet the tu may cham cong: {ex.Message}");
            }

            await SaveAggregatesAsync(aggregates, result, GetDirectDeviceSourceName(options), userId);
            return result;
        }

        [SupportedOSPlatform("windows")]
        private async Task<BaseList> GetSummaryFromAccess(AttendanceRequest request)
        {
            if (!OperatingSystem.IsWindows())
            {
                return new BaseList();
            }

            var result = new BaseList();
            var options = GetMachineOptions();
            if (!string.IsNullOrWhiteSpace(options.DbSourceError))
            {
                return result;
            }

            if (string.IsNullOrWhiteSpace(options.DbPath))
            {
                return result;
            }

            if (!File.Exists(options.DbPath))
            {
                return result;
            }

                var (fromDate, toDate) = NormalizeDateRange(request.From, request.To);

            if (!TryOpenAccessConnection(options, out var connection, out _))
            {
                return result;
            }

            using (connection)
            {
                var tables = GetTableNames(connection);
                if (tables.Count == 0)
                {
                    return result;
                }

                var columnsByTable = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
                foreach (var table in tables)
                {
                    columnsByTable[table] = GetColumnNames(connection, table);
                }

                var logSchema = ResolveLogTableSchema(options, columnsByTable);
                if (logSchema == null)
                {
                    return result;
                }

                var userSchema = ResolveUserTableSchema(options, columnsByTable);
                var userMap = LoadUserMap(connection, userSchema);

                string? requestedFingerprint = string.IsNullOrWhiteSpace(request.FingerprintCode)
                    ? null
                    : request.FingerprintCode.Trim();
                int? requestedEmployeeId = request.EmployeeId;
                if (requestedEmployeeId.HasValue && requestedEmployeeId.Value > 0)
                {
                    var employee = await _unitOfWork.EmployeeRep.GetById(requestedEmployeeId.Value);
                    requestedFingerprint = employee?.FingerprintCode;
                }

                var scheduleContext = LoadScheduleContext(connection, columnsByTable);
                var aggregates = LoadAggregates(connection, logSchema, userMap, fromDate, toDate);
                var grouped = aggregates.Values
                    .GroupBy(a => a.FingerprintCode, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

                var items = new List<AttendanceSummaryIndexModel>();
                var token = request.Token?.Trim();
                var tokenLower = string.IsNullOrWhiteSpace(token) ? null : token.ToLowerInvariant();

                var employeeRequest = new EmployeeRequest
                {
                    Page = 1,
                    Limit = 10000,
                    UserId = request.UserId,
                    Token = token,
                    IsDeleted = false
                };

                var employeeList = await _unitOfWork.EmployeeRep.GetAll(employeeRequest);
                var employees = employeeList.Data?.OfType<EmployeeIndexModel>().ToList() ?? new List<EmployeeIndexModel>();
                var employeeMap = new Dictionary<string, EmployeeIndexModel>(StringComparer.OrdinalIgnoreCase);

                foreach (var employee in employees)
                {
                    if (string.IsNullOrWhiteSpace(employee.FingerprintCode))
                    {
                        continue;
                    }

                    var fingerprint = employee.FingerprintCode.Trim();
                    if (!employeeMap.ContainsKey(fingerprint))
                    {
                        employeeMap[fingerprint] = employee;
                    }

                    if (!string.IsNullOrWhiteSpace(requestedFingerprint) &&
                        !string.Equals(requestedFingerprint, fingerprint, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(tokenLower))
                    {
                        var fingerprintLower = fingerprint.ToLowerInvariant();
                        var nameLower = (employee.FullName ?? string.Empty).ToLowerInvariant();
                        if (!fingerprintLower.Contains(tokenLower) && !nameLower.Contains(tokenLower))
                        {
                            continue;
                        }
                    }

                    var summary = new AttendanceSummaryIndexModel
                    {
                        EmployeeId = employee.Id,
                        FingerprintCode = fingerprint,
                        FullName = employee.FullName,
                        DepartmentText = employee.DepartmentText,
                        PositionText = employee.PositionText,
                        TotalWorkDays = 0,
                        TotalWorkHours = 0,
                        TotalOvertimeHours = 0,
                        TotalHours = 0,
                        LateCount = 0,
                        LateMinutes = 0,
                        EarlyCount = 0,
                        EarlyMinutes = 0,
                        OffCount = 0
                    };

                    items.Add(summary);
                }

                foreach (var kvp in grouped)
                {
                    var fingerprint = kvp.Key;
                    var group = kvp.Value;

                    if (!string.IsNullOrWhiteSpace(requestedFingerprint) &&
                        !string.Equals(requestedFingerprint, fingerprint, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (!employeeMap.TryGetValue(fingerprint, out var employee))
                    {
                        if (!string.IsNullOrWhiteSpace(tokenLower))
                        {
                            var fingerprintLower = fingerprint.ToLowerInvariant();
                            if (!fingerprintLower.Contains(tokenLower))
                            {
                                continue;
                            }
                        }
                    }

                    var totalHours = group.Sum(x => CalculateWorkHours(x) ?? 0m);
                    var totalDays = group.Count;
                    var lateMinutesTotal = 0;
                    var earlyMinutesTotal = 0;
                    var lateCount = 0;
                    var earlyCount = 0;

                    foreach (var aggregate in group)
                    {
                        var scheduleInfo = scheduleContext.GetScheduleInfo(aggregate.UserInfo, aggregate.WorkDate);
                        var (lateMinutes, earlyMinutes) = CalculateLateEarly(aggregate, scheduleInfo.Shift);
                        if (lateMinutes > 0)
                        {
                            lateCount++;
                            lateMinutesTotal += lateMinutes;
                        }

                        if (earlyMinutes > 0)
                        {
                            earlyCount++;
                            earlyMinutesTotal += earlyMinutes;
                        }
                    }

                    var existing = items.FirstOrDefault(x => string.Equals(x.FingerprintCode, fingerprint, StringComparison.OrdinalIgnoreCase));
                    if (existing != null)
                    {
                        existing.TotalWorkDays = totalDays;
                        existing.TotalWorkHours = totalHours;
                        existing.TotalHours = totalHours;
                        existing.LateCount = lateCount;
                        existing.LateMinutes = lateMinutesTotal;
                        existing.EarlyCount = earlyCount;
                        existing.EarlyMinutes = earlyMinutesTotal;
                        continue;
                    }

                    var nameFromAccess = group.Select(x => x.EmployeeName).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
                    var fullName = employee?.FullName ?? nameFromAccess ?? string.Empty;
                    var computedId = ComputeStableEmployeeId(fingerprint);

                    items.Add(new AttendanceSummaryIndexModel
                    {
                        EmployeeId = employee?.Id ?? computedId,
                        FingerprintCode = fingerprint,
                        FullName = string.IsNullOrWhiteSpace(fullName) ? null : fullName,
                        DepartmentText = employee?.DepartmentText,
                        PositionText = employee?.PositionText,
                        TotalWorkDays = totalDays,
                        TotalWorkHours = totalHours,
                        TotalOvertimeHours = 0,
                        TotalHours = totalHours,
                        LateCount = lateCount,
                        LateMinutes = lateMinutesTotal,
                        EarlyCount = earlyCount,
                        EarlyMinutes = earlyMinutesTotal,
                        OffCount = 0
                    });
                }

                items = items
                    .OrderBy(x => x.FullName ?? x.FingerprintCode)
                    .ToList();

                result.Total = items.Count;
                result.Data = items;
                return result;
            }
        }

        [SupportedOSPlatform("windows")]
        private async Task<List<AttendanceDetailModel>> GetDetailsFromAccess(int? employeeId, string? fingerprintCode, DateTime fromDate, DateTime toDate)
        {
            if (!OperatingSystem.IsWindows())
            {
                return new List<AttendanceDetailModel>();
            }

            var options = GetMachineOptions();
            var results = new List<AttendanceDetailModel>();
            if (!string.IsNullOrWhiteSpace(options.DbSourceError))
            {
                return results;
            }

            if (string.IsNullOrWhiteSpace(options.DbPath))
            {
                return results;
            }

            if (!File.Exists(options.DbPath))
            {
                return results;
            }

            var (from, to) = NormalizeDateRange(fromDate, toDate);

            string? targetFingerprint = fingerprintCode;
            if (employeeId.HasValue && employeeId.Value > 0)
            {
                var employee = await _unitOfWork.EmployeeRep.GetById(employeeId.Value);
                targetFingerprint = employee?.FingerprintCode;
            }

            if (!TryOpenAccessConnection(options, out var connection, out _))
            {
                return results;
            }

            using (connection)
            {
                var tables = GetTableNames(connection);
                if (tables.Count == 0)
                {
                    return results;
                }

                var columnsByTable = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
                foreach (var table in tables)
                {
                    columnsByTable[table] = GetColumnNames(connection, table);
                }

                var logSchema = ResolveLogTableSchema(options, columnsByTable);
                if (logSchema == null)
                {
                    return results;
                }

                var userSchema = ResolveUserTableSchema(options, columnsByTable);
                var userMap = LoadUserMap(connection, userSchema);
                var scheduleContext = LoadScheduleContext(connection, columnsByTable);

                var aggregates = LoadAggregates(connection, logSchema, userMap, from, to);

                foreach (var aggregate in aggregates.Values)
                {
                    if (!string.IsNullOrWhiteSpace(targetFingerprint) &&
                        !string.Equals(aggregate.FingerprintCode, targetFingerprint, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (employeeId.HasValue && employeeId.Value < 0)
                    {
                        var computedId = ComputeStableEmployeeId(aggregate.FingerprintCode);
                        if (employeeId.Value != computedId)
                        {
                            continue;
                        }
                    }

                    var workHours = CalculateWorkHours(aggregate);
                    var scheduleInfo = scheduleContext.GetScheduleInfo(aggregate.UserInfo, aggregate.WorkDate);
                    var (lateMinutes, earlyMinutes) = CalculateLateEarly(aggregate, scheduleInfo.Shift);
                    results.Add(new AttendanceDetailModel
                    {
                        WorkDate = aggregate.WorkDate,
                        DayName = GetDayName(aggregate.WorkDate),
                        CheckIn = aggregate.FirstTime,
                        CheckOut = aggregate.LastTime,
                        WorkDay = aggregate.PunchCount > 0 ? 1 : 0,
                        WorkHours = workHours,
                        WorkDayPlus = 0,
                        WorkHoursPlus = 0,
                        LateMinutes = lateMinutes,
                        EarlyMinutes = earlyMinutes,
                        ShiftName = scheduleInfo.ShiftCode,
                        Symbol = scheduleInfo.WeekendSymbol,
                        SymbolPlus = null,
                        TotalHours = workHours,
                        FingerprintCode = aggregate.FingerprintCode
                    });
                }
            }

            return results.OrderBy(x => x.WorkDate).ToList();
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
            try
            {
                var resolvedSource = AttendanceMachinePathResolver.Resolve(options.DbUrl, options.DbPath);
                options.DbConfiguredSource = resolvedSource?.ConfiguredSource;
                options.DbSourceName = resolvedSource?.DisplayName;
                options.DbPath = resolvedSource?.LocalPath;
            }
            catch (Exception ex)
            {
                options.DbConfiguredSource = AttendanceMachinePathResolver.ResolveConfiguredSource(
                    options.DbUrl,
                    options.DbPath);
                options.DbSourceError = ex.Message;
                options.DbPath = null;
            }

            options.DbUrl = string.IsNullOrWhiteSpace(options.DbUrl)
                ? null
                : options.DbUrl.Trim();
            options.DeviceIp = string.IsNullOrWhiteSpace(options.DeviceIp)
                ? null
                : options.DeviceIp.Trim();
            options.DevicePort = options.DevicePort <= 0 ? 4370 : options.DevicePort;
            options.DeviceTimeoutSeconds = options.DeviceTimeoutSeconds <= 0
                ? 120
                : options.DeviceTimeoutSeconds;
            options.RealtimeSyncIntervalSeconds = options.RealtimeSyncIntervalSeconds <= 0
                ? 10
                : options.RealtimeSyncIntervalSeconds;
            options.RealtimeSyncLookbackDays = options.RealtimeSyncLookbackDays <= 0
                ? 2
                : options.RealtimeSyncLookbackDays;
            options.DirectSqlConnectionString = string.IsNullOrWhiteSpace(options.DirectSqlConnectionString)
                ? null
                : options.DirectSqlConnectionString.Trim();
            return options;
        }

        private bool UseAccessRealtime()
        {
            return _configuration.GetValue<bool>("AttendanceMachine:UseAccessRealtime");
        }

        private bool UseDirectSqlRealtime()
        {
            return _configuration.GetValue<bool>("AttendanceMachine:UseDirectSqlRealtime");
        }

        private static string GetDirectSqlSourceName(string connectionString)
        {
            try
            {
                var builder = new SqlConnectionStringBuilder(connectionString);
                return string.IsNullOrWhiteSpace(builder.InitialCatalog)
                    ? "AttendanceDirectSync"
                    : builder.InitialCatalog;
            }
            catch
            {
                return "AttendanceDirectSync";
            }
        }

        private static string GetDirectDeviceSourceName(AttendanceMachineOptions options)
        {
            var deviceIp = string.IsNullOrWhiteSpace(options.DeviceIp) ? "unknown" : options.DeviceIp.Trim();
            return $"Device_{deviceIp}_{options.DevicePort}";
        }

        [SupportedOSPlatform("windows")]
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

        [SupportedOSPlatform("windows")]
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

                var scheduleColumn = ResolveColumn(columns, options.UserScheduleColumn, ScheduleIdColumns);
                return new UserTableSchema(options.UserTable, userIdColumn, fingerprintColumn, nameColumn, scheduleColumn);
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
                var scheduleColumn = ResolveColumn(columns, options.UserScheduleColumn, ScheduleIdColumns);

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
                    best = new UserTableSchema(tableName, userIdColumn, fingerprintColumn, nameColumn, scheduleColumn);
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

        [SupportedOSPlatform("windows")]
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

            if (!string.IsNullOrWhiteSpace(schema.ScheduleColumn))
            {
                selectColumns.Add(schema.ScheduleColumn);
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
                int? scheduleId = null;

                if (!string.IsNullOrWhiteSpace(schema.FingerprintColumn))
                {
                    fingerprint = GetReaderValue(reader, index++);
                }

                if (!string.IsNullOrWhiteSpace(schema.NameColumn))
                {
                    name = GetReaderValue(reader, index++);
                }

                if (!string.IsNullOrWhiteSpace(schema.ScheduleColumn))
                {
                    scheduleId = TryGetInt(reader.GetValue(index++));
                }

                var info = new MachineUserInfo
                {
                    UserId = rawUserId.Trim(),
                    FingerprintCode = string.IsNullOrWhiteSpace(fingerprint) ? null : fingerprint.Trim(),
                    Name = string.IsNullOrWhiteSpace(name) ? null : name.Trim(),
                    ScheduleId = scheduleId
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

        private static void AggregateAttendanceLog(
            Dictionary<string, AttendanceAggregate> aggregates,
            string rawUserId,
            DateTime checkTime,
            MachineUserInfo? userInfo)
        {
            if (string.IsNullOrWhiteSpace(rawUserId))
            {
                return;
            }

            var fingerprint = userInfo?.FingerprintCode;
            if (string.IsNullOrWhiteSpace(fingerprint))
            {
                fingerprint = rawUserId;
            }

            fingerprint = fingerprint.Trim();
            if (string.IsNullOrWhiteSpace(fingerprint))
            {
                return;
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

            if (aggregate.UserInfo == null && userInfo != null)
            {
                aggregate.UserInfo = userInfo;
            }

            aggregate.Update(checkTime.TimeOfDay);
        }

        private static AttendanceRecord BuildAttendanceRecord(AttendanceAggregate aggregate, string sourcePath)
        {
            var checkIn = aggregate.FirstTime;
            var checkOut = aggregate.LastTime;
            var workHours = CalculateWorkHours(aggregate);

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

        private async Task SaveAggregatesAsync(
            Dictionary<string, AttendanceAggregate> aggregates,
            AttendanceImportResult result,
            string sourceName,
            int userId)
        {
            var employeeCache = new Dictionary<string, EmployeeResolution>(StringComparer.OrdinalIgnoreCase);

            foreach (var aggregate in aggregates.Values
                .OrderBy(item => item.WorkDate)
                .ThenBy(item => item.FingerprintCode, StringComparer.OrdinalIgnoreCase))
            {
                var record = BuildAttendanceRecord(aggregate, sourceName);
                record.SourceFile = sourceName;

                await ApplyEmployeeResolutionAsync(record, employeeCache);

                result.Total++;
                var saved = await _unitOfWork.AttendanceRep.UpsertAsync(record, userId);
                if (saved)
                {
                    result.TotalSuccess++;
                }
                else
                {
                    AddError(result, result.Total, "KhÃ´ng thá»ƒ lÆ°u dá»¯ liá»‡u.");
                }
            }
        }

        private async Task RefreshDeviceRawLogsAsync(
            List<AttendanceDeviceRawLog> rawLogs,
            AttendanceMachineOptions options,
            DateTime fromDate,
            DateTime toExclusive)
        {
            if (string.IsNullOrWhiteSpace(options.DeviceIp))
            {
                throw new InvalidOperationException("AttendanceMachine:DeviceIp is empty.");
            }

            var connectionString = GetApplicationConnectionString();
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("ConnectionStrings:stringConnect7 is empty.");
            }

            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            using (var deleteCommand = new SqlCommand(@"
DELETE FROM dbo.AttendanceDeviceLogs
WHERE DeviceIp = @DeviceIp
  AND DevicePort = @DevicePort
  AND RecordTime >= @FromDate
  AND RecordTime < @ToDate;", connection, transaction))
            {
                deleteCommand.Parameters.Add("@DeviceIp", SqlDbType.NVarChar, 50).Value = options.DeviceIp;
                deleteCommand.Parameters.Add("@DevicePort", SqlDbType.Int).Value = options.DevicePort;
                deleteCommand.Parameters.Add("@FromDate", SqlDbType.DateTime2).Value = fromDate;
                deleteCommand.Parameters.Add("@ToDate", SqlDbType.DateTime2).Value = toExclusive;
                await deleteCommand.ExecuteNonQueryAsync();
            }

            if (rawLogs.Count > 0)
            {
                var table = new DataTable();
                table.Columns.Add("DeviceIp", typeof(string));
                table.Columns.Add("DevicePort", typeof(int));
                table.Columns.Add("DeviceUserId", typeof(string));
                table.Columns.Add("NormalizedUserId", typeof(string));
                table.Columns.Add("RecordTime", typeof(DateTime));
                table.Columns.Add("InOutMode", typeof(string));
                table.Columns.Add("Source", typeof(string));

                foreach (var item in rawLogs
                    .GroupBy(log => new
                    {
                        log.DeviceIp,
                        log.DevicePort,
                        log.DeviceUserId,
                        log.RecordTime
                    })
                    .Select(group => group.First())
                    .OrderBy(log => log.RecordTime)
                    .ThenBy(log => log.DeviceUserId, StringComparer.OrdinalIgnoreCase))
                {
                    table.Rows.Add(
                        item.DeviceIp,
                        item.DevicePort,
                        item.DeviceUserId,
                        string.IsNullOrWhiteSpace(item.NormalizedUserId) ? DBNull.Value : item.NormalizedUserId,
                        item.RecordTime,
                        string.IsNullOrWhiteSpace(item.InOutMode) ? DBNull.Value : item.InOutMode,
                        item.Source);
                }

                using var bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, transaction)
                {
                    DestinationTableName = "dbo.AttendanceDeviceLogs",
                    BatchSize = 1000
                };

                bulkCopy.ColumnMappings.Add("DeviceIp", "DeviceIp");
                bulkCopy.ColumnMappings.Add("DevicePort", "DevicePort");
                bulkCopy.ColumnMappings.Add("DeviceUserId", "DeviceUserId");
                bulkCopy.ColumnMappings.Add("NormalizedUserId", "NormalizedUserId");
                bulkCopy.ColumnMappings.Add("RecordTime", "RecordTime");
                bulkCopy.ColumnMappings.Add("InOutMode", "InOutMode");
                bulkCopy.ColumnMappings.Add("Source", "Source");

                await bulkCopy.WriteToServerAsync(table);
            }

            transaction.Commit();
        }

        private string? GetApplicationConnectionString()
        {
            var connectionString = _configuration.GetConnectionString("stringConnect7");
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                return connectionString.Trim();
            }

            connectionString = _configuration.GetConnectionString("stringConnect");
            return string.IsNullOrWhiteSpace(connectionString) ? null : connectionString.Trim();
        }

        private static string? NormalizeDeviceInOutMode(byte state)
        {
            return state <= 3 ? state.ToString(CultureInfo.InvariantCulture) : null;
        }

        private async Task ApplyEmployeeResolutionAsync(
            AttendanceRecord record,
            Dictionary<string, EmployeeResolution> employeeCache)
        {
            if (string.IsNullOrWhiteSpace(record.FingerprintCode))
            {
                return;
            }

            var rawFingerprint = record.FingerprintCode.Trim();
            var normalizedFingerprint = NormalizeUserId(rawFingerprint);
            var resolution = await ResolveEmployeeResolutionAsync(rawFingerprint, normalizedFingerprint, employeeCache);

            if (resolution.EmployeeId > 0)
            {
                record.EmployeeId = resolution.EmployeeId;
            }

            if (!string.IsNullOrWhiteSpace(resolution.FingerprintCode))
            {
                record.FingerprintCode = resolution.FingerprintCode;
            }

            if (string.IsNullOrWhiteSpace(record.EmployeeName) &&
                !string.IsNullOrWhiteSpace(resolution.EmployeeName))
            {
                record.EmployeeName = resolution.EmployeeName;
            }
        }

        private async Task<EmployeeResolution> ResolveEmployeeResolutionAsync(
            string rawFingerprint,
            string normalizedFingerprint,
            Dictionary<string, EmployeeResolution> employeeCache)
        {
            if (employeeCache.TryGetValue(rawFingerprint, out var resolution))
            {
                return resolution;
            }

            if (!string.IsNullOrWhiteSpace(normalizedFingerprint) &&
                employeeCache.TryGetValue(normalizedFingerprint, out resolution))
            {
                return resolution;
            }

            Employee? employee = await _unitOfWork.EmployeeRep.GetByFingerprintCode(rawFingerprint);
            if (employee == null &&
                !string.Equals(rawFingerprint, normalizedFingerprint, StringComparison.OrdinalIgnoreCase))
            {
                employee = await _unitOfWork.EmployeeRep.GetByFingerprintCode(normalizedFingerprint);
            }

            resolution = employee != null && employee.Id > 0
                ? new EmployeeResolution
                {
                    EmployeeId = employee.Id,
                    FingerprintCode = string.IsNullOrWhiteSpace(employee.FingerprintCode)
                        ? rawFingerprint
                        : employee.FingerprintCode.Trim(),
                    EmployeeName = string.IsNullOrWhiteSpace(employee.FullName)
                        ? null
                        : employee.FullName.Trim()
                }
                : EmployeeResolution.Empty;

            employeeCache[rawFingerprint] = resolution;
            if (!string.IsNullOrWhiteSpace(normalizedFingerprint))
            {
                employeeCache[normalizedFingerprint] = resolution;
            }

            return resolution;
        }

        [SupportedOSPlatform("windows")]
        private static Dictionary<string, AttendanceAggregate> LoadAggregates(
            OleDbConnection connection,
            LogTableSchema logSchema,
            Dictionary<string, MachineUserInfo> userMap,
            DateTime fromDate,
            DateTime toDate)
        {
            var aggregates = new Dictionary<string, AttendanceAggregate>(StringComparer.OrdinalIgnoreCase);
            var fromDateOnly = fromDate.Date;
            var toExclusive = toDate.Date.AddDays(1);

            var logQuery = $"SELECT [{logSchema.UserIdColumn}], [{logSchema.TimeColumn}] FROM [{logSchema.TableName}] " +
                           $"WHERE [{logSchema.TimeColumn}] >= ? AND [{logSchema.TimeColumn}] < ? " +
                           $"ORDER BY [{logSchema.TimeColumn}]";

            using var command = new OleDbCommand(logQuery, connection);
            command.Parameters.Add(new OleDbParameter { OleDbType = OleDbType.Date, Value = fromDateOnly });
            command.Parameters.Add(new OleDbParameter { OleDbType = OleDbType.Date, Value = toExclusive });

            using var reader = command.ExecuteReader();
            if (reader == null)
            {
                return aggregates;
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

                if (aggregate.UserInfo == null && userInfo != null)
                {
                    aggregate.UserInfo = userInfo;
                }

                aggregate.Update(checkTime.TimeOfDay);
            }

            return aggregates;
        }

        private static (DateTime from, DateTime to) NormalizeDateRange(DateTime? fromDate, DateTime? toDate)
        {
            if (!fromDate.HasValue || !toDate.HasValue || fromDate.Value == DateTime.MinValue || toDate.Value == DateTime.MinValue)
            {
                var now = DateTime.Today;
                var from = new DateTime(now.Year, now.Month, 1);
                var to = from.AddMonths(1).AddDays(-1);
                return (from, to);
            }

            if (fromDate.Value > toDate.Value)
            {
                return (toDate.Value.Date, fromDate.Value.Date);
            }

            return (fromDate.Value.Date, toDate.Value.Date);
        }

        private static decimal? CalculateWorkHours(AttendanceAggregate aggregate)
        {
            if (!aggregate.FirstTime.HasValue || !aggregate.LastTime.HasValue)
            {
                return null;
            }

            var duration = aggregate.LastTime.Value - aggregate.FirstTime.Value;
            if (duration < TimeSpan.Zero)
            {
                duration = duration.Add(TimeSpan.FromDays(1));
            }

            return Math.Round((decimal)duration.TotalHours, 2);
        }

        private static (int lateMinutes, int earlyMinutes) CalculateLateEarly(AttendanceAggregate aggregate, ShiftDetail? shift)
        {
            if (shift == null ||
                !aggregate.FirstTime.HasValue ||
                !shift.InTime.HasValue)
            {
                return (0, 0);
            }

            var lateGrace = Math.Max(shift.LateGraceMinutes ?? 0, 0);
            var earlyGrace = Math.Max(shift.EarlyGraceMinutes ?? 0, 0);

            var lateMinutes = 0;
            var earlyMinutes = 0;

            var checkIn = aggregate.FirstTime.Value;
            var shiftStart = shift.InTime.Value;
            var lateDiff = (checkIn - shiftStart).TotalMinutes;
            if (lateDiff > lateGrace)
            {
                lateMinutes = (int)Math.Round(lateDiff - lateGrace);
            }

            if (!shift.OutTime.HasValue)
            {
                return (Math.Max(0, lateMinutes), 0);
            }

            var shiftEnd = shift.OutTime.Value;

            if (!aggregate.LastTime.HasValue || aggregate.PunchCount <= 1)
            {
                return (Math.Max(0, lateMinutes), 0);
            }

            var checkOut = aggregate.LastTime.Value;
            var checkOutMinutes = checkOut.TotalMinutes;
            var shiftStartMinutes = shiftStart.TotalMinutes;
            var shiftEndMinutes = shiftEnd.TotalMinutes;
            if (shiftEndMinutes <= shiftStartMinutes)
            {
                shiftEndMinutes += 24 * 60;
                if (checkOutMinutes < shiftStartMinutes)
                {
                    checkOutMinutes += 24 * 60;
                }
            }

            var earlyDiff = shiftEndMinutes - checkOutMinutes;
            if (earlyDiff > earlyGrace)
            {
                earlyMinutes = (int)Math.Round(earlyDiff - earlyGrace);
            }

            return (Math.Max(0, lateMinutes), Math.Max(0, earlyMinutes));
        }

        private static int ComputeStableEmployeeId(string fingerprint)
        {
            if (string.IsNullOrWhiteSpace(fingerprint))
            {
                return -1;
            }

            unchecked
            {
                uint hash = 2166136261;
                foreach (var ch in fingerprint.ToUpperInvariant())
                {
                    hash ^= ch;
                    hash *= 16777619;
                }

                var stable = (int)(hash % int.MaxValue);
                return stable >= 0 ? -(stable + 1) : stable;
            }
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

        private static bool TryGetTimeOfDay(object value, out TimeSpan time)
        {
            time = default;
            if (value == null || value == DBNull.Value)
            {
                return false;
            }

            if (value is TimeSpan ts)
            {
                time = ts;
                return true;
            }

            if (value is DateTime dt)
            {
                time = dt.TimeOfDay;
                return true;
            }

            if (value is double d)
            {
                time = TimeSpan.FromMinutes(d);
                return true;
            }

            if (value is int i)
            {
                time = TimeSpan.FromMinutes(i);
                return true;
            }

            if (value is short s)
            {
                time = TimeSpan.FromMinutes(s);
                return true;
            }

            if (value is long l)
            {
                time = TimeSpan.FromMinutes(l);
                return true;
            }

            if (value is decimal dec)
            {
                time = TimeSpan.FromMinutes((double)dec);
                return true;
            }

            var text = Convert.ToString(value, CultureInfo.InvariantCulture);
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            if (TimeSpan.TryParse(text, CultureInfo.InvariantCulture, out var parsed))
            {
                time = parsed;
                return true;
            }

            if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsedDate))
            {
                time = parsedDate.TimeOfDay;
                return true;
            }

            if (DateTime.TryParse(text, new CultureInfo("vi-VN"), DateTimeStyles.AssumeLocal, out parsedDate))
            {
                time = parsedDate.TimeOfDay;
                return true;
            }

            return false;
        }

        private static int? TryGetInt(object? value)
        {
            if (value == null || value == DBNull.Value)
            {
                return null;
            }

            if (value is int intValue)
            {
                return intValue;
            }

            if (value is short shortValue)
            {
                return shortValue;
            }

            if (value is long longValue)
            {
                return (int)longValue;
            }

            if (value is decimal decimalValue)
            {
                return (int)decimalValue;
            }

            if (value is double doubleValue)
            {
                return (int)doubleValue;
            }

            var text = Convert.ToString(value, CultureInfo.InvariantCulture);
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            {
                return parsed;
            }

            if (int.TryParse(text, NumberStyles.Integer, new CultureInfo("vi-VN"), out parsed))
            {
                return parsed;
            }

            return null;
        }

        private static bool? TryGetBool(object? value)
        {
            if (value == null || value == DBNull.Value)
            {
                return null;
            }

            if (value is bool boolValue)
            {
                return boolValue;
            }

            if (value is short shortValue)
            {
                return shortValue != 0;
            }

            if (value is int intValue)
            {
                return intValue != 0;
            }

            if (value is long longValue)
            {
                return longValue != 0;
            }

            var text = Convert.ToString(value, CultureInfo.InvariantCulture);
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            if (bool.TryParse(text, out var parsedBool))
            {
                return parsedBool;
            }

            if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedInt))
            {
                return parsedInt != 0;
            }

            return null;
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

        [SupportedOSPlatform("windows")]
        private static ScheduleContext LoadScheduleContext(OleDbConnection connection, Dictionary<string, List<string>> columnsByTable)
        {
            var context = new ScheduleContext();

            var scheduleTable = FindTableName(columnsByTable, "Schedule", "Schedules");
            if (!string.IsNullOrWhiteSpace(scheduleTable))
            {
                var columns = columnsByTable[scheduleTable];
                var scheduleIdColumn = ResolveColumn(columns, null, ScheduleIdColumns);
                if (!string.IsNullOrWhiteSpace(scheduleIdColumn))
                {
                    var scheduleNameColumn = ResolveColumn(columns, null, ScheduleNameColumns);
                    var absentSatColumn = ResolveColumn(columns, null, ScheduleAbsentSatColumns);
                    var absentSunColumn = ResolveColumn(columns, null, ScheduleAbsentSunColumns);

                    var selectColumns = new List<string> { scheduleIdColumn };
                    if (!string.IsNullOrWhiteSpace(scheduleNameColumn))
                    {
                        selectColumns.Add(scheduleNameColumn);
                    }

                    if (!string.IsNullOrWhiteSpace(absentSatColumn))
                    {
                        selectColumns.Add(absentSatColumn);
                    }

                    if (!string.IsNullOrWhiteSpace(absentSunColumn))
                    {
                        selectColumns.Add(absentSunColumn);
                    }

                    var selectList = string.Join(", ", selectColumns.Select(col => $"[{col}]"));
                    using var command = new OleDbCommand($"SELECT {selectList} FROM [{scheduleTable}]", connection);
                    using var reader = command.ExecuteReader();
                    if (reader != null)
                    {
                        while (reader.Read())
                        {
                            var scheduleId = TryGetInt(reader.GetValue(0));
                            if (!scheduleId.HasValue)
                            {
                                continue;
                            }

                            var index = 1;
                            string? name = null;
                            bool? absentSat = null;
                            bool? absentSun = null;

                            if (!string.IsNullOrWhiteSpace(scheduleNameColumn))
                            {
                                name = GetReaderValue(reader, index++);
                            }

                            if (!string.IsNullOrWhiteSpace(absentSatColumn))
                            {
                                absentSat = TryGetBool(reader.GetValue(index++));
                            }

                            if (!string.IsNullOrWhiteSpace(absentSunColumn))
                            {
                                absentSun = TryGetBool(reader.GetValue(index++));
                            }

                            context.Schedules[scheduleId.Value] = new ScheduleInfo
                            {
                                Name = name,
                                IsAbsentSat = absentSat,
                                IsAbsentSun = absentSun
                            };
                        }
                    }
                }
            }

            var shiftTable = FindTableName(columnsByTable, "Shifts", "Shift");
            if (!string.IsNullOrWhiteSpace(shiftTable))
            {
                var columns = columnsByTable[shiftTable];
                var shiftIdColumn = ResolveColumn(columns, null, ShiftIdColumns);
                if (!string.IsNullOrWhiteSpace(shiftIdColumn))
                {
                    var shiftCodeColumn = ResolveColumn(columns, null, ShiftCodeColumns);
                    var shiftNameColumn = ResolveColumn(columns, null, ShiftNameColumns);
                    var shiftInColumn = ResolveColumn(columns, null, ShiftInTimeColumns);
                    var shiftOutColumn = ResolveColumn(columns, null, ShiftOutTimeColumns);
                    var shiftLateColumn = ResolveColumn(columns, null, ShiftLateMinutesColumns);
                    var shiftEarlyColumn = ResolveColumn(columns, null, ShiftEarlyMinutesColumns);

                    var selectColumns = new List<string>();
                    int? AddColumn(string? columnName)
                    {
                        if (string.IsNullOrWhiteSpace(columnName))
                        {
                            return null;
                        }

                        var existingIndex = selectColumns.FindIndex(c =>
                            string.Equals(c, columnName, StringComparison.OrdinalIgnoreCase));
                        if (existingIndex >= 0)
                        {
                            return existingIndex;
                        }

                        selectColumns.Add(columnName);
                        return selectColumns.Count - 1;
                    }

                    var shiftIdIndex = AddColumn(shiftIdColumn) ?? 0;
                    var shiftCodeIndex = AddColumn(shiftCodeColumn);
                    var shiftNameIndex = AddColumn(shiftNameColumn);
                    var shiftInIndex = AddColumn(shiftInColumn);
                    var shiftOutIndex = AddColumn(shiftOutColumn);
                    var shiftLateIndex = AddColumn(shiftLateColumn);
                    var shiftEarlyIndex = AddColumn(shiftEarlyColumn);

                    var selectList = string.Join(", ", selectColumns.Select(col => $"[{col}]"));
                    using var command = new OleDbCommand($"SELECT {selectList} FROM [{shiftTable}]", connection);
                    using var reader = command.ExecuteReader();
                    if (reader != null)
                    {
                        while (reader.Read())
                        {
                            var shiftId = TryGetInt(reader.GetValue(shiftIdIndex));
                            if (!shiftId.HasValue)
                            {
                                continue;
                            }

                            var detail = new ShiftDetail
                            {
                                Code = shiftCodeIndex.HasValue ? GetReaderValue(reader, shiftCodeIndex.Value)?.Trim() : null,
                                Name = shiftNameIndex.HasValue ? GetReaderValue(reader, shiftNameIndex.Value)?.Trim() : null,
                                LateGraceMinutes = shiftLateIndex.HasValue ? TryGetInt(reader.GetValue(shiftLateIndex.Value)) : null,
                                EarlyGraceMinutes = shiftEarlyIndex.HasValue ? TryGetInt(reader.GetValue(shiftEarlyIndex.Value)) : null
                            };

                            if (shiftInIndex.HasValue && TryGetTimeOfDay(reader.GetValue(shiftInIndex.Value), out var shiftInTime))
                            {
                                detail.InTime = shiftInTime;
                            }

                            if (shiftOutIndex.HasValue && TryGetTimeOfDay(reader.GetValue(shiftOutIndex.Value), out var shiftOutTime))
                            {
                                detail.OutTime = shiftOutTime;
                            }

                            context.Shifts[shiftId.Value] = detail;
                        }
                    }
                }
            }

            var wScheduleTable = FindTableName(columnsByTable, "WSchedules", "WSchedule", "WShifts", "WShift");
            if (!string.IsNullOrWhiteSpace(wScheduleTable))
            {
                var columns = columnsByTable[wScheduleTable];
                var scheduleIdColumn = ResolveColumn(columns, null, ScheduleIdColumns);
                var dayIdColumn = ResolveColumn(columns, null, WeekScheduleDayColumns);
                var shiftIdColumn = ResolveColumn(columns, null, ShiftIdColumns);
                if (!string.IsNullOrWhiteSpace(scheduleIdColumn) &&
                    !string.IsNullOrWhiteSpace(dayIdColumn) &&
                    !string.IsNullOrWhiteSpace(shiftIdColumn))
                {
                    var selectList = string.Join(", ", new[] { scheduleIdColumn, dayIdColumn, shiftIdColumn }
                        .Select(col => $"[{col}]"));
                    using var command = new OleDbCommand($"SELECT {selectList} FROM [{wScheduleTable}]", connection);
                    using var reader = command.ExecuteReader();
                    if (reader != null)
                    {
                        while (reader.Read())
                        {
                            var scheduleId = TryGetInt(reader.GetValue(0));
                            var dayId = TryGetInt(reader.GetValue(1));
                            var shiftId = TryGetInt(reader.GetValue(2));
                            if (!scheduleId.HasValue || !dayId.HasValue || !shiftId.HasValue)
                            {
                                continue;
                            }

                            var key = (scheduleId.Value, dayId.Value);
                            context.WeekSchedules[key] = shiftId.Value;
                        }
                    }
                }
            }

            var mScheduleTable = FindTableName(columnsByTable, "MSchedules", "MSchedule", "MShifts", "MShift");
            if (!string.IsNullOrWhiteSpace(mScheduleTable))
            {
                var columns = columnsByTable[mScheduleTable];
                var scheduleIdColumn = ResolveColumn(columns, null, ScheduleIdColumns);
                var dayIdColumn = ResolveColumn(columns, null, WeekScheduleDayColumns);
                var monthIdColumn = ResolveColumn(columns, null, ScheduleMonthColumns);
                var shiftIdColumn = ResolveColumn(columns, null, ShiftIdColumns);
                if (!string.IsNullOrWhiteSpace(scheduleIdColumn) &&
                    !string.IsNullOrWhiteSpace(dayIdColumn) &&
                    !string.IsNullOrWhiteSpace(monthIdColumn) &&
                    !string.IsNullOrWhiteSpace(shiftIdColumn))
                {
                    var selectList = string.Join(", ", new[] { scheduleIdColumn, dayIdColumn, monthIdColumn, shiftIdColumn }
                        .Select(col => $"[{col}]"));
                    using var command = new OleDbCommand($"SELECT {selectList} FROM [{mScheduleTable}]", connection);
                    using var reader = command.ExecuteReader();
                    if (reader != null)
                    {
                        while (reader.Read())
                        {
                            var scheduleId = TryGetInt(reader.GetValue(0));
                            var dayId = TryGetInt(reader.GetValue(1));
                            var monthId = TryGetInt(reader.GetValue(2));
                            var shiftId = TryGetInt(reader.GetValue(3));
                            if (!scheduleId.HasValue || !dayId.HasValue || !monthId.HasValue || !shiftId.HasValue)
                            {
                                continue;
                            }

                            var key = (scheduleId.Value, monthId.Value, dayId.Value);
                            context.YearSchedules[key] = shiftId.Value;
                        }
                    }
                }
            }

            var yScheduleTable = FindTableName(columnsByTable, "YSchedules", "YSchedule", "YShifts", "YShift");
            if (!string.IsNullOrWhiteSpace(yScheduleTable))
            {
                var columns = columnsByTable[yScheduleTable];
                var scheduleIdColumn = ResolveColumn(columns, null, ScheduleIdColumns);
                var dayIdColumn = ResolveColumn(columns, null, WeekScheduleDayColumns);
                var monthIdColumn = ResolveColumn(columns, null, ScheduleMonthColumns);
                var shiftIdColumn = ResolveColumn(columns, null, ShiftIdColumns);
                if (!string.IsNullOrWhiteSpace(scheduleIdColumn) &&
                    !string.IsNullOrWhiteSpace(dayIdColumn) &&
                    !string.IsNullOrWhiteSpace(monthIdColumn) &&
                    !string.IsNullOrWhiteSpace(shiftIdColumn))
                {
                    var selectList = string.Join(", ", new[] { scheduleIdColumn, dayIdColumn, monthIdColumn, shiftIdColumn }
                        .Select(col => $"[{col}]"));
                    using var command = new OleDbCommand($"SELECT {selectList} FROM [{yScheduleTable}]", connection);
                    using var reader = command.ExecuteReader();
                    if (reader != null)
                    {
                        while (reader.Read())
                        {
                            var scheduleId = TryGetInt(reader.GetValue(0));
                            var dayId = TryGetInt(reader.GetValue(1));
                            var monthId = TryGetInt(reader.GetValue(2));
                            var shiftId = TryGetInt(reader.GetValue(3));
                            if (!scheduleId.HasValue || !dayId.HasValue || !monthId.HasValue || !shiftId.HasValue)
                            {
                                continue;
                            }

                            var key = (scheduleId.Value, monthId.Value, dayId.Value);
                            context.YearSchedules[key] = shiftId.Value;
                        }
                    }
                }
            }

            var tempScheduleTable = FindTableName(columnsByTable, "UserTempSch", "UserTempSchedule", "TempSchedule");
            if (!string.IsNullOrWhiteSpace(tempScheduleTable))
            {
                var columns = columnsByTable[tempScheduleTable];
                var userIdColumn = ResolveColumn(columns, null, TempScheduleUserColumns);
                var scheduleIdColumn = ResolveColumn(columns, null, ScheduleIdColumns);
                var startColumn = ResolveColumn(columns, null, TempScheduleStartColumns);
                var endColumn = ResolveColumn(columns, null, TempScheduleEndColumns);
                if (!string.IsNullOrWhiteSpace(userIdColumn) &&
                    !string.IsNullOrWhiteSpace(scheduleIdColumn) &&
                    !string.IsNullOrWhiteSpace(startColumn) &&
                    !string.IsNullOrWhiteSpace(endColumn))
                {
                    var selectList = string.Join(", ", new[] { userIdColumn, scheduleIdColumn, startColumn, endColumn }
                        .Select(col => $"[{col}]"));
                    using var command = new OleDbCommand($"SELECT {selectList} FROM [{tempScheduleTable}]", connection);
                    using var reader = command.ExecuteReader();
                    if (reader != null)
                    {
                        while (reader.Read())
                        {
                            var rawUserId = GetReaderValue(reader, 0);
                            if (string.IsNullOrWhiteSpace(rawUserId))
                            {
                                continue;
                            }

                            var scheduleId = TryGetInt(reader.GetValue(1));
                            if (!scheduleId.HasValue)
                            {
                                continue;
                            }

                            if (!TryGetDateTime(reader.GetValue(2), out var startDate))
                            {
                                continue;
                            }

                            if (!TryGetDateTime(reader.GetValue(3), out var endDate))
                            {
                                continue;
                            }

                            var item = new UserTempSchedule
                            {
                                UserId = rawUserId.Trim(),
                                ScheduleId = scheduleId.Value,
                                StartDate = startDate.Date,
                                EndDate = endDate.Date
                            };

                            context.AddTempSchedule(item);
                        }
                    }
                }
            }

            return context;
        }

        private static string? FindTableName(Dictionary<string, List<string>> columnsByTable, params string[] candidates)
        {
            foreach (var candidate in candidates)
            {
                var match = columnsByTable.Keys.FirstOrDefault(
                    table => string.Equals(table, candidate, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrWhiteSpace(match))
                {
                    return match;
                }
            }

            return null;
        }

        private sealed class ScheduleContext
        {
            public Dictionary<int, ScheduleInfo> Schedules { get; } = new();
            public Dictionary<int, ShiftDetail> Shifts { get; } = new();
            public Dictionary<(int scheduleId, int dayId), int> WeekSchedules { get; } = new();
            public Dictionary<(int scheduleId, int dayId), int> MonthSchedules { get; } = new();
            public Dictionary<(int scheduleId, int monthId, int dayId), int> YearSchedules { get; } = new();
            public Dictionary<string, List<UserTempSchedule>> TempSchedulesByUserId { get; } = new(StringComparer.OrdinalIgnoreCase);

            public void AddTempSchedule(UserTempSchedule item)
            {
                if (!TempSchedulesByUserId.TryGetValue(item.UserId, out var list))
                {
                    list = new List<UserTempSchedule>();
                    TempSchedulesByUserId[item.UserId] = list;
                }

                list.Add(item);
            }

            public ScheduleDisplayInfo GetScheduleInfo(MachineUserInfo? userInfo, DateTime workDate)
            {
                if (userInfo == null)
                {
                    return ScheduleDisplayInfo.Empty;
                }

                var scheduleId = ResolveScheduleId(userInfo, workDate);
                if (!scheduleId.HasValue)
                {
                    return ScheduleDisplayInfo.Empty;
                }

                var shiftId = ResolveShiftId(scheduleId.Value, workDate);
                if (shiftId.HasValue &&
                    Shifts.TryGetValue(shiftId.Value, out var shift))
                {
                    return new ScheduleDisplayInfo
                    {
                        ShiftCode = GetShiftDisplayName(shift),
                        WeekendSymbol = GetWeekendSymbol(scheduleId.Value, workDate),
                        Shift = shift
                    };
                }

                if (scheduleId.HasValue && Shifts.TryGetValue(scheduleId.Value, out var directShift))
                {
                    return new ScheduleDisplayInfo
                    {
                        ShiftCode = GetShiftDisplayName(directShift),
                        WeekendSymbol = GetWeekendSymbol(scheduleId.Value, workDate),
                        Shift = directShift
                    };
                }

                return new ScheduleDisplayInfo
                {
                    ShiftCode = null,
                    WeekendSymbol = GetWeekendSymbol(scheduleId.Value, workDate),
                    Shift = null
                };
            }

            private int? ResolveScheduleId(MachineUserInfo userInfo, DateTime workDate)
            {
                var scheduleId = userInfo.ScheduleId;
                var userId = userInfo.UserId;
                if (!string.IsNullOrWhiteSpace(userId) &&
                    TempSchedulesByUserId.TryGetValue(userId, out var list))
                {
                    var date = workDate.Date;
                    var overrideSchedule = list.FirstOrDefault(item =>
                        item.StartDate <= date && item.EndDate >= date);
                    if (overrideSchedule != null)
                    {
                        return overrideSchedule.ScheduleId;
                    }
                }

                return scheduleId;
            }

            private string? GetWeekendSymbol(int scheduleId, DateTime workDate)
            {
                if (!Schedules.TryGetValue(scheduleId, out var schedule))
                {
                    return null;
                }

                if (workDate.DayOfWeek == DayOfWeek.Saturday && schedule.IsAbsentSat == true)
                {
                    return "T7";
                }

                if (workDate.DayOfWeek == DayOfWeek.Sunday && schedule.IsAbsentSun == true)
                {
                    return "CN";
                }

                return null;
            }

            private static int ToScheduleDayId(DayOfWeek dayOfWeek)
            {
                return dayOfWeek == DayOfWeek.Sunday ? 1 : ((int)dayOfWeek + 1);
            }

            private int? ResolveShiftId(int scheduleId, DateTime workDate)
            {
                var dayId = ToScheduleDayId(workDate.DayOfWeek);
                var monthId = workDate.Month;

                if (YearSchedules.TryGetValue((scheduleId, monthId, dayId), out var yearShiftId))
                {
                    return yearShiftId;
                }

                if (MonthSchedules.TryGetValue((scheduleId, dayId), out var monthShiftId))
                {
                    return monthShiftId;
                }

                if (WeekSchedules.TryGetValue((scheduleId, dayId), out var weekShiftId))
                {
                    return weekShiftId;
                }

                return null;
            }

            private static string? GetShiftDisplayName(ShiftDetail shift)
            {
                if (!string.IsNullOrWhiteSpace(shift.Code))
                {
                    return shift.Code;
                }

                return string.IsNullOrWhiteSpace(shift.Name) ? null : shift.Name;
            }
        }

        private sealed class ScheduleInfo
        {
            public string? Name { get; set; }
            public bool? IsAbsentSat { get; set; }
            public bool? IsAbsentSun { get; set; }
        }

        private sealed class ScheduleDisplayInfo
        {
            public static ScheduleDisplayInfo Empty { get; } = new();
            public string? ShiftCode { get; set; }
            public string? WeekendSymbol { get; set; }
            public ShiftDetail? Shift { get; set; }
        }

        private sealed class ShiftDetail
        {
            public string? Code { get; set; }
            public string? Name { get; set; }
            public TimeSpan? InTime { get; set; }
            public TimeSpan? OutTime { get; set; }
            public int? LateGraceMinutes { get; set; }
            public int? EarlyGraceMinutes { get; set; }
        }

        private sealed class UserTempSchedule
        {
            public string UserId { get; set; } = string.Empty;
            public int ScheduleId { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
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
            public UserTableSchema(string tableName, string userIdColumn, string? fingerprintColumn, string? nameColumn, string? scheduleColumn)
            {
                TableName = tableName;
                UserIdColumn = userIdColumn;
                FingerprintColumn = fingerprintColumn;
                NameColumn = nameColumn;
                ScheduleColumn = scheduleColumn;
            }

            public string TableName { get; }
            public string UserIdColumn { get; }
            public string? FingerprintColumn { get; }
            public string? NameColumn { get; }
            public string? ScheduleColumn { get; }
        }

        private sealed class MachineUserInfo
        {
            public string? UserId { get; set; }
            public string? FingerprintCode { get; set; }
            public string? Name { get; set; }
            public int? ScheduleId { get; set; }
        }

        private sealed class EmployeeResolution
        {
            public static EmployeeResolution Empty { get; } = new();
            public int EmployeeId { get; set; }
            public string? FingerprintCode { get; set; }
            public string? EmployeeName { get; set; }
        }

        private sealed class AttendanceDeviceRawLog
        {
            public string DeviceIp { get; set; } = string.Empty;
            public int DevicePort { get; set; }
            public string DeviceUserId { get; set; } = string.Empty;
            public string? NormalizedUserId { get; set; }
            public DateTime RecordTime { get; set; }
            public string? InOutMode { get; set; }
            public string Source { get; set; } = string.Empty;
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
            public MachineUserInfo? UserInfo { get; set; }
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
