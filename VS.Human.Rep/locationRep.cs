using Microsoft.Extensions.Configuration;
using VS.Human.Item;
using VS.Human.Rep.Model;

namespace VS.Human.Rep
{
    public class locationRep : RepositoryBase<Partner>, IlocationRep
    {

        public locationRep(IConfiguration configuration)
            : base(configuration)
        {

            tableName = "regionals";
            sqlGetALl = "sp_regionals_getAll";
        }

        public async Task<BaseList> GetAll()
        {
            var result = await GetBaseAll<RegionalIndexModel>(new BaseRequest() { },
            new
            {

            });
            return result;
        }


    }
}
