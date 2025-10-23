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

        Task<BaseList> GetallRegional();
    }
}
