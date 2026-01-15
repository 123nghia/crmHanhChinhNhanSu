using VS.Human.Item;
using VS.Human.Rep.Model;

namespace VS.Human.Rep
{
    public interface IInternalNewsRep
    {
        Task<BaseList> GetAll(InternalNewsRequest request);
        Task<InternalNewsItem> GetById(int id);
        Task<bool> AddOrUpdate(InternalNewsItem item);
        Task<bool> Delete(int id);
    }
}
