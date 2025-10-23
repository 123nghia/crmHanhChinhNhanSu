using VS.Human.Business.Model;
using VS.Human.Item;
using VS.Human.Rep.Model;

namespace VS.Human.Business
{
    public interface IPartnerBusiness
    {
        Task<bool> AddOrUpdate(PartnerAdd item);

        Task<bool> Delete(int id);

        Task<Partner> GetById(int id);

        Task<BaseList> GetAll(CommonRequest request);

    }
}
