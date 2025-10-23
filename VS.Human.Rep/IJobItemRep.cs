using VS.Human.Item;
using VS.Human.Rep.Model;

namespace VS.Human.Rep
{
    public interface IJobItemRep
    {
        Task<bool> AddOrUpdate(JobItem item);

        Task<bool> Delete(int id);

        Task<JobItem> GetById(int id);

        Task<BaseList> GetAll(JobRequest request);

        Task<BaseList> GetAll2(JobRequest2 request);

    }
}
