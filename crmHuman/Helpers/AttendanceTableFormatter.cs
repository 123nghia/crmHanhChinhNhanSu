using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace crmHuman.Helpers
{
    public static class AttendanceTableFormatter
    {
        private static readonly Dictionary<string, string> TableNames = new(StringComparer.OrdinalIgnoreCase)
        {
            ["CheckInOut"] = "L\u1ECBch s\u1EED ch\u1EA5m c\u00F4ng",
            ["EndCycle"] = "K\u1EBFt th\u00FAc chu k\u1EF3",
            ["DelInOut"] = "L\u1ECBch s\u1EED \u0111\u00E3 xo\u00E1",
            ["InOut"] = "Qu\u1EB9t th\u1EBB",
            ["InOutArr"] = "Qu\u1EB9t th\u1EBB (m\u1EA3ng)",
            ["Shifts"] = "Ca l\u00E0m vi\u1EC7c",
            ["Schedule"] = "L\u1ECBch l\u00E0m vi\u1EC7c",
            ["MSchedules"] = "L\u1ECBch theo th\u00E1ng",
            ["WSchedules"] = "L\u1ECBch theo tu\u1EA7n",
            ["YSchedules"] = "L\u1ECBch theo n\u0103m",
            ["UserTempSch"] = "L\u1ECBch t\u1EA1m theo nh\u00E2n vi\u00EAn",
            ["Absent"] = "Danh m\u1EE5c ngh\u1EC9 ph\u00E9p",
            ["AbsentSymbol"] = "K\u00FD hi\u1EC7u c\u00F4ng",
            ["Holiday"] = "Ng\u00E0y l\u1EC5",
            ["Weekend"] = "Ng\u00E0y ngh\u1EC9 cu\u1ED1i tu\u1EA7n"
        };

        public static void NormalizeTables(List<AccessTableData> tables)
        {
            foreach (var table in tables)
            {
                table.DisplayName = TableNames.TryGetValue(table.Name, out var display) ? display : table.Name;
                NormalizeRows(table);
            }
        }

        private static void NormalizeRows(AccessTableData table)
        {
            if (table.Rows.Count == 0 || table.Columns.Count == 0)
            {
                return;
            }

            for (var rowIndex = 0; rowIndex < table.Rows.Count; rowIndex++)
            {
                var row = table.Rows[rowIndex];
                for (var colIndex = 0; colIndex < table.Columns.Count; colIndex++)
                {
                    if (colIndex >= row.Count)
                    {
                        continue;
                    }

                    var column = table.Columns[colIndex];
                    var value = row[colIndex];
                    row[colIndex] = FormatValue(table.Name, column, value);
                }
            }
        }

        private static string FormatValue(string tableName, string column, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            if (column.Equals("DayID", StringComparison.OrdinalIgnoreCase))
            {
                if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var dayId))
                {
                    return dayId switch
                    {
                        1 => "Ch\u1EE7 nh\u1EADt",
                        2 => "Th\u1EE9 2",
                        3 => "Th\u1EE9 3",
                        4 => "Th\u1EE9 4",
                        5 => "Th\u1EE9 5",
                        6 => "Th\u1EE9 6",
                        7 => "Th\u1EE9 7",
                        _ => value
                    };
                }
            }

            if (column.Equals("MonthID", StringComparison.OrdinalIgnoreCase))
            {
                if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var monthId) &&
                    monthId >= 1 && monthId <= 12)
                {
                    return $"Th\u00E1ng {monthId}";
                }
            }

            if (column.Equals("InOutMode", StringComparison.OrdinalIgnoreCase))
            {
                return value switch
                {
                    "0" => "V\u00E0o",
                    "1" => "Ra",
                    "2" => "V\u00E0o",
                    "3" => "Ra",
                    _ => value
                };
            }

            if (column.Equals("IsAbsentSat", StringComparison.OrdinalIgnoreCase) ||
                column.Equals("IsAbsentSun", StringComparison.OrdinalIgnoreCase))
            {
                return NormalizeBool(value);
            }

            if (value.Equals("True", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("False", StringComparison.OrdinalIgnoreCase))
            {
                return NormalizeBool(value);
            }

            if (column.Equals("WorkMinutes", StringComparison.OrdinalIgnoreCase) ||
                column.Equals("LateMinutes", StringComparison.OrdinalIgnoreCase) ||
                column.Equals("EarlyMinutes", StringComparison.OrdinalIgnoreCase) ||
                column.Equals("OverTime", StringComparison.OrdinalIgnoreCase))
            {
                if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var minutes))
                {
                return $"{minutes:0.#} ph\u00FAt";
            }
            }

            if (column.Equals("TimeStr", StringComparison.OrdinalIgnoreCase) ||
                column.Equals("InTime", StringComparison.OrdinalIgnoreCase) ||
                column.Equals("OutTime", StringComparison.OrdinalIgnoreCase) ||
                column.Equals("HolidayDate", StringComparison.OrdinalIgnoreCase) ||
                column.Equals("FromDate", StringComparison.OrdinalIgnoreCase) ||
                column.Equals("ToDate", StringComparison.OrdinalIgnoreCase) ||
                column.Equals("BDate", StringComparison.OrdinalIgnoreCase) ||
                column.Equals("EDate", StringComparison.OrdinalIgnoreCase))
            {
                if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var date))
                {
                    return date.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
                }
            }

            return value;
        }

        private static string NormalizeBool(string value)
        {
            if (value.Equals("True", StringComparison.OrdinalIgnoreCase) || value == "1")
            {
                return "C\u00F3";
            }

            if (value.Equals("False", StringComparison.OrdinalIgnoreCase) || value == "0")
            {
                return "Kh\u00F4ng";
            }

            return value;
        }
    }
}
