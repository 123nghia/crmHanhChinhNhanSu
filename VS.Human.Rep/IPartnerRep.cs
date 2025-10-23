using VS.Human.Item;
using VS.Human.Rep.Model;

namespace VS.Human.Rep
{
    public interface IPartnerRep
    {
        Task<bool> AddOrUpdate(Partner item);

        Task<bool> Delete(int id);

        Task<Partner> GetById(int id);

        Task<BaseList> GetAll(CommonRequest request);



    }
}
