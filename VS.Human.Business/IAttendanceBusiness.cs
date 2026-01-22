using Microsoft.AspNetCore.Http;
using VS.Human.Item;

namespace VS.Human.Business
{
    public interface IAttendanceBusiness
    {
        Task<BaseList> GetSummary(AttendanceRequest request);
        Task<List<AttendanceDetailModel>> GetDetails(int? employeeId, string? fingerprintCode, DateTime fromDate, DateTime toDate, int userId);
        Task<AttendanceImportResult> ImportAsync(IFormFile file, int userId);
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
