using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VS.Human.Business;
using VS.Human.Item;
using VS.Human.Rep.Model;

namespace crmHuman.Pages.Leave
{
    public class LeaveRequestModel : BaseModel2
    {
        private readonly ILeaveBusiness _leaveBusiness;
        private readonly ImasterDataBussiness _masterDataBusiness;
        private readonly IEmpBusiness _employeeBusiness;
        private const string RoleHcns = "9";

        public LeaveRequestModel(ILeaveBusiness leaveBusiness, ImasterDataBussiness masterDataBusiness, IEmpBusiness employeeBusiness)
        {
            _leaveBusiness = leaveBusiness;
            _masterDataBusiness = masterDataBusiness;
            _employeeBusiness = employeeBusiness;
            KeyPage = "LeaveRequest";
            TitlePage = "Quản lý nghỉ phép";
        }

        public BaseList LeaveList { get; set; }
        public List<VS.Human.Rep.Model.MasterData> LeaveTypes { get; set; }
        public List<ManagerLeadIndex> EmployeeList { get; set; }
        public LeaveBalanceIndexModel LeaveBalance { get; set; }

        private static bool IsManagerRole(string? roleCode)
        {
            return roleCode == "3";
        }

        private static bool IsAdminRole(string? roleCode)
        {
            return roleCode == "1" || roleCode == "8" || roleCode == "9";
        }

        private (int? employeeId, int? userId) ResolveLeaveScope()
        {
            var userId = UserData?.UserId > 0 ? UserData.UserId : (int?)null;
            var roleCode = UserData?.RoleCode;

            if (!IsManagerRole(roleCode) && !IsAdminRole(roleCode))
            {
                return (userId, userId);
            }

            return (null, userId);
        }

        private async Task<bool> CanAccessLeaveAsync(LeaveIndexModel? leave)
        {
            if (leave == null || leave.Id <= 0 || UserData?.UserId <= 0)
            {
                return false;
            }

            if (leave.EmployeeId == UserData.UserId || IsAdminRole(UserData.RoleCode))
            {
                return true;
            }

            if (!IsManagerRole(UserData.RoleCode))
            {
                return false;
            }

            var scoped = await _leaveBusiness.GetLeaveList(null, null, null, null, 1, 5000, UserData.UserId);
            var items = scoped.Data?.OfType<LeaveIndexModel>() ?? Enumerable.Empty<LeaveIndexModel>();
            return items.Any(item => item.Id == leave.Id);
        }

        public async Task OnGetAsync(int page = 1, int limit = 20)
        {
            GetInfoUser();
            if (!(Permision.View ?? false))
            {
                LeaveList = new BaseList { Data = new List<object>(), Total = 0 };
            }
            else
            {
                var scope = ResolveLeaveScope();
                LeaveList = await _leaveBusiness.GetLeaveList(scope.employeeId, null, null, null, page, limit, scope.userId);
            }

            if (UserData.UserId > 0)
            {
                LeaveBalance = await _leaveBusiness.GetEmployeeLeaveBalance(UserData.UserId);
            }

            LeaveTypes = (await _masterDataBusiness.GetallByTypeData(30))
                ?.Where(t => t.Code == "NP" || t.Code == "NKL")
                .ToList()
                ?? new List<VS.Human.Rep.Model.MasterData>();

            var managers = await _employeeBusiness.GetAllManager();
            var handoverEmployees = managers.Data?.Cast<ManagerLeadIndex>().ToList() ?? new List<ManagerLeadIndex>();

            var hcnsSource = await _employeeBusiness.GetByRoleCodes(new[] { RoleHcns });
            var hcnsLookup = hcnsSource
                .Where(x => x.Id > 0)
                .GroupBy(x => x.Id)
                .ToDictionary(g => g.Key, g => g.First(), EqualityComparer<int>.Default);

            foreach (var employee in handoverEmployees)
            {
                if (hcnsLookup.ContainsKey(employee.Id))
                {
                    employee.RoleLabel = "HCNS";
                }
            }

            var hcnsEmployees = hcnsLookup.Values
                .Select(x => new ManagerLeadIndex
                {
                    Id = x.Id,
                    FullName = x.FullName ?? string.Empty,
                    UserName = x.UserName ?? string.Empty,
                    RoleLabel = "HCNS"
                })
                .ToList()
                ?? new List<ManagerLeadIndex>();

            EmployeeList = handoverEmployees
                .Concat(hcnsEmployees)
                .Where(x => x.Id > 0 && x.Id != UserData.UserId)
                .GroupBy(x => x.Id)
                .Select(g => g
                    .OrderByDescending(x => !string.IsNullOrWhiteSpace(x.RoleLabel))
                    .ThenBy(x => x.FullName)
                    .First())
                .OrderBy(x => x.FullName)
                .ToList();
        }

