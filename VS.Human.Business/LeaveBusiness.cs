using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;
using VS.Human.Business.Imp;
using VS.Human.Item;
using VS.Human.Rep;
using VS.Human.Rep.Model;

namespace VS.Human.Business
{
    public class LeaveBusiness : BaseBusiness, ILeaveBusiness
    {
        public LeaveBusiness(IUnitOfWork unitOfWork, IHttpContextAccessor contextAccessor) : base(unitOfWork, contextAccessor)
        {
        }

        public async Task<BaseList> GetLeaveList(int? employeeId, int? status, DateTime? fromDate, DateTime? toDate, int page, int limit)
        {
            return await _unitOfWork.LeaveRep.GetAll(employeeId, status, fromDate, toDate, page, limit);
        }

        public async Task<LeaveIndexModel> GetLeaveById(int id)
        {
            return await _unitOfWork.LeaveRep.GetById(id);
        }

        public async Task<int> CreateOrUpdateLeave(LeaveAddUpdate model, int userId)
        {
            // Business logic: validate dates
            if (model.FromDate > model.ToDate)
            {
                return -1; // Or throw custom exception
            }

            // Calculate NumDays if not provided or to ensure correctness
            // Basic logic: count days inclusive
            if (model.NumDays <= 0)
            {
                model.NumDays = (decimal)(model.ToDate.Date - model.FromDate.Date).TotalDays + 1;
            }

            return await _unitOfWork.LeaveRep.Save(model, userId);
        }

        public async Task<bool> ApproveLeave(int id, int status, int approverId, string comment)
        {
            // Validate status
            if (status != 1 && status != 2) return false;

            return await _unitOfWork.LeaveRep.Approve(id, status, approverId, comment);
        }

        public async Task<bool> ApproveWorkflow(int id, string action, int approverId, string roleCode, string comment)
        {
            return await _unitOfWork.LeaveRep.ApproveWorkflow(id, action, approverId, roleCode, comment);
        }

        public async Task<List<LeaveHistory>> GetLeaveHistory(int leaveId)
        {
            return await _unitOfWork.LeaveRep.GetHistory(leaveId);
        }

        public async Task<bool> DeleteLeave(int id, int userId)
        {
            return await _unitOfWork.LeaveRep.Delete(id, userId);
        }
    }
}
