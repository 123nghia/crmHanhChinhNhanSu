using Microsoft.AspNetCore.Http;
using VS.Human.Business.Imp;
using VS.Human.Item;
using VS.Human.Rep;

namespace VS.Human.Business
{
    public class LeaveBalanceBusiness : BaseBusiness, ILeaveBalanceBusiness
    {
        public LeaveBalanceBusiness(IUnitOfWork unitOfWork, IHttpContextAccessor contextAccessor)
            : base(unitOfWork, contextAccessor)
        {
        }

        public async Task<BaseList> GetLeaveBalances(LeaveBalanceRequest request)
        {
            return await _unitOfWork.EmployeeRep.GetLeaveBalances(request);
        }

        public async Task<bool> UpdateLeaveBalance(int employeeId, decimal? allowedLeaveDays, decimal? carryOverLeaveDays, decimal? usedLeaveDays, int userId)
        {
            return await _unitOfWork.EmployeeRep.UpdateLeaveBalance(employeeId, allowedLeaveDays, carryOverLeaveDays, usedLeaveDays, userId);
        }
    }
}
