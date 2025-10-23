using Microsoft.AspNetCore.Http;
using VS.Human.Item;
using VS.Human.Rep;

namespace VS.Human.Business.Imp
{
    public class ReportBussiness : BaseBusiness, IReoportBussiness
    {
        public ReportBussiness(IUnitOfWork unitOfWork,
            IHttpContextAccessor httpContextAccessor)
            : base(unitOfWork, httpContextAccessor)
        {

        }

        public async Task<BaseList> GetAllImpact(OrderRequest request)
        {
            return await _unitOfWork.OrderRep.GetAllImpactReport(request);
        }

        public async Task<BaseList> GetAllOrderStatus(OrderRequest request)
        {
            return await _unitOfWork.OrderRep.GetAllOrderStatus(request);
        }



        public async Task<BaseList> GetAllRecordingFile(ReportCDRequest request)
        {
            var result = await _unitOfWork.ReportRepository.GetAllRecordingFile(request);

            var baseList = new BaseList();

            baseList.Data = result.Data;

            return baseList;
        }


        public async Task<BaseList> GetAllRecordGroupByDay(GetAllRecordGroupByLineCodeRequest request)
        {
            var result = await _unitOfWork.ReportTalkTimeGroupByDay.GetAll(request);
            var baseList = new BaseList();
            baseList.Data = result.Data;
            return baseList;
        }


        public async Task<GetOverViewDashboardReponse> GetOverViewDashBoard(GetOverViewDashboard entity)
        {
            return await _unitOfWork.ReportTalkTimeGroupByDay.GetOverViewDashBoard(entity);


        }
        public async Task<BaseList> GetAllTalkTime(GetAllRecordGroupByLineCodeRequest request)
        {
            var result = await _unitOfWork.ReportTalkTimeGroupByDay.GetAll(request);
            var baseList = new BaseList();
            baseList.Data = result.Data;
            return baseList;
        }

    }
}
