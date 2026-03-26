using VS.Human.Item;
using VS.Human.Rep.Model;

namespace VS.Human.Rep
{
    public interface IScheduleInterviewRep
    {
        Task<bool> AddOrUpdate(ScheduleInterview item);
        Task<int> SaveAndGetId(ScheduleInterview item);
        Task<bool> Delete(int id);
        Task<ScheduleInterview> GetById(int id);
        Task<BaseList> GetAll(ScheduleInterviewRquest request);
        Task<bool> HasViewAccess(int scheduleId, int userId, string? roleCode);
        Task<bool> HasManageAccess(int scheduleId, int userId, string? roleCode);


    }
}