        public async Task<IActionResult> OnGetLeaveListAsync(int page = 1, int limit = 20)
        {
            GetInfoUser();
            var scope = ResolveLeaveScope();
            var result = await _leaveBusiness.GetLeaveList(scope.employeeId, null, null, null, page, limit, scope.userId);
            return new JsonResult(result);
        }

        public async Task<IActionResult> OnGetLeaveByIdAsync(int id)
        {
            GetInfoUser();
            if (!(Permision.View ?? false))
            {
                return new JsonResult(new { success = false, message = "Forbidden" });
            }

            var result = await _leaveBusiness.GetLeaveById(id);
            if (!await CanAccessLeaveAsync(result))
            {
                return new JsonResult(new { success = false, message = "Forbidden" });
            }

            return new JsonResult(result);
        }

        public async Task<IActionResult> OnGetLeaveHistoryAsync(int id)
        {
            GetInfoUser();
            if (!(Permision.View ?? false))
            {
                return new JsonResult(new { success = false, message = "Forbidden" });
            }

            var leave = await _leaveBusiness.GetLeaveById(id);
            if (!await CanAccessLeaveAsync(leave))
            {
                return new JsonResult(new { success = false, message = "Forbidden" });
            }

            var result = await _leaveBusiness.GetLeaveHistory(id);
            return new JsonResult(result);
        }

        public async Task<IActionResult> OnPostSaveAsync([FromBody] LeaveAddUpdate model)
        {
            GetInfoUser();
            if (!(Permision.Add ?? false) && model.Id == 0)
            {
                return new JsonResult(new { success = false, message = "Bạn không có quyền tạo đơn nghỉ phép." });
            }

            if (!(Permision.Edit ?? false) && model.Id > 0)
            {
                return new JsonResult(new { success = false, message = "Bạn không có quyền chỉnh sửa đơn nghỉ phép." });
            }

            var effectiveEmployeeId = UserData.UserId;
            if (model.Id > 0)
            {
                var existing = await _leaveBusiness.GetLeaveById(model.Id);
                if (!await CanAccessLeaveAsync(existing))
                {
                    return new JsonResult(new { success = false, message = "Bạn không có quyền xử lý đơn nghỉ phép này." });
                }

                effectiveEmployeeId = existing.EmployeeId;
            }

            model.EmployeeId = effectiveEmployeeId;

            var result = await _leaveBusiness.CreateOrUpdateLeave(model, UserData.UserId);
            if (result > 0)
            {
                return new JsonResult(new { success = true, id = result });
            }

            var message = "Có lỗi xảy ra.";
            if (result == -1)
            {
                message = "Ngày bắt đầu không được lớn hơn ngày kết thúc.";
            }
            else if (result == -2)
            {
                message = "Nghỉ phép năm phải đăng ký trước ít nhất 1 ngày.";
            }
            else if (result == -3)
            {
                message = "Đơn nghỉ phép này đã tồn tại trên hệ thống. Vui lòng kiểm tra lại danh sách trước khi lưu.";
            }
            else if (result == -4)
            {
                message = "Hệ thống đang xử lý một yêu cầu trùng lặp. Vui lòng thử lại sau ít giây.";
            }

            return new JsonResult(new { success = false, message });
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            GetInfoUser();
            if (!(Permision.Delete ?? false))
            {
                return new JsonResult(new { success = false, message = "Bạn không có quyền xóa đơn nghỉ phép." });
            }

            var result = await _leaveBusiness.DeleteLeave(id, UserData.UserId);
            return new JsonResult(new { success = result });
        }

