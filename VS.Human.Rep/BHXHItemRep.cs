using Microsoft.Extensions.Configuration;
using VS.Human.Item;
using VS.Human.Rep.Model;

namespace VS.Human.Rep
{
    public class BHXHItemRep : RepositoryBase<BHXHItem>, IBHXHItemRep
    {

        public BHXHItemRep(IConfiguration configuration)
            : base(configuration)
        {

            tableName = "BHXHItem";
            sqlGetALl = "sp_BHXHItem_getAll";
        }

        private async Task<bool> Update(BHXHItem item)
        {
            var parameter = new
            {
                item.Id,
                item.Relid,
                item.NumberCode,
                item.PITDate,
                item.EffectedFrom,
                item.StartMonth,
                item.RegBHYT,
                item.Number,
                item.RegPageNumber,
                item.UpdatedBy,

            };
            return await this.ExecuteSQL("sp_BHXHItem_update", parameter);
        }

        private async Task<bool> Add(BHXHItem item)
        {
            var parameter = new
            {
                item.UserName,
                item.NumberCode,
                item.Relid,
                item.PITDate,
                item.EffectedFrom,
                item.StartMonth,
                item.RegBHYT,
                item.Number,
                item.RegPageNumber,
                item.CreatedBy
               
            };
            return await this.ExecuteSQL("sp_BHXHItem_insert", parameter);
        }
        public async Task<bool> AddOrUpdate(BHXHItem item)
        {
            if (item.Id > 0)
            {
                var itemUpdate = await GetById(item.Id);
                if (itemUpdate != null)
                {
                    itemUpdate.Number = item.Number;   
                    itemUpdate.RegBHYT = item.RegBHYT;
                    itemUpdate.RegPageNumber = item.RegPageNumber;
                    itemUpdate.UpdatedBy = item.UpdatedBy;
                    itemUpdate.NumberCode = item.NumberCode;
                    itemUpdate.EffectedFrom = item.EffectedFrom;
                    itemUpdate.PITDate = item.PITDate;
                    itemUpdate.StartMonth = item.StartMonth;

                    return await Update(itemUpdate);
                }
            }
            return await Add(item);
        }

        public async Task<BaseList> GetAll(CommonRequest request)
        {
            var result = await GetBaseAll<CommonIndexModel>(request,
            new
            {
                request.Token,
                request.From,
                request.To,
                request.UserId,
                request.Limit,
                request.Page,
                request.OrderBy
            });
            return result;
        }



        public async Task<BHXHItem> GetInfo(string userName)
        {
            var data = await GetFirstRecordBySql<BHXHItem>("sp_BHXHItem_getAll", new
            {
                UserName = userName
            });
            return data;
        }
    }
}
