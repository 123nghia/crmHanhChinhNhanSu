using Dapper;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using VS.Human.Item;
using VS.Human.Rep.Model;

namespace VS.Human.Rep
{
    public class AttendanceRep : RepositoryBase<AttendanceRecord>, IAttendanceRep
    {
        public AttendanceRep(IConfiguration configuration) : base(configuration)
        {
        }

        public async Task<bool> UpsertAsync(AttendanceRecord record, int userId)
        {
            var p = new DynamicParameters();
            p.Add("@EmployeeId", record.EmployeeId);
            p.Add("@FingerprintCode", record.FingerprintCode);
            p.Add("@EmployeeName", record.EmployeeName);
            p.Add("@DepartmentName", record.DepartmentName);
            p.Add("@PositionName", record.PositionName);
            p.Add("@WorkDate", record.WorkDate);
            p.Add("@DayName", record.DayName);
            p.Add("@CheckIn", record.CheckIn);
            p.Add("@CheckOut", record.CheckOut);
            p.Add("@WorkDay", record.WorkDay);
            p.Add("@WorkHours", record.WorkHours);
            p.Add("@WorkDayPlus", record.WorkDayPlus);
            p.Add("@WorkHoursPlus", record.WorkHoursPlus);
            p.Add("@LateMinutes", record.LateMinutes);
            p.Add("@EarlyMinutes", record.EarlyMinutes);
            p.Add("@Shift1", record.Shift1);
            p.Add("@Shift2", record.Shift2);
            p.Add("@Shift3", record.Shift3);
            p.Add("@ShiftName", record.ShiftName);
            p.Add("@Symbol", record.Symbol);
            p.Add("@SymbolPlus", record.SymbolPlus);
            p.Add("@TotalHours", record.TotalHours);
            p.Add("@SourceFile", record.SourceFile);
            p.Add("@RowIndex", record.RowIndex);
            p.Add("@UserId", userId);

            return await ExecuteSQL("sp_Attendance_Upsert", p, CommandType.StoredProcedure);
        }

        public async Task<BaseList> GetSummary(AttendanceRequest request)
        {
            var page = request.Page;
            var limit = request.Limit;
            ProcessInputPaging(ref page, ref limit, out var offset);

            request.Page = page;
            request.Limit = limit;

            var parameters = new
            {
                FromDate = request.From,
                ToDate = request.To,
                EmployeeId = request.EmployeeId,
                Token = request.Token ?? string.Empty,
                offset,
                limit,
                UserId = request.UserId
            };

            return await GetBaseAll<AttendanceSummaryIndexModel>(request, parameters, "sp_Attendance_GetSummary");
        }

        public async Task<List<AttendanceDetailModel>> GetDetails(int? employeeId, string? fingerprintCode, DateTime? fromDate, DateTime? toDate, int? userId)
        {
            var p = new DynamicParameters();
            p.Add("@EmployeeId", employeeId);
            p.Add("@FingerprintCode", fingerprintCode);
            p.Add("@FromDate", fromDate);
            p.Add("@ToDate", toDate);
            p.Add("@UserId", userId);

            return await ExecuteSQL<AttendanceDetailModel>("sp_Attendance_GetDetails", p);
        }
    }
}
