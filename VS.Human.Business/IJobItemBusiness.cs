using VS.Human.Business.Model;
using VS.Human.Item;
using VS.Human.Rep.Model;

namespace VS.Human.Business
{
    public interface IJobItemBusiness
    {
        Task<bool> AddOrUpdate(JobItemAdd item);
        Task<bool> Delete(int id);
        Task<JobItem> GetById(int id);

        Task<BaseList> GetAll(JobRequest request);
        Task<BaseList> GetAll2(JobRequest2 request);
    }
}
