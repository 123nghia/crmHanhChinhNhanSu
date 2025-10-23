using VS.Human.Business.Model;
using VS.Human.Item;
using VS.Human.Rep.Model;

namespace VS.Human.Business
{
    public interface IScheduleInterviewBussiness
    {



        Task<bool> AddOrUpdate(ScheduleInterviewAdd item);

        Task<bool> Delete(int id);

        Task<ScheduleInterview> GetById(int id);

        Task<BaseList> GetAll(ScheduleInterviewRquest request);

        Task<BaseList> GetallRegional();
    }
}
