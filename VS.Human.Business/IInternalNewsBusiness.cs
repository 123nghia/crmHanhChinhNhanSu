using VS.Human.Item;
using VS.Human.Rep.Model;

namespace VS.Human.Business
{
    public interface IInternalNewsBusiness
    {
        Task<BaseList> GetAll(InternalNewsRequest request);
        Task<InternalNewsItem> GetById(int id);
        Task<bool> AddOrUpdate(InternalNewsItem item);
        Task<bool> Delete(int id);
    }
}
