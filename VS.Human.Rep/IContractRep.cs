using VS.Human.Item;
using VS.Human.Rep.Model;

namespace VS.Human.Rep
{
    public interface IContractRep
    {
        Task<BaseList> GetAll(ContractRequest request);
        Task<Contract?> GetById(int id);
        Task<int> Add(Contract item);
        Task<bool> Update(Contract item);
        Task<bool> Delete(int id, int userId);
        Task<bool> AddHistory(ContractHistory history);
        Task<bool> SignInternal(int id, int signedBy, DateTime signedAt, string signatureHash, string fileHash, string? signNote, string signMethod, string? signedByUserNameSnapshot, string? signedByFullNameSnapshot, string? signedIpAddress, string? signedUserAgent, string? signedFileArchivePath, string? signatureImagePath, string? signatureIntentText, DateTime? termsAcceptedAt);
        Task<bool> SignInternalHr(int id, int signedBy, DateTime signedAt, string signatureHash, string fileHash, string? signNote, string signMethod, string? hrSignedByUserNameSnapshot, string? hrSignedByFullNameSnapshot, string? hrSignedIpAddress, string? hrSignedUserAgent);
        Task<bool> SetOriginalFileHash(int id, string hash);
        Task<List<ContractHistory>> GetHistory(int contractId);
        Task<List<Contract>> GetExpiring(int days);
        Task<List<ContractStatusCount>> GetStatusCounts();
    }
}
