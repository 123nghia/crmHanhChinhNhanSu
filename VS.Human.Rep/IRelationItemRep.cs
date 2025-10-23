using VS.Human.Rep.Model;

namespace VS.Human.Rep
{
    public interface IRelationItemRep
    {
        Task<bool> AddOrUpdate(RelationItem itemAdd);

        Task<bool> Delete(int id);

        Task<RelationItem> GetInfo(string userName);

        Task<RelationItem> GetAll(string userName);
    }
}
