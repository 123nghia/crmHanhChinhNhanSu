using crmHuman.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Versioning;
using System.Threading.Tasks;

namespace crmHuman.Pages.Attendance
{
    public class AbsentModel : BaseModel2
    {
        private readonly IConfiguration _configuration;
        private readonly AccessAttendanceReader _reader;

        public AbsentModel(IConfiguration configuration)
        {
            _configuration = configuration;
            _reader = new AccessAttendanceReader(configuration);
            KeyPage = "Attendance";
            TitlePage = "Ngh\u1EC9 ph\u00E9p / k\u00FD hi\u1EC7u c\u00F4ng / ng\u00E0y l\u1EC5";
        }

        public List<AccessTableData> Tables { get; private set; } = new List<AccessTableData>();
        public string? ErrorMessage { get; private set; }
        public string? SearchToken { get; private set; }
        public int PageSize { get; private set; } = 50;

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

            if (_configuration.GetValue<bool>("AttendanceMachine:UseAccessRealtime"))
            {
                ErrorMessage = "He thong chi dong bo nen tu file MDB vao SQL. Man hinh nay khong doc truc tiep Access de tranh loi OLEDB.";
                return Task.FromResult<IActionResult>(Page());
            }

            if (_configuration.GetValue<bool>("AttendanceMachine:UseDirectSqlRealtime"))
            {
                ErrorMessage = "Ch\u1EBF \u0111\u1ED9 k\u1EBFt n\u1ED1i tr\u1EF1c ti\u1EBFp ch\u1EC9 \u0111\u1ED3ng b\u1ED9 log ch\u1EA5m c\u00F4ng. D\u1EEF li\u1EC7u ph\u00E9p/l\u1EC5 t\u1EEB WiseEye hi\u1EC7n kh\u00F4ng kh\u1EA3 d\u1EE5ng.";
                return Task.FromResult<IActionResult>(Page());
            }

            if (!OperatingSystem.IsWindows())
            {
                ErrorMessage = "Ch\u1EC9 h\u1ED7 tr\u1EE3 \u0111\u1ECDc d\u1EEF li\u1EC7u Access tr\u00EAn Windows.";
                return Task.FromResult<IActionResult>(Page());
            }

            SearchToken = Request.Query["q"];
            PageSize = ParseInt(Request.Query["ps"], 50);

            ErrorMessage = _reader.GetConfigError();
            if (string.IsNullOrWhiteSpace(ErrorMessage))
            {
                Tables = _reader.LoadTables(new[]
                {
                    BuildQuery("Absent", new[] { "AbsentID", "AbsentName", "Symbol", "WorkMinutes" }),
                    BuildQuery("AbsentSymbol", new[] { "Symbol", "SymbolName", "Value" }),
                    BuildQuery("Holiday", new[] { "HolidayName", "HolidayDate", "FromDate", "ToDate" }),
                    BuildQuery("Weekend", new[] { "WeekendName", "FromDate", "ToDate" })
                }, 5000);
                AttendanceTableFormatter.NormalizeTables(Tables);
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
                    ["AbsentID"] = "M\u00E3 ngh\u1EC9",
                    ["AbsentName"] = "T\u00EAn ngh\u1EC9",
                    ["Symbol"] = "K\u00FD hi\u1EC7u",
                    ["WorkMinutes"] = "Ph\u00FAt c\u00F4ng",
                    ["SymbolName"] = "T\u00EAn k\u00FD hi\u1EC7u",
                    ["Value"] = "Gi\u00E1 tr\u1ECB",
                    ["HolidayName"] = "T\u00EAn ng\u00E0y l\u1EC5",
                    ["HolidayDate"] = "Ng\u00E0y l\u1EC5",
                    ["FromDate"] = "T\u1EEB ng\u00E0y",
                    ["ToDate"] = "\u0110\u1EBFn ng\u00E0y",
                    ["WeekendName"] = "T\u00EAn ng\u00E0y ngh\u1EC9"
                },
                SearchToken = SearchToken,
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
    }
}
