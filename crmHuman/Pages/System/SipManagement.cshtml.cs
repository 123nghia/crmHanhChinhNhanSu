using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VS.Human.Business;
using VS.Human.Business.Model;
using VS.Human.Rep.Model;

namespace crmHuman.Pages.System
{
    [Authorize]
    public class SipManagementModel : BaseModel2
    {
        private readonly ISipBusiness _sipBusiness;

        public SipManagementModel(ISipBusiness sipBusiness)
        {
            _sipBusiness = sipBusiness;
            TitlePage = "Cấu hình SIP";
            KeyPage = "SipManagement";
        }

        public List<SipServer> Servers { get; set; } = new List<SipServer>();
        public SipServer CurrentServer { get; set; } = new SipServer
        {
            Port = 5060,
            Transport = "UDP",
            IsActive = 1
        };
        public List<SipLineViewModel> Lines { get; set; } = new List<SipLineViewModel>();
        public List<SipEmployeeOption> Employees { get; set; } = new List<SipEmployeeOption>();

        public async Task<IActionResult> OnGetAsync()
        {
            GetInfoUser();
            if (!CanViewSipManagement())
            {
                return Forbid();
            }

            Servers = await _sipBusiness.GetServers();
            CurrentServer = Servers.FirstOrDefault(x => x.IsActive > 0)
                ?? Servers.FirstOrDefault()
                ?? CurrentServer;
            Lines = await _sipBusiness.GetLines();
            Employees = await _sipBusiness.GetAssignableEmployees();
            return Page();
        }

        public async Task<IActionResult> OnPostSaveServerAsync([FromBody] SipServerSaveRequest request)
        {
            GetInfoUser();
            if (!CanManageSip())
            {
                return BuildResult(false, "Không có quyền thao tác.", 403);
            }

            var result = await _sipBusiness.SaveServer(request);
            return BuildResult(result.IsSuccess, result.Message, result.IsSuccess ? 200 : 400, result.Data);
        }

        public async Task<IActionResult> OnPostSaveLineAsync([FromBody] SipLineSaveRequest request)
        {
            GetInfoUser();
            if (!CanManageSip())
            {
                return BuildResult(false, "Không có quyền thao tác.", 403);
            }

            var result = await _sipBusiness.SaveLine(request);
            return BuildResult(result.IsSuccess, result.Message, result.IsSuccess ? 200 : 400, result.Data);
        }

        public async Task<IActionResult> OnPostAssignLineAsync([FromBody] SipAssignRequest request)
        {
            GetInfoUser();
            if (!CanManageSip())
            {
                return BuildResult(false, "Không có quyền thao tác.", 403);
            }

            var result = await _sipBusiness.AssignLine(request);
            return BuildResult(result.IsSuccess, result.Message, result.IsSuccess ? 200 : 400);
        }

        public async Task<IActionResult> OnPostRevokeLineAsync([FromBody] SipRevokeRequest request)
        {
            GetInfoUser();
            if (!CanManageSip())
            {
                return BuildResult(false, "Không có quyền thao tác.", 403);
            }

            var result = await _sipBusiness.RevokeLine(request.LineId);
            return BuildResult(result.IsSuccess, result.Message, result.IsSuccess ? 200 : 400);
        }

        private bool CanViewSipManagement()
        {
            return UserData?.RoleCode == "1" || (Permision.View ?? false);
        }

        private bool CanManageSip()
        {
            return UserData?.RoleCode == "1" || (Permision.Add ?? false) || (Permision.Edit ?? false) || (Permision.Delete ?? false);
        }

        private JsonResult BuildResult(bool success, string message, int statusCode, object? data = null)
        {
            return new JsonResult(new
            {
                success,
                message,
                data
            })
            {
                StatusCode = statusCode
            };
        }
    }
}
