using crmHuman.Helpers;
using crmHuman.Model;
using crmHuman.DisplayModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VS.Human.Business;
using VS.Human.Item;
using VS.Human.Rep.Model;

namespace crmHuman.Pages.Leave
{
    public class LeaveBalanceModel : BaseModel2
    {
        private readonly ILeaveBalanceBusiness _leaveBalanceBusiness;
        private readonly ILeaveBusiness _leaveBusiness;
        private readonly IEmpBusiness _employeeBusiness;
        private readonly ImasterDataBussiness _masterDataBusiness;

        public LeaveBalanceModel(ILeaveBalanceBusiness leaveBalanceBusiness, ILeaveBusiness leaveBusiness, IEmpBusiness employeeBusiness, ImasterDataBussiness masterDataBusiness)
        {
            _leaveBalanceBusiness = leaveBalanceBusiness;
            _leaveBusiness = leaveBusiness;
            _employeeBusiness = employeeBusiness;
            _masterDataBusiness = masterDataBusiness;
            KeyPage = "LeaveBalance";
            TitlePage = "Quản lý số ngày nghỉ phép";
        }

        public BaseList LeaveBalanceList { get; set; } = new BaseList();
        public LeaveBalanceRequest RequestSearch { get; set; } = new LeaveBalanceRequest();
        public LeaveBalanceIndexModel? MyBalance { get; set; }
        public List<DataMasterItem> MasterDataList { get; set; } = new List<DataMasterItem>();

        public int TotalRecord => LeaveBalanceList.Total;

        public async Task<IActionResult> OnGetAsync([FromQuery] LeaveBalanceRequest request)
        {
            if (!HttpContext.User.Identity.IsAuthenticated)
            {
                return Redirect("/Login");
            }

            GetInfoUser();
            RequestSearch = request ?? new LeaveBalanceRequest();
            RequestSearch.Token ??= string.Empty;

            if (!(Permision.View ?? false))
            {
                LeaveBalanceList = new BaseList { Data = new List<object>(), Total = 0 };
                return Page();
            }

            if (UserData?.RoleCode == "2")
            {
                RequestSearch.Page = 1;
                RequestSearch.Limit = 1;
                RequestSearch.Token = string.Empty;

                MyBalance = await _leaveBusiness.GetEmployeeLeaveBalance(UserData.UserId);
                if (MyBalance == null || MyBalance.Id <= 0)
                {
                    LeaveBalanceList = new BaseList { Data = new List<object>(), Total = 0 };
                    return Page();
                }

                LeaveBalanceList = new BaseList
                {
                    Data = new List<LeaveBalanceIndexModel> { MyBalance },
                    Total = 1
                };
                return Page();
            }

            // Load master data for filters
            var masterDataResult = await _masterDataBusiness.GetAll(new CommonRequest());
            if (masterDataResult?.Data != null)
            {
                foreach (var item in masterDataResult.Data)
                {
                    var tempItem = item as dynamic;
                    if (tempItem != null)
                    {
                        MasterDataList.Add(new DataMasterItem
                        {
                            Name = tempItem.Name ?? string.Empty,
                            TypeData = tempItem.TypeData ?? 0,
                            Code = tempItem.Code ?? string.Empty,
                            ApplyFor = tempItem.ApplyFor ?? 0
                        });
                    }
                }
            }

            LeaveBalanceList = await _leaveBalanceBusiness.GetLeaveBalances(RequestSearch);
            return Page();
        }

        public async Task<IActionResult> OnGetLeaveDetailsAsync(int employeeId)
        {
            GetInfoUser();
            if (!(Permision.View ?? false))
            {
                return new JsonResult(new List<object>());
            }

            if (UserData?.RoleCode == "2" && employeeId != UserData.UserId)
            {
                return new JsonResult(new List<object>());
            }

            if (employeeId <= 0)
            {
                return new JsonResult(new List<object>());
            }

            string managerName = string.Empty;
            var employee = await _employeeBusiness.GetById(employeeId);
            if (employee?.ManagerId.HasValue == true && employee.ManagerId.Value > 0)
            {
                var manager = await _employeeBusiness.GetById(employee.ManagerId.Value);
                managerName = manager?.FullName ?? manager?.UserName ?? string.Empty;
            }

            var result = await _leaveBusiness.GetLeaveList(employeeId, null, null, null, 1, 1000);
            var items = result?.Data?.OfType<LeaveIndexModel>().ToList() ?? new List<LeaveIndexModel>();
            var response = items.Select(item => new
            {
                item.LeaveTypeName,
                item.FromDate,
                item.ToDate,
                item.NumDays,
                item.Status,
                item.Reason,
                CreatedAt = item.CreateAt,
                CurrentApproverName = ResolveCurrentApproverName(item, managerName)
            }).ToList();

            return new JsonResult(response);
        }

        public async Task<IActionResult> OnPostUpdateLeaveBalanceAsync([FromBody] LeaveBalanceUpdateRequest request)
        {
            GetInfoUser();
            if (!(Permision.Edit ?? false))
            {
                return ApiResponseHelper.Error("No permission", StatusCodes.Status403Forbidden);
            }

            var errors = new List<object>();
            ValidationHelper.ValidateId(request.EmployeeId, "employeeId", "nhan vien", errors);

            if (request.AllowedLeaveDays.HasValue && request.AllowedLeaveDays.Value < 0)
            {
                errors.Add(new { name = "allowedLeaveDays", Content = "So ngay phep khong duoc nho hon 0" });
            }

            if (request.CarryOverLeaveDays.HasValue && request.CarryOverLeaveDays.Value < 0)
            {
                errors.Add(new { name = "carryOverLeaveDays", Content = "So ngay phep ton nam cu khong duoc nho hon 0" });
            }

            if (request.UsedLeaveDays.HasValue && request.UsedLeaveDays.Value < 0)
            {
                errors.Add(new { name = "usedLeaveDays", Content = "So ngay phep da dung khong duoc nho hon 0" });
            }

            if (ValidationHelper.HasErrors(errors))
            {
                return ApiResponseHelper.BadRequest(errors);
            }

            var result = await _leaveBalanceBusiness.UpdateLeaveBalance(
                request.EmployeeId,
                request.AllowedLeaveDays,
                request.CarryOverLeaveDays,
                request.UsedLeaveDays,
                UserData.UserId);

            return ApiResponseHelper.SuccessResponse(new { success = result });
        }

        private static string ResolveCurrentApproverName(LeaveIndexModel item, string managerName)
        {
            if (item == null)
            {
                return string.Empty;
            }

            switch (item.Status)
            {
                case 0:
                    return FirstNonEmpty(managerName, item.LeadApproverName, item.ApproverName, "Lead");
                case 1:
                    return FirstNonEmpty(item.HCNSApproverName, "HCNS");
                case 2:
                    return FirstNonEmpty(item.BGDApproverName, "BGD");
                case 3:
                case 4:
                case 5:
                case 6:
                    return FirstNonEmpty(item.ApproverName, item.BGDApproverName, item.HCNSApproverName, item.LeadApproverName);
                default:
                    return FirstNonEmpty(item.ApproverName, item.BGDApproverName, item.HCNSApproverName, item.LeadApproverName);
            }
        }

        private static string FirstNonEmpty(params string?[] values)
        {
            foreach (var value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value.Trim();
                }
            }

            return string.Empty;
        }

    }
}
