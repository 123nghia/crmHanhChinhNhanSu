using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using VS.Human.Business.Imp;
using VS.Human.Rep;
using VS.Human.Rep.Model;

namespace VS.Human.Business
{
    public class PermissionBusiness : BaseBusiness, IPermissionBusiness
    {
        public PermissionBusiness(IUnitOfWork unitOfWork, IHttpContextAccessor contextAccessor) : base(unitOfWork, contextAccessor)
        {
        }

        public async Task<List<PermissionConfigViewModel>> GetPermissionsByRole(string roleCode)
        {
            return await _unitOfWork.PermissionRep.GetPermissionsByRole(roleCode);
        }

        public List<PermissionConfigViewModel> GetPermissionsByRoleSync(string roleCode)
        {
            return _unitOfWork.PermissionRep.GetPermissionsByRoleSync(roleCode);
        }

        public async Task<bool> SavePermissions(List<PermissionConfigViewModel> permissions, string roleCode)
        {
            if (permissions == null || permissions.Count == 0) return false;

            foreach (var p in permissions)
            {
                var roleAllow = new RoleAllow
                {
                    RoleCode = roleCode,
                    PageCode = p.PageCode,
                    IsView = p.IsView,
                    IsAdd = p.IsAdd,
                    IsEdit = p.IsEdit,
                    IsDelete = p.IsDelete,
                    IsApprove = p.IsApprove,
                    CreatedBy = GetUserId()
                };
                await _unitOfWork.PermissionRep.SavePermission(roleAllow);
            }
            return true;
        }
    }
}
