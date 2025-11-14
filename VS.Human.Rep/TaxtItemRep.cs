using Microsoft.Extensions.Configuration;
using VS.Human.Rep.Model;

namespace VS.Human.Rep
{
    public class TaxtItemRep : RepositoryBase<TaxItem>, ITaxtItemRep
    {

        public TaxtItemRep(IConfiguration configuration)
            : base(configuration)
        {

            tableName = "TaxItem";
            sqlGetALl = "sp_tax_getAll";
        }

        private async Task<bool> Update(TaxItem item)
        {
            var parameter = new
            {
                item.Id,
                item.UserName,
                item.CodeId,
                item.Number,
                item.RegBHYT,
                item.PageTax,
                item.BiaSo,
                item.UpdatedBy
     
            };
            return await this.ExecuteSQL("sp_tax_udpate", parameter);

        }

        private async Task<bool> Add(TaxItem item)
        {
            var parameter = new
            {
                item.UserName,
                item.CodeId,
                item.Number,
                item.PageTax,
                item.BiaSo,
                item.RegBHYT,
                item.CreatedBy
    
            };
            return await this.ExecuteSQL("sp_tax_insert", parameter);
        }

        public async Task<bool> AddOrUpdate(TaxItem item)
        {
            if (item.Id > 0)
            {
                var itemUpdate = await GetById(item.Id);
                if (itemUpdate != null)
                {
                    itemUpdate.BiaSo = item.BiaSo;
                    itemUpdate.PageTax = item.PageTax;
                    itemUpdate.CodeId = item.CodeId;
                    itemUpdate.UpdatedBy = item.UpdatedBy;
                    itemUpdate.RegBHYT = item.RegBHYT;
 
                    return await Update(itemUpdate);
                }
            }
            return await Add(item);
        }

        public async Task<TaxItem> GetAll(string userid)
        {
            var data = await GetFirstRecordBySql<TaxItem>("sp_tax_getAll", new
            {
                UserName = userid
            });
            return data;
        }
        public async Task<TaxItem> GetInfo(string userName)
        {
            var data = await GetFirstRecordBySql<TaxItem>("sp_tax_getAll", new
            {
                userid = userName
            });
            return data;
        }

    }
}
