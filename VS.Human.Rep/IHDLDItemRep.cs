using VS.Human.Rep.Model;

namespace VS.Human.Rep
{
    public interface IHDLDItemRep
    {
        Task<bool> AddOrUpdate(HDLD itemAdd);
        Task<bool> Delete(int id);
        Task<HDLD> GetInfo(string userId);

    }
}

