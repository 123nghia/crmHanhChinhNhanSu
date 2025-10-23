using Microsoft.AspNetCore.Http;
using VS.Human.Business.Model;
using VS.Human.Item;
using VS.Human.Rep;
using VS.Human.Rep.Model;

namespace VS.Human.Business.Imp
{
    public class JobItemBusiness : BaseBusiness, IJobItemBusiness
    {
        public JobItemBusiness(IUnitOfWork unitOfWork,
            IHttpContextAccessor httpContextAccessor)
            : base(unitOfWork, httpContextAccessor)
        {

        }
        public async Task<bool> AddOrUpdate(JobItemAdd item)
        {
            item.CreatedBy = GetUserId();
            return await _unitOfWork.JobItemRep.AddOrUpdate(item);
        }

        public async Task<bool> Delete(int id)
        {
            return await _unitOfWork.JobItemRep.Delete(id);
        }

        public async Task<BaseList> GetAll(JobRequest request)
        {
            return await _unitOfWork.JobItemRep.GetAll(request);
        }

        public async Task<BaseList> GetAll2(JobRequest2 request)
        {
            return await _unitOfWork.JobItemRep.GetAll2(request);
        }


        public async Task<JobItem> GetById(int id)
        {
            return await _unitOfWork.JobItemRep.GetById(id);
        }
    }
}
