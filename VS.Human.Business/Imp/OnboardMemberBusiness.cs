using Microsoft.AspNetCore.Http;
using VS.Human.Item;
using VS.Human.Rep;
using VS.Human.Rep.Model;

namespace VS.Human.Business.Imp
{
    public class OnboardMemberBusiness : BaseBusiness, IOnboardMemberBusiness
    {
        public OnboardMemberBusiness(IUnitOfWork unitOfWork,
            IHttpContextAccessor httpContextAccessor)
            : base(unitOfWork, httpContextAccessor)
        {

        }
        public async Task<bool> AddOrUpdate(OnboardMember item)
        {

            item.CreatedBy = GetUserId();
            return await _unitOfWork.OnboardMemberRep.AddOrUpdate(item);
        }
        public async Task<bool> UpdateOnboard(dynamic item)
        {
            return await _unitOfWork.OnboardMemberRep.UpdateOnboard(item);
        }
        public async Task<BaseList> GetAll(OrderRequest request)
        {
            return await _unitOfWork.OnboardMemberRep.GetAll(request);
        }
        public async Task<OnboardMember> GetById(int id)
        {
            return await _unitOfWork.OnboardMemberRep.GetById(id);
        }
        public async Task<bool> UpdateOnboardStatus()
        {
            return await _unitOfWork.OnboardMemberRep.UpdateOnboardStatus();
        }

    }
}
