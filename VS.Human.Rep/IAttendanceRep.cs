using VS.Human.Item;
using VS.Human.Rep.Model;

namespace VS.Human.Rep
{
    public interface IAttendanceRep
    {
        Task<bool> UpsertAsync(AttendanceRecord record, int userId);
        Task<BaseList> GetSummary(AttendanceRequest request);
        Task<List<AttendanceDetailModel>> GetDetails(int? employeeId, string? fingerprintCode, DateTime? fromDate, DateTime? toDate, int? userId);
        Task<List<AttendanceDepartmentRuleModel>> GetDepartmentRulesAsync();
        Task<bool> UpsertDepartmentRuleAsync(AttendanceDepartmentRuleModel rule, int userId);
        Task<bool> DeleteDepartmentRuleAsync(int id, int userId);
        Task<List<AttendanceHolidayModel>> GetHolidaysAsync();
        Task<bool> UpsertHolidayAsync(AttendanceHolidayModel holiday, int userId);
        Task<bool> DeleteHolidayAsync(int id, int userId);
        Task<List<AttendanceHolidayModel>> GetHolidaysForEvaluationAsync(DateTime fromDate, DateTime toDate);
        Task<List<AttendanceEvaluationRecord>> GetRecordsForEvaluationAsync(DateTime fromDate, DateTime toDate);
        Task<List<AttendanceEvaluationEmployee>> GetEmployeesForEvaluationAsync(DateTime fromDate, DateTime toDate);
        Task<List<AttendanceApprovedLeave>> GetApprovedLeavesForEvaluationAsync(DateTime fromDate, DateTime toDate);
        Task<bool> UpdateEvaluationAsync(AttendanceEvaluationUpdate update, int userId);
    }
}
