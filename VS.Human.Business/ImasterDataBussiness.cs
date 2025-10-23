using VS.Human.Business.Model;
using VS.Human.Item;
using VS.Human.Rep.Model;

namespace VS.Human.Business
{
    public interface ImasterDataBussiness
    {



        Task<bool> AddOrUpdate(MasterDataAdd item);

        Task<bool> Delete(int id);

        Task<MasterData> GetById(int id);

        Task<BaseList> GetAll(CommonRequest request);

        Task<BaseList> GetallRegional();
    }
}
