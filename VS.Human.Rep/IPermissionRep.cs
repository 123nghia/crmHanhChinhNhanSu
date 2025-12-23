using System.Collections.Generic;
using System.Threading.Tasks;
using VS.Human.Rep.Model;

namespace VS.Human.Rep
{
    public interface IPermissionRep
    {
        Task<List<PermissionConfigViewModel>> GetPermissionsByRole(string roleCode);
        List<PermissionConfigViewModel> GetPermissionsByRoleSync(string roleCode);
        Task<bool> SavePermission(RoleAllow permission);
    }
}
