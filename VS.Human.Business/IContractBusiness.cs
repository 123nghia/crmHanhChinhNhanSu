using VS.Human.Item;
using VS.Human.Rep.Model;

namespace VS.Human.Business
{
    public interface IContractBusiness
    {
        Task<BaseList> GetAll(ContractRequest request);
        Task<Contract?> GetById(int id);
        Task<bool> Add(Contract item, int userId);
        Task<bool> Update(Contract item, int userId);
        Task<bool> Delete(int id, int userId);
        Task<bool> SignInternalHr(int contractId, int signedBy, DateTime signedAt, string signatureHash, string fileHash, string? signNote, string signMethod);
        Task<bool> SignInternal(int contractId, int signedBy, DateTime signedAt, string signatureHash, string fileHash, string? signNote, string signMethod);
        Task<List<ContractHistory>> GetHistory(int contractId);
        Task<List<Contract>> GetExpiring(int days);
        Task<List<ContractStatusCount>> GetStatusCounts();
    }
}