        public async Task<IActionResult> OnPostExportAsync()
        {
            if (!HttpContext.User.Identity.IsAuthenticated)
            {
                return Redirect("/Login");
            }

            GetInfoUser();
            if (!(Permision.View ?? false))
            {
                return new JsonResult(new { success = false, message = "Bạn không có quyền xuất dữ liệu." });
            }

            var scope = ResolveLeaveScope();
            var leaveList = await _leaveBusiness.GetLeaveList(scope.employeeId, null, null, null, 1, 5000, scope.userId);
            var items = leaveList.Data?.OfType<LeaveIndexModel>().ToList() ?? new List<LeaveIndexModel>();

            if (items.Count == 0)
            {
                var errorBytes = global::System.Text.Encoding.UTF8.GetBytes("Không có dữ liệu để xuất.");
                return File(errorBytes, "text/plain", "Khong_co_du_lieu.txt");
            }

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Nghỉ phép");

            var headers = new[]
            {
                "STT",
                "Người tạo",
                "Ngày tạo",
                "Loại nghỉ",
                "Từ ngày",
                "Đến ngày",
                "Số ngày",
                "Người nhận bàn giao",
                "Lý do",
                "Trạng thái",
                "Người phê duyệt cuối"
            };

            worksheet.Cells[1, 1, 1, headers.Length].Merge = true;
            worksheet.Cells[1, 1].Value = "DỮ LIỆU NGHỈ PHÉP";
            worksheet.Cells[2, 1, 2, headers.Length].Merge = true;
            worksheet.Cells[2, 1].Value = $"Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}";
            worksheet.Cells[1, 1, 2, headers.Length].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            worksheet.Cells[1, 1, 2, headers.Length].Style.Font.Bold = true;

            const int headerRow = 4;
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cells[headerRow, i + 1].Value = headers[i];
            }

            worksheet.Cells[headerRow, 1, headerRow, headers.Length].Style.Font.Bold = true;
            worksheet.Cells[headerRow, 1, headerRow, headers.Length].Style.Fill.PatternType = ExcelFillStyle.Solid;
            worksheet.Cells[headerRow, 1, headerRow, headers.Length].Style.Fill.BackgroundColor.SetColor(global::System.Drawing.Color.LightGray);

            var row = headerRow + 1;
            var stt = 1;
            foreach (var item in items)
            {
                worksheet.Cells[row, 1].Value = stt++;
                worksheet.Cells[row, 2].Value = item.EmployeeName;
                worksheet.Cells[row, 3].Value = item.CreateAt.ToString("dd/MM/yyyy");
                worksheet.Cells[row, 4].Value = item.LeaveTypeName;
                worksheet.Cells[row, 5].Value = item.FromDate.ToString("dd/MM/yyyy");
                worksheet.Cells[row, 6].Value = item.ToDate.ToString("dd/MM/yyyy");
                worksheet.Cells[row, 7].Value = item.NumDays;
                worksheet.Cells[row, 8].Value = item.HandoverEmployeeName;
                worksheet.Cells[row, 9].Value = item.Reason;
                worksheet.Cells[row, 10].Value = GetLeaveStatusText(item.Status);
                worksheet.Cells[row, 11].Value = item.ApproverName;
                row++;
            }

            var dataRange = worksheet.Cells[headerRow, 1, row - 1, headers.Length];
            dataRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            dataRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            dataRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            dataRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

            var fileBytes = package.GetAsByteArray();
            var fileName = $"DuLieuNghiPhep_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        private static string GetLeaveStatusText(int status)
        {
            return status switch
            {
                0 => "Chờ quản lý trực tiếp phê duyệt",
                1 => "Chờ HCNS phê duyệt",
                2 => "Chờ BGĐ phê duyệt",
                3 => "Đã phê duyệt",
                4 => "Đã phê duyệt (HCNS duyệt thay)",
                5 => "Từ chối",
                6 => "Đã hủy",
                _ => "Không xác định"
            };
        }
    }
}
