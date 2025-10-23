using VS.Human.Item;
using VS.Human.Rep.Model;

namespace VS.Human.Rep
{
    public interface IBHXHItemRep
    {
        Task<bool> AddOrUpdate(BHXHItem item);

        Task<bool> Delete(int id);

        Task<BHXHItem> GetInfo(string userName);

    }
}
