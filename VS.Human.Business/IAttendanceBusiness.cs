using Microsoft.AspNetCore.Http;
using VS.Human.Item;

namespace VS.Human.Business
{
    public interface IAttendanceBusiness
    {
        Task<BaseList> GetSummary(AttendanceRequest request);
        Task<List<AttendanceDetailModel>> GetDetails(int? employeeId, string? fingerprintCode, DateTime fromDate, DateTime toDate, int userId);
        Task<List<AttendanceDepartmentRuleModel>> GetDepartmentRulesAsync();
        Task<bool> SaveDepartmentRuleAsync(AttendanceDepartmentRuleModel rule, int userId);
        Task<bool> DeleteDepartmentRuleAsync(int id, int userId);
        Task<List<AttendanceHolidayModel>> GetHolidaysAsync();
        Task<bool> SaveHolidayAsync(AttendanceHolidayModel holiday, int userId);
        Task<bool> DeleteHolidayAsync(int id, int userId);
        Task<int> EvaluateAttendanceRangeAsync(DateTime fromDate, DateTime toDate, int userId);
        Task<AttendanceImportResult> ImportAsync(IFormFile file, int userId);
        Task<AttendanceImportResult> SyncFromAccessAsync(DateTime fromDate, DateTime toDate, int userId);
        Task<AttendanceImportResult> SyncFromDeviceAsync(DateTime fromDate, DateTime toDate, int userId);
        Task<AttendanceImportResult> SyncFromDirectSqlAsync(DateTime fromDate, DateTime toDate, int userId);
    }

    public class AttendanceImportResult
    {
        public int Total { get; set; }
        public int TotalSuccess { get; set; }
        public int TotalError { get; set; }
        public List<AttendanceImportError> Errors { get; set; } = new List<AttendanceImportError>();
    }

    public class AttendanceImportError
    {
        public int Row { get; set; }
        public string Content { get; set; } = string.Empty;
    }
}
