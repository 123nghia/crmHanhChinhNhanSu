using VS.Human.Item;
using VS.Human.Rep.Model;

namespace VS.Human.Rep
{
    public interface ICandidateRep
    {
        Task<bool> AddOrUpdate(Candidate item);

        Task<bool> Delete(int id);

        Task<Candidate> GetById(int id);

        Task<BaseList> GetAll(CandidateRequest request);

        Task<bool> AddCandidateWidthOrder(dynamic item);
        public Task<bool> DeleteReal(int id);
    }
}
