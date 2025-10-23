using Microsoft.AspNetCore.Http;
using VS.Human.Item;
using VS.Human.Rep;

namespace VS.Human.Business.Imp
{
    public class DashboardBusinness : BaseBusiness, IDashboardBusinness
    {
        public DashboardBusinness(IUnitOfWork unitOfWork,
            IHttpContextAccessor httpContextAccessor)
            : base(unitOfWork, httpContextAccessor)
        {

        }
        public async Task<BaseList> GetParam(OrderRequest request)
        {

            var dataResult =
                await _unitOfWork.DashboardRep
                                  .GetInfoDashboard(request);

            return dataResult;
        }

        public async Task<BaseList> GetInfoCount(OrderRequest request)
        {

            var dataResult =
                await _unitOfWork.DashboardRep
                                  .GetInfoCount(request);

            return dataResult;
        }

        public async Task<BaseList> GetALLOrder(OrderRequest request)
        {

            var dataResult = await _unitOfWork.DashboardRep
                                    .GetALLOrder(request);

            return dataResult;
        }
        public async Task<BaseList> GetALLOrderInfo(OrderRequest request)
        {
            var dataResult = await _unitOfWork.DashboardRep
                                    .GetALLOrderInfo(request);

            return dataResult;
        }

        public async Task<BaseList> GetAllOnboardCV(OrderRequest request)
        {
            var dataResult = await _unitOfWork.DashboardRep
                                    .GetAllOnboardCV(request);

            return dataResult;
        }

        public async Task<BaseList> GetAllOrderApply(OrderRequest request)
        {
            var dataResult = await _unitOfWork.DashboardRep
                                    .GetAllOrderApply(request);

            return dataResult;
        }


        public async Task<BaseList> GetAllOrderDraft(OrderRequest request)
        {
            var dataResult = await _unitOfWork.DashboardRep
                                    .GetAllOrderDraft(request);

            return dataResult;
        }

        public async Task<BaseList> GetALlOrderRemove(OrderRequest request)
        {
            var dataResult = await _unitOfWork.DashboardRep
                                    .GetALlOrderRemoveDup(request);

            return dataResult;
        }



        public async Task<BaseList> GetAllCV(OrderRequest request)
        {
            var dataResult = await _unitOfWork.DashboardRep
                                    .GetAllCV(request);

            return dataResult;
        }



        public async Task<BaseList> GetDashboardStatus(OrderRequest request)
        {
            var dataResult = await _unitOfWork.DashboardRep.GetDashboardStatus(request);
            return dataResult;
        }

        public Task<BaseList> GetTopImpactOrder(OrderRequest request)
        {
            var dataResponse = _unitOfWork.DashboardRep.GetTopImpactOrder(request);
            return dataResponse;
        }

        public Task<BaseList> GetTopOrder(OrderRequest request)
        {
            var dataResponse = _unitOfWork.DashboardRep.GetTopOrder(request);
            return dataResponse;
        }

        public Task<BaseList> StatisticsReport(OrderRequest request)
        {
            var dataResponse = _unitOfWork.DashboardRep.StatisticsReport(request);
            return dataResponse;
        }

        public async Task<BaseList> GetAllCountCVGroupByDate(OrderRequest request)
        {

            var dataResult =
                await _unitOfWork.DashboardRep
                                  .GetAllCountCVGroupByDate(request);

            return dataResult;
        }
        public async Task<BaseList> GetAllCountCVApplyGroupByDate(OrderRequest request)
        {

            var dataResult =
                await _unitOfWork.DashboardRep
                                  .GetAllCountCVApplyGroupByDate(request);

            return dataResult;
        }
        public async Task<BaseList> GetAllCountCVOnboardGroupByDate(OrderRequest request)
        {

            var dataResult =
                await _unitOfWork.DashboardRep
                                  .GetAllCountCVOnboardGroupByDate(request);

            return dataResult;
        }
    }
}
