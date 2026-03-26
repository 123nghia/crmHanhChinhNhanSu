using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using VS.Human.Business;
using VS.Human.Rep.Model;
using VS.Human.Item;

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

        public async Task OnGetAsync(int page = 1, int limit = 20)
        {
            GetInfoUser();
            if (!(Permision.View ?? false))
            {
                // Handle unauthorized access if needed, or rely on layout hiding links
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
            var result = await _leaveBusiness.GetLeaveById(id);
            return new JsonResult(result);
        }

        public async Task<IActionResult> OnGetLeaveHistoryAsync(int id)
        {
            var result = await _leaveBusiness.GetLeaveHistory(id);
            return new JsonResult(result);
        }

        public async Task<IActionResult> OnPostSaveAsync([FromBody] LeaveAddUpdate model)
        {
            GetInfoUser();
            if (!(Permision.Add ?? false) && model.Id == 0) return new JsonResult(new { success = false, message = "No permission to add" });
            if (!(Permision.Edit ?? false) && model.Id > 0) return new JsonResult(new { success = false, message = "No permission to edit" });

            if (model.EmployeeId == 0)
            {
                model.EmployeeId = UserData.UserId;
            }

            var result = await _leaveBusiness.CreateOrUpdateLeave(model, UserData.UserId);
            if (result > 0)
            {
                return new JsonResult(new { success = true, id = result });
            }

            var message = "Co loi xay ra";
            if (result == -1)
            {
                message = "Ngay bat dau khong duoc lon hon ngay ket thuc.";
            }
            else if (result == -2)
            {
                message = "Nghi phep nam phai dang ky truoc it nhat 1 ngay.";
            }

            return new JsonResult(new { success = false, message });
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            GetInfoUser();
            if (!(Permision.Delete ?? false)) return new JsonResult(new { success = false, message = "No permission to delete" });

            var result = await _leaveBusiness.DeleteLeave(id, UserData.UserId);
            return new JsonResult(new { success = result });
        }
    }
}
