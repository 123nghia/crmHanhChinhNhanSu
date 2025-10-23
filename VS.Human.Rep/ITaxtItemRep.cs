using VS.Human.Rep.Model;

namespace VS.Human.Rep
{
    public interface ITaxtItemRep
    {
        Task<bool> AddOrUpdate(TaxItem itemAdd);
        Task<bool> Delete(int id);
        Task<TaxItem> GetInfo(string userId);

    }
}

