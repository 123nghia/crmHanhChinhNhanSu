using VS.Human.Item;
using VS.Human.Rep.Model;

namespace VS.Human.Rep
{
    public interface IDocumentDataRep
    {
        Task<bool> AddOrUpdate(DocumentData item);
        Task<bool> Delete(int id);
        Task<DocumentData> GetById(int id);
        Task<BaseList> GetAll(DocumentDataRquest request);
        Task<List<int>> GetShares(int documentId);
        Task<bool> AddShare(int documentId, int userId);
        Task<bool> RemoveShare(int documentId, int userId);
        Task<DocumentData?> GetByToken(string token);
    }
}
