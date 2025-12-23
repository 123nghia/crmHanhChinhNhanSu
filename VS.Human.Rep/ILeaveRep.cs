using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VS.Human.Rep.Model;
using VS.Human.Item;

namespace VS.Human.Rep
{
    public interface ILeaveRep
    {
        Task<BaseList> GetAll(int? employeeId, int? status, DateTime? fromDate, DateTime? toDate, int page, int limit);
        Task<LeaveIndexModel> GetById(int id);
        Task<int> Save(LeaveAddUpdate model, int userId);
        Task<bool> Approve(int id, int status, int approverId, string comment);
        Task<bool> Delete(int id, int userId);
    }
}
