using VS.Human.Item;
using VS.Human.Rep.Model;

namespace VS.Human.Business
{
    public interface IMailGroupBusiness
    {
        Task<BaseList> GetAll(MailGroupRequest request);
        Task<List<MailGroupItem>> GetSelectableGroupsAsync();
        Task<MailGroupItem> GetById(int id);
        Task<bool> Save(MailGroupItem item);
        Task<bool> Delete(int id);
        Task<List<string>> ResolveRecipientsAsync(IEnumerable<int> groupIds);
    }
}
