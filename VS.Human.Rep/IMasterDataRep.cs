using VS.Human.Item;
using VS.Human.Rep.Model;

namespace VS.Human.Rep
{
    public interface IMasterDataRep
    {
        Task<bool> AddOrUpdate(MasterData item);

        Task<bool> Delete(int id);

        Task<MasterData> GetById(int id);
        Task<MasterData?> GetByCode(string code, int typeData);
        Task<MasterData?> GetByName(string name, int typeData);



        Task<BaseList> GetAll(CommonRequest request);
        Task<List<MasterData>> GetByTypeData(int typeData);
        Task<int> GetNextAvailableTypeData();



    }
}
