using VS.Human.Item;
using VS.Human.Rep.Model;

namespace VS.Human.Rep
{
    public interface IParrentChildRep
    {
        Task<bool> AddOrUpdate(ParrentChild parrentChild);
        Task<BaseList> GetAll(int? Rel, int? Type);



    }
}
