using VS.Human.Item;
using VS.Human.Rep.Model;

namespace VS.Human.Rep
{
    public interface ISupportRequestRep
    {
        Task<int> Create(SupportRequestItem item, int userId);
        Task<BaseList> GetAll(SupportRequestListRequest request);
        Task<SupportRequestIndexModel?> GetById(int id);
        Task<List<SupportRequestHistory>> GetHistory(int requestId);
        Task<bool> UpdateStatus(int id, int status, string? comment, int updatedBy);
        Task<bool> Delete(int id, int userId);
    }
}
