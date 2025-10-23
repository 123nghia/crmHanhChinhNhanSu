using VS.Human.Item;

namespace VS.Human.Business
{
    public interface IReoportBussiness
    {

        Task<BaseList> GetAllImpact(OrderRequest request);
        Task<BaseList> GetAllOrderStatus(OrderRequest request);

        Task<BaseList> GetAllRecordingFile(ReportCDRequest request);
        Task<BaseList> GetAllTalkTime(GetAllRecordGroupByLineCodeRequest request);

        Task<GetOverViewDashboardReponse> GetOverViewDashBoard(GetOverViewDashboard request);
    }
}
