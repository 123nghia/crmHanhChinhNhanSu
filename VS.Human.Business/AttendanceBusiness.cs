using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using VS.Human.Business.Imp;
using VS.Human.Business.Helpers;
using VS.Human.Item;
using VS.Human.Rep;
using VS.Human.Rep.Model;

namespace VS.Human.Business
{
    public class AttendanceBusiness : BaseBusiness, IAttendanceBusiness
    {
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

        public AttendanceBusiness(IUnitOfWork unitOfWork, IHttpContextAccessor contextAccessor)
            : base(unitOfWork, contextAccessor)
        {
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
    }
}
