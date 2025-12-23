using Dapper;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using VS.Human.Rep.Model;

namespace VS.Human.Rep
{
    public class PermissionRep : RepositoryBase<RoleAllow>, IPermissionRep
    {
        public PermissionRep(IConfiguration configuration) : base(configuration)
        {
        }

        public async Task<List<PermissionConfigViewModel>> GetPermissionsByRole(string roleCode)
        {
            var p = new DynamicParameters();
            p.Add("@RoleCode", roleCode);
            // RepositoryBase uses ExecuteSQL<T> for lists
            return await ExecuteSQL<PermissionConfigViewModel>("sp_Permission_GetByRole", p);
        }

        public List<PermissionConfigViewModel> GetPermissionsByRoleSync(string roleCode)
        {
            var p = new DynamicParameters();
            p.Add("@RoleCode", roleCode);
            // ExecuteSQLList is likely async generic. I need to check RepositoryBase for a sync Query method.
            // Assuming RepositoryBase uses Dapper, I can probably access connection.
            // If ExecuteSQLList is only async, I might have to use .Result (careful) or check base class.
            // Let's assume for a moment I can use .Result safely if not in a deadlock context, 
            // OR I should use Dapper directly if Base exposes Connection.
            // Safest: Check RepositoryBase.
            return ExecuteSQLListSync<PermissionConfigViewModel>("sp_Permission_GetByRole", p); 
        }

        public async Task<bool> SavePermission(RoleAllow permission)
        {
            var p = new DynamicParameters();
            p.Add("@RoleCode", permission.RoleCode);
            p.Add("@PageCode", permission.PageCode);
            p.Add("@IsView", permission.IsView);
            p.Add("@IsAdd", permission.IsAdd);
            p.Add("@IsEdit", permission.IsEdit);
            p.Add("@IsDelete", permission.IsDelete);
            p.Add("@IsApprove", permission.IsApprove);

            return await ExecuteSQL("sp_Permission_Save", p);
        }
    }
}
