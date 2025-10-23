using VS.Human.Item;
using VS.Human.Rep.Model;

namespace VS.Human.Business
{
    public interface IOnboardMemberBusiness
    {
        Task<bool> AddOrUpdate(OnboardMember item);
        Task<BaseList> GetAll(OrderRequest request);
        Task<OnboardMember> GetById(int id);
        Task<bool> UpdateOnboard(dynamic request);
        Task<bool> UpdateOnboardStatus();

    }
}
