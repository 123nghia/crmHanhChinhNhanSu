using VS.Human.Item;
using VS.Human.Rep.Model;

namespace VS.Human.Business
{
    public interface IGroupBusiness
    {
        Task<bool> AddOrUpdate(GroupItem item);
        Task<bool> AddOrUpdateMember(GroupMember item);
        Task<bool> Delete(int id);
        Task<bool> DeleteMember(int id);
        Task<GroupItem> GetById(int id);
        Task<BaseList> GetAllMember(int groupId);
        Task<BaseList> GetAll(GroupRequest request);
        Task<BaseList> GetAllManager(int leadGroupId = -1);
        Task<BaseList> GetAllMemberNotGroup();

    }
}
