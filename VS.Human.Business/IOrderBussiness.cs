using VS.Human.Business.Model;
using VS.Human.Item;
using VS.Human.Rep.Model;

namespace VS.Human.Business
{
    public interface IOrderBussiness
    {
        Task<bool> AddOrUpdate(OrderItemAdd item);
        Task<bool> AddOrUpdateInfoOrder(OrderInfoItemAdd item);
        Task<bool> ChangeStatusApply(dynamic item);
        Task<bool> Assignee(OrderAssingeeAdd item);
        Task<bool> OrderResultRequest(OrderResultRequest item);
        Task<bool> ChangeReturnOrder(OrderResultRequest item);
        Task<bool> ChangePushCaseCTV(OrderResultRequest item);

        Task<bool> AddImpact(OrderImpactHIstoryAdd item);
        Task<bool> Delete(int id);
        Task<bool> DeleteImpact(int id);
        Task<Order> GetById(int id);
        Task<Order> GetByCode(string orderCode);
        Task<OrderInfoIndexModel> GetInfo(int id);
        Task<BaseList> GetAllImpact(OrderRequest request);
        Task<BaseList> GetAll(OrderRequest request);
        Task<BaseList> GetALlHistory(OrderRequest request);
        Task<bool> AddCandidateOrderMarketting(OrderCandidateMarkettingItemAdd item);

        Task<BaseList> GetAllOrderReport(OrderReportRequest request);


        Task<bool> DeleteOrder(int id);
    }
}
