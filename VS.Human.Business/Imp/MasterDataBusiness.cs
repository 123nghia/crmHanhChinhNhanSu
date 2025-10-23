using Microsoft.AspNetCore.Http;
using VS.Human.Business.Model;
using VS.Human.Item;
using VS.Human.Rep;
using VS.Human.Rep.Model;

namespace VS.Human.Business.Imp
{
    public class MasterDataBusiness : BaseBusiness, ImasterDataBussiness
    {
        public MasterDataBusiness(IUnitOfWork unitOfWork,
            IHttpContextAccessor httpContextAccessor)
            : base(unitOfWork, httpContextAccessor)
        {

        }
        public async Task<bool> AddOrUpdate(MasterDataAdd item)
        {
            return await _unitOfWork.MasterDataRep.AddOrUpdate(item);
        }
        public async Task<bool> Delete(int id)
        {
            return await _unitOfWork.MasterDataRep.Delete(id);
        }

        public async Task<BaseList> GetAll(CommonRequest request)
        {
            return await _unitOfWork.MasterDataRep.GetAll(request);
        }

        public async Task<BaseList> GetallRegional()
        {
            return await _unitOfWork.LocationRep.GetAll();
        }
        public async Task<MasterData> GetById(int id)
        {
            return await _unitOfWork.MasterDataRep.GetById(id);
        }


    }
}
