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
                // For regular employees, only show their own requests
                int? filterEmployeeId = null;
                if (UserData.RoleCode != "1") // Not Admin
                {
                    filterEmployeeId = UserData.UserId;
                }

                LeaveList = await _leaveBusiness.GetLeaveList(filterEmployeeId, null, null, null, page, limit);
            }

            LeaveTypes = await _masterDataBusiness.GetallByTypeData(30);
            var managers = await _employeeBusiness.GetAllManager();
            EmployeeList = managers.Data?.Cast<ManagerLeadIndex>().ToList() ?? new List<ManagerLeadIndex>();
        }

        public async Task<IActionResult> OnGetLeaveListAsync(int page = 1, int limit = 20)
        {
            GetInfoUser();
            int? filterEmployeeId = null;
            if (UserData.RoleCode != "1")
            {
                filterEmployeeId = UserData.UserId;
            }
            var result = await _leaveBusiness.GetLeaveList(filterEmployeeId, null, null, null, page, limit);
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
            return new JsonResult(new { success = result > 0, id = result });
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
