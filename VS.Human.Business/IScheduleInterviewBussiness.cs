using VS.Human.Business.Model;
using VS.Human.Item;
using VS.Human.Rep.Model;

namespace VS.Human.Business
{
    public interface IScheduleInterviewBussiness
    {
        Task<InterviewScheduleSaveResult> SaveInterviewSchedule(ScheduleInterviewAdd item, int actorUserId);

        Task<bool> AddOrUpdate(ScheduleInterviewAdd item);

        Task<bool> Delete(int id);

        Task<ScheduleInterview> GetById(int id);

        Task<BaseList> GetAll(ScheduleInterviewRquest request);

        Task<BaseList> GetallRegional();
        Task<bool> HasViewAccess(int scheduleId, int userId, string? roleCode);
        Task<bool> HasManageAccess(int scheduleId, int userId, string? roleCode);
    }
}
