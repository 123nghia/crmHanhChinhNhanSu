using VS.Human.Business.Model;
using VS.Human.Item;
using VS.Human.Rep.Model;

namespace VS.Human.Business
{
    public interface ICandidateBusiness
    {

        Task<bool> Add(CandidateAdd item);
        Task<Candidate?> FindDuplicateForCreate(CandidateAdd item);
        Task<Candidate> GetById(int Id);
        Task<bool> Update(CandidateDetailUpdate item);
        Task<bool> UpdateProfile(CandidateProfileUpdate item);
        Task<bool> ChangePassword(string password, int id);
        Task<Candidate> Login(string userName, string password);
        Task<bool> ApprovePassInterview(int candidateId);
        Task<bool> ApprovePendingEmployee(int candidateId);
        Task<Employee?> Onboard(int candidateId);
        Task<bool> Delete(int id, bool reactive = false);
        Task<BaseList> GetAll(CandidateRequest request);
        Task<bool> HasViewAccess(int candidateId, int userId, string? roleCode);
        Task<bool> HasManageAccess(int candidateId, int userId, string? roleCode);
    }
}
