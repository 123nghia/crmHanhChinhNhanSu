using VS.Human.Item;
using VS.Human.Rep.Model;

namespace VS.Human.Business
{
    public interface IParrentChildBussiness
    {
        Task<bool> AddOrUpdate(ParrentChild parrentChild);
        Task<BaseList> GetAll(int? Rel, int? Type);
    }
}
