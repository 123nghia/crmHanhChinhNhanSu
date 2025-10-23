using VS.Human.Item;

namespace VS.Human.Business
{
    public interface IReportCDRBussiness
    {
        public Task<ReportCDRReponse> GetAllRecordingFile(ReportCDRequest request);
        public Task<ReportCDRReponse> GetAllRecordingByOrderId(int orderid);
    }

}
