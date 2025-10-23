using Microsoft.AspNetCore.Http;
using VS.Human.Business.Model;
using VS.Human.Item;
using VS.Human.Rep;
using VS.Human.Rep.Model;

namespace VS.Human.Business.Imp
{
    public class ScheduleInterviewBusiness : BaseBusiness, IScheduleInterviewBussiness
    {
        public ScheduleInterviewBusiness(IUnitOfWork unitOfWork,
            IHttpContextAccessor httpContextAccessor)
            : base(unitOfWork, httpContextAccessor)
        {

        }
        public async Task<bool> AddOrUpdate(ScheduleInterviewAdd item)
        {
            return await _unitOfWork.ScheduleInterviewRep.AddOrUpdate(item);
        }
        public async Task<bool> Delete(int id)
        {
            return await _unitOfWork.ScheduleInterviewRep.Delete(id);
        }

        public async Task<BaseList> GetAll(ScheduleInterviewRquest request)
        {
            return await _unitOfWork.ScheduleInterviewRep.GetAll(request);
        }

        public async Task<BaseList> GetallRegional()
        {
            return await _unitOfWork.LocationRep.GetAll();
        }
        public async Task<ScheduleInterview> GetById(int id)
        {
            return await _unitOfWork.ScheduleInterviewRep.GetById(id);
        }


    }
}
