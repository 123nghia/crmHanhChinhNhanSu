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
        Task<bool> SignInternal(int id, int signedBy, DateTime signedAt, string signatureHash, string fileHash, string? signNote, string signMethod);
        Task<bool> SignInternalHr(int id, int signedBy, DateTime signedAt, string signatureHash, string fileHash, string? signNote, string signMethod);
        Task<List<ContractHistory>> GetHistory(int contractId);
        Task<List<Contract>> GetExpiring(int days);
        Task<List<ContractStatusCount>> GetStatusCounts();
    }
}
