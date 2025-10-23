using VS.Human.Item;
using VS.Human.Rep.Model;

namespace VS.Human.Rep
{
    public interface IOnboardMemberRep
    {
        Task<bool> AddOrUpdate(OnboardMember item);
        Task<bool> UpdateOnboard(dynamic item);

        Task<BaseList> GetAll(OrderRequest request);
        Task<OnboardMember> GetById(int id);

        public Task<bool> UpdateOnboardStatus();


    }
}
