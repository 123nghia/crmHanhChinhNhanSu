using Microsoft.Extensions.Configuration;
using VS.Human.Item;
using VS.Human.Rep.Model;

namespace VS.Human.Rep
{
    public class ParrentChildRep : RepositoryBaseNoTModel, IParrentChildRep
    {

        public ParrentChildRep(IConfiguration configuration)
            : base(configuration)
        {

            tableName = "MasterData";
            sqlGetALl = "sp_masterData_getAll";
        }


        private async Task<bool> Update(ParrentChild item)
        {
            var parameter = new
            {
                item.Text,
                item.RelId,
                item.Type,
                item.Id
            };
            return await this.ExecuteSQL("sp_parrentChild_update", parameter);
        }
        private async Task<bool> Add(ParrentChild item)
        {
            var parameter = new
            {
                item.Text,
                item.RelId,
                item.Type
            };
            return await this.ExecuteSQL("sp_parrentChild_insert", parameter);
        }


        public async Task<BaseList> GetAll(int? Rel, int? Type)
        {
            var result = await GetBaseAll<ParrentChildIndexModel>(new BaseRequest() { },
             new
             {
                 Rel,
                 Type
             }, "sp_ParrentChild_getAll");
            return result;
        }

        public async Task<bool> AddOrUpdate(ParrentChild parrentChild)
        {
            if (parrentChild.Id < 1)
            {
                return await Add(parrentChild);
            }
            return await Update(parrentChild);

        }
    }
}
