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
        Task<bool> RequestInternalSign(int id, int requestedBy, DateTime requestedAt);
        Task<bool> SignInternal(int id, int signedBy, DateTime signedAt, string signatureHash, string fileHash, string? signNote, string signMethod, string? signedByUserNameSnapshot, string? signedByFullNameSnapshot, string? signedIpAddress, string? signedUserAgent, string? signedFileArchivePath, string? signatureImagePath, string? signatureIntentText, DateTime? termsAcceptedAt = null);
    }
}
