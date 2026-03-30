using VS.Human.Item;
using VS.Human.Rep.Model;

namespace VS.Human.Business
{
    public interface ISupportRequestBusiness
    {
        Task<BaseList> GetAll(SupportRequestListRequest request);
        Task<SupportRequestIndexModel?> GetById(int id);
        Task<List<SupportRequestHistory>> GetHistory(int requestId);
        Task<(bool Success, string Message, int Id)> Create(SupportRequestItem item, int userId);
        Task<(bool Success, string Message)> UpdateStatus(SupportRequestStatusUpdate model, int userId, string? roleCode);
        Task<(bool Success, string Message)> Delete(int id, int userId, string? roleCode);
    }
}
