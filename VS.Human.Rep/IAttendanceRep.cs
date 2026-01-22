using VS.Human.Item;
using VS.Human.Rep.Model;

namespace VS.Human.Rep
{
    public interface IAttendanceRep
    {
        Task<bool> UpsertAsync(AttendanceRecord record, int userId);
        Task<BaseList> GetSummary(AttendanceRequest request);
        Task<List<AttendanceDetailModel>> GetDetails(int? employeeId, string? fingerprintCode, DateTime? fromDate, DateTime? toDate, int? userId);
    }
}
