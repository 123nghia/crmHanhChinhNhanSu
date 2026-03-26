using VS.Human.Item;
using VS.Human.Rep.Model;

namespace VS.Human.Rep
{
    public interface IEmployeeRep
    {
        Task<Employee> Login(string userName, string password);
        Task<bool> AddOrUpdate(Employee item);
        Task<bool> ChangePassword(string password, int id);
        Task<bool> UpdateCredentials(int id, string userName, string password);
        Task<bool> UpdateAvatar(int id, string avatarFile, int updatedBy);
        Task<bool> UpdateMailSignature(int id, string? mailSignature, int updatedBy);

        Task<bool> Delete(int id, bool reactive = false);
        Task<Employee> GetById(int id);
        Task<Employee> CheckDuplicate(string email, string phone);
        Task<Employee> GetByUserName(string userName);
        Task<Employee?> GetByFingerprintCode(string fingerprintCode);
        Task<Employee> GetLastByEmailOrPhone(string email, string phone);
        Task<List<Employee>> GetDuplicateSeeds();
        Task<List<Employee>> GetByRoleCodes(IEnumerable<string> roleCodes);
        Task<BaseList> GetAll(EmployeeRequest id);
        Task<BaseList> GetAllExtended(EmployeeRequest request);
        Task<List<EmployeeExtendedModel>> ExecuteExport(EmployeeRequest request);
        Task<BaseList> GetAllManager(int leadgroup = -1);
        Task<Account> GetByLineCode(string lineCode);
        Task<BaseList> GetLeaveBalances(LeaveBalanceRequest request);
        Task<bool> UpdateLeaveBalance(int employeeId, decimal? allowedLeaveDays, decimal? carryOverLeaveDays, decimal? usedLeaveDays, decimal? expiredLeaveDays, int userId);

    }
}
