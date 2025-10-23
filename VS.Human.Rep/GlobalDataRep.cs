using Microsoft.Extensions.Configuration;
using VS.Human.Item;

namespace VS.Human.Rep
{
    public class GlobalDataRep : RepositoryBaseNoTModel, IGlobalDataRep
    {

        public GlobalDataRep(IConfiguration configuration)
            : base(configuration)
        {
            tableName = "";
            sqlGetALl = "sp_Partner_getAll";
        }

        public async Task<int> GetCountSource()
        {
            var result = await this.ExecuteSQL<GlobaleDataIndex>("sp_Order_GetCountSource",
            new
            {

            });

            var fist = result.FirstOrDefault();
            if (fist != null)
            {
                return fist.Total;
            }
            return 0;

        }
        public async Task<int> GetCountSourceCTV()
        {
            var result = await this.ExecuteSQL<GlobaleDataIndex>("sp_Order_GetCountSourceCTV",
            new
            {

            });
            var fist = result.FirstOrDefault();
            if (fist != null)
            {
                return fist.Total;
            }

            return 0;
        }

    }
}
