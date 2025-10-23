using Microsoft.AspNetCore.Http;
using VS.Human.Rep;

namespace VS.Human.Business.Imp
{
    public class GlobalDataBusinness : BaseBusiness, IGlobalDataBusiness
    {
        public GlobalDataBusinness(IUnitOfWork unitOfWork,
            IHttpContextAccessor httpContextAccessor)
            : base(unitOfWork, httpContextAccessor)
        {

        }

        public async Task<int> GetCountSource()
        {

            return await _unitOfWork.GlobalDataRep.GetCountSource();
        }
        public async Task<int> GetCountSourceCTV()
        {

            return await _unitOfWork.GlobalDataRep.GetCountSourceCTV();
        }

    }
}
