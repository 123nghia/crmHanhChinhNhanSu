using VS.Human.Item;

namespace VS.Human.Business
{
    public interface IDashboardBusinness
    {
        Task<BaseList> GetTopOrder(OrderRequest request);
        Task<BaseList> GetTopImpactOrder(OrderRequest request);
        Task<BaseList> StatisticsReport(OrderRequest request);
        Task<BaseList> GetParam(OrderRequest request);
        Task<BaseList> GetALLOrder(OrderRequest request);
        Task<BaseList> GetDashboardStatus(OrderRequest request);

        Task<BaseList> GetAllCV(OrderRequest request);

        Task<BaseList> GetALLOrderInfo(OrderRequest request);

        Task<BaseList> GetAllOnboardCV(OrderRequest request);
        Task<BaseList> GetAllOrderApply(OrderRequest request);
        Task<BaseList> GetAllOrderDraft(OrderRequest request);

        Task<BaseList> GetALlOrderRemove(OrderRequest request);
        Task<BaseList> GetInfoCount(OrderRequest request);

        Task<BaseList> GetAllCountCVGroupByDate(OrderRequest request);
        Task<BaseList> GetAllCountCVApplyGroupByDate(OrderRequest request);
        Task<BaseList> GetAllCountCVOnboardGroupByDate(OrderRequest request);
        Task<dynamic> GetLeaveSummary(int userId, string roleCode);
    }
}
