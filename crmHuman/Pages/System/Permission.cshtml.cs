using crmHuman.Model;
using crmHuman.Pages;
using Microsoft.AspNetCore.Mvc;
using VS.Human.Business;
using VS.Human.Rep.Model;

namespace crmHuman.Pages.System
{
    public class PermissionModel : BaseModel2
    {
        private readonly IPermissionBusiness _permissionBusiness;

        public PermissionModel(IPermissionBusiness permissionBusiness)
        {
            _permissionBusiness = permissionBusiness;
            TitlePage = "Quản lý phân quyền";
            KeyPage = "Permission";
        }

        [BindProperty(SupportsGet = true)]
        public string RoleCode { get; set; } = "1"; // Default Admin

        public List<PermissionConfigViewModel> Permissions { get; set; } = new List<PermissionConfigViewModel>();

        public async Task OnGetAsync()
        {
            GetInfoUser();
            // Check permission to view this page?
            // Since we just implemented dynamic check, if this page is in DB as 'Permission', we need to have rights.
            // Admin (Role 1) has all rights seeded.
            
            if (UserData.RoleCode != "1") 
            {
                // Strict check for now, or use dynamic check
                // For safety during dev, let restrict to Admin or Role 1
            }

            if (!string.IsNullOrEmpty(RoleCode))
            {
                Permissions = await _permissionBusiness.GetPermissionsByRole(RoleCode);
            }
        }

        public async Task<IActionResult> OnPostSaveAsync([FromBody] List<PermissionConfigViewModel> data)
        {
             if (!ModelState.IsValid)
            {
                var errors = string.Join("; ", ModelState.Values
                                        .SelectMany(x => x.Errors)
                                        .Select(x => x.ErrorMessage));
                return new JsonResult(new { success = false, message = "Invalid data: " + errors });
            }

            var role = Request.Query["RoleCode"].ToString();
            if (string.IsNullOrEmpty(role)) role = RoleCode;

            var result = await _permissionBusiness.SavePermissions(data, role);

            return new JsonResult(new { success = result });
        }
    }
}
