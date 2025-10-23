using Microsoft.AspNetCore.Http;
using VS.Human.Item;
using VS.Human.Rep;

namespace VS.Human.Business.Imp
{
    public class ReportCDRBussiness : BaseBusiness, IReportCDRBussiness
    {
        public ReportCDRBussiness(IUnitOfWork unitOfWork,
            IHttpContextAccessor httpContextAccessor)
            : base(unitOfWork, httpContextAccessor)
        {

        }


        public async Task<ReportCDRReponse> GetAllRecordingFile(ReportCDRequest request)
        {
            return await _unitOfWork.ReportRepository.GetAllRecordingFile(request);
        }


        public async Task<ReportCDRReponse> GetAllRecordingByOrderId(int orderId)
        {
            return await _unitOfWork.ReportRepository.GetAllRecordingByOrdercode(orderId);

        }


    }
}
