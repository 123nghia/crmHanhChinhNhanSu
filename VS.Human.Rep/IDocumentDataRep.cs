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


    }
}
