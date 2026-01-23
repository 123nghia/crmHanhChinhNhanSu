using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VS.Human.Item;
using VS.Human.Rep.Model;

namespace VS.Human.Business
{
    public interface ILateEarlyBusiness
    {
        Task<BaseList> GetList(int? employeeId, int? status, DateTime? fromDate, DateTime? toDate, int page, int limit);
        Task<LateEarlyIndexModel?> GetById(int id);
        Task<int> CreateOrUpdate(LateEarlyAddUpdate model, int userId);
        Task<bool> ApproveWorkflow(int id, string action, int approverId, string roleCode, string? comment);
        Task<List<LateEarlyHistory>> GetHistory(int requestId);
        Task<bool> Delete(int id, int userId);
    }
}
