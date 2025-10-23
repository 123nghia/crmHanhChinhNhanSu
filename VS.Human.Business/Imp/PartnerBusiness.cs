using Microsoft.AspNetCore.Http;
using VS.Human.Business.Model;
using VS.Human.Item;
using VS.Human.Rep;
using VS.Human.Rep.Model;

namespace VS.Human.Business.Imp
{
    public class PartnerBusiness : BaseBusiness, IPartnerBusiness
    {
        public PartnerBusiness(IUnitOfWork unitOfWork,
            IHttpContextAccessor httpContextAccessor)
            : base(unitOfWork, httpContextAccessor)
        {

        }
        public async Task<bool> AddOrUpdate(PartnerAdd item)
        {


            return await _unitOfWork.PartnerRep.AddOrUpdate(item);
        }

        public async Task<bool> Delete(int id)
        {
            return await _unitOfWork.PartnerRep.Delete(id);
        }

        public async Task<BaseList> GetAll(CommonRequest request)
        {
            return await _unitOfWork.PartnerRep.GetAll(request);
        }

        public async Task<Partner> GetById(int id)
        {
            return await _unitOfWork.PartnerRep.GetById(id);
        }
    }
}
