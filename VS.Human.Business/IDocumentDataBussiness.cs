using VS.Human.Business.Model;
using VS.Human.Item;
using VS.Human.Rep.Model;

namespace VS.Human.Business
{
    public interface IDocumentDataBussiness
    {

        Task<bool> AddOrUpdate(DocumentDataAddRequest item);

        Task<bool> Delete(int id);

        Task<DocumentData> GetById(int id);

        Task<BaseList> GetAll(DocumentDataRquest request);
        Task<BaseList> GetDocuments(int? parentId, int currentUserId, int page = 1, int limit = 20);
        Task<int> CreateFolder(string name, int? parentId, int userId);
        Task<int> SaveFile(string name, string filePath, int? parentId, int userId);
        Task<List<int>> GetShares(int documentId);
        Task<bool> UpdateShares(int documentId, List<int> userIds);
        Task<bool> UpdateAccessLevel(int documentId, int accessLevel, int userId);
        Task<DocumentData?> GetByToken(string token);
        Task<bool> UpdateDisplayText(int id, string text, int userId);
        Task<bool> RequestInternalSign(int id, int requestedBy, DateTime requestedAt);
        Task<bool> SignInternal(int id, int signedBy, DateTime signedAt, string signatureHash, string fileHash, string? signNote, string signMethod, string? signedByUserNameSnapshot, string? signedByFullNameSnapshot, string? signedIpAddress, string? signedUserAgent, string? signedFileArchivePath, string? signatureImagePath, string? signatureIntentText, DateTime? termsAcceptedAt = null);
        Task<BaseList> GetallRegional();
    }
}
