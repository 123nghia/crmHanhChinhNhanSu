using VS.Human.Item;
using VS.Human.Rep.Model;

namespace VS.Human.Rep
{
    public interface IFormTemplateRep
    {
        Task<BaseList> GetAll(FormTemplateRequest request);
        Task<FormTemplateItem> GetById(int id);
        Task<bool> Save(FormTemplateItem item);
        Task<bool> Delete(int id);
    }
}
