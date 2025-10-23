using VS.Human.Item;

namespace VS.Human.Rep
{
    public interface IDashboardRep
    {

        Task<BaseList> GetTopOrder(OrderRequest request);
        Task<BaseList> GetTopImpactOrder(OrderRequest request);
        Task<BaseList> StatisticsReport(OrderRequest request);
        Task<BaseList> GetInfoDashboard(OrderRequest request);
        Task<BaseList> GetInfoCount(OrderRequest request);
        Task<BaseList> GetALLOrder(OrderRequest request);

        Task<BaseList> GetALLOrderInfo(OrderRequest request);
        Task<BaseList> GetAllOnboardCV(OrderRequest request);
        Task<BaseList> GetAllOrderApply(OrderRequest request);

        Task<BaseList> GetAllOrderDraft(OrderRequest request);

        Task<BaseList> GetALlOrderRemoveDup(OrderRequest request);

        Task<BaseList> GetDashboardStatus(OrderRequest request);
        Task<BaseList> GetAllCV(OrderRequest request);


        Task<BaseList> GetAllCountCVGroupByDate(OrderRequest request);
        Task<BaseList> GetAllCountCVApplyGroupByDate(OrderRequest request);
        Task<BaseList> GetAllCountCVOnboardGroupByDate(OrderRequest request);





    }
}
