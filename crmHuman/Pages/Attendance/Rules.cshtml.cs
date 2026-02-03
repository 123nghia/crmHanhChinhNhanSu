using crmHuman.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Versioning;
using System.Threading.Tasks;

namespace crmHuman.Pages.Attendance
{
    public class RulesModel : BaseModel2
    {
        private readonly AccessAttendanceReader _reader;

        public RulesModel(IConfiguration configuration)
        {
            _reader = new AccessAttendanceReader(configuration);
            KeyPage = "Attendance";
            TitlePage = "Quy t\u1EAFc \u0111i mu\u1ED9n / v\u1EC1 s\u1EDBm / OT";
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
                    BuildQuery("Shifts", new[]
                    {
                        "ShiftID",
                        "ShiftCode",
                        "ShiftName",
                        "InTime",
                        "OutTime",
                        "LateMinutes",
                        "EarlyMinutes",
                        "OverTime",
                        "WorkMinutes"
                    })
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
                    ["ShiftID"] = "M\u00E3 ca",
                    ["ShiftCode"] = "K\u00FD hi\u1EC7u ca",
                    ["ShiftName"] = "T\u00EAn ca",
                    ["InTime"] = "Gi\u1EDD v\u00E0o",
                    ["OutTime"] = "Gi\u1EDD ra",
                    ["LateMinutes"] = "Tr\u1EC5 (ph\u00FAt)",
                    ["EarlyMinutes"] = "S\u1EDBm (ph\u00FAt)",
                    ["OverTime"] = "OT (ph\u00FAt)",
                    ["WorkMinutes"] = "Ph\u00FAt c\u00F4ng"
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
