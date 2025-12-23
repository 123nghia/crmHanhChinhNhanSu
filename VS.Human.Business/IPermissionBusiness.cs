using System.Collections.Generic;
using System.Threading.Tasks;
using VS.Human.Rep.Model;

namespace VS.Human.Business
{
    public interface IPermissionBusiness
    {
        Task<List<PermissionConfigViewModel>> GetPermissionsByRole(string roleCode);
        List<PermissionConfigViewModel> GetPermissionsByRoleSync(string roleCode);
        Task<bool> SavePermissions(List<PermissionConfigViewModel> permissions, string roleCode);
    }
}
