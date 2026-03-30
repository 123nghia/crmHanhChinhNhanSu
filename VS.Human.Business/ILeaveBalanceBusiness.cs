using VS.Human.Item;

namespace VS.Human.Business
{
    public interface ILeaveBalanceBusiness
    {
        Task<BaseList> GetLeaveBalances(LeaveBalanceRequest request);
        Task<bool> UpdateLeaveBalance(int employeeId, decimal? allowedLeaveDays, decimal? carryOverLeaveDays, decimal? expiredLeaveDays, int userId);
    }
}
