using VS.Human.Item;
using VS.Human.Rep.Model;

namespace VS.Human.Rep
{
    public interface IOrderRep
    {
        Task<bool> Assignee(dynamic item);
        Task<bool> OrderResultRequest(dynamic item);

        Task<bool> ChangeReturnOrder(dynamic item);
        Task<bool> ChangePushCaseCTV(dynamic item);


        Task<bool> AddOrUpdate(Order item);
        Task<bool> AddOrUpdateInfoOrder(dynamic item);
        Task<bool> ChangeStatusApply(dynamic item);
        Task<bool> AddImpact(OrderImpactHIstory item);
        Task<bool> Delete(int id);
        public Task<bool> DeleteReal(int id);
        Task<bool> DeleteImpact(int id);
        Task<Order> GetById(int id);

        Task<Order> GetByCode(string orderCode);
        Task<OrderInfoIndexModel> GetOrderInfo(int id);
        Task<BaseList> GetAllImpact(OrderRequest request);
        Task<bool> ImportSourMarketting(dynamic request);
        Task<BaseList> GetAllImpactReport(OrderRequest request);

        Task<BaseList> GetAllOrderStatus(OrderRequest request);

        Task<BaseList> GetALlHistory(OrderRequest request);
        Task<BaseList> GetAll(OrderRequest request);
        Task<BaseList> GetAllOrderReport(OrderReportRequest request);

        Task<bool> AddCandidateOrder(dynamic item);
        Task<bool> UpdateCandidateOrder(dynamic item);
    }
}
