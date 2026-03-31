using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VS.Human.Item;
using VS.Human.Rep.Model;

namespace VS.Human.Rep
{
    public interface ILateEarlyRep
    {
        Task<BaseList> GetAll(int? employeeId, int? status, DateTime? fromDate, DateTime? toDate, int page, int limit, int? currentUserId = null, string? currentRoleCode = null);
        Task<LateEarlyIndexModel?> GetById(int id);
        Task<int> Save(LateEarlyAddUpdate model, int userId, int status);
        Task<bool> ApproveWorkflow(int id, string action, int approverId, string roleCode, string? comment);
        Task<List<LateEarlyHistory>> GetHistory(int requestId);
        Task<bool> Delete(int id, int userId);
    }
}
