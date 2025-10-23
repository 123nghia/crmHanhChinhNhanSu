using Microsoft.Extensions.Configuration;
using VS.Human.Item;
using VS.Human.Rep.Model;

namespace VS.Human.Rep
{
    public class PartnerRep : RepositoryBase<Partner>, IPartnerRep
    {

        public PartnerRep(IConfiguration configuration)
            : base(configuration)
        {

            tableName = "Partner";
            sqlGetALl = "sp_Partner_getAll";
        }

        private async Task<bool> Update(Partner item)
        {
            var parameter = new
            {
                item.Id,
                item.Name,
                item.ShortName,
                item.TaxCode,

                item.UpdatedBy,

            };
            return await this.ExecuteSQL("sp_partner_update", parameter);
        }
        private async Task<bool> Add(Partner item)
        {
            var parameter = new
            {
                item.Name,
                item.ShortName,
                item.TaxCode,
                item.CreatedBy
            };
            return await this.ExecuteSQL("sp_partner_insert", parameter);
        }




        public async Task<bool> AddOrUpdate(Partner item)
        {
            if (item.Id > 0)
            {
                var itemUpdate = await GetById(item.Id);
                if (itemUpdate != null)
                {
                    itemUpdate.Name = item.Name;
                    itemUpdate.ShortName = item.ShortName;
                    itemUpdate.TaxCode = item.TaxCode;

                    itemUpdate.UpdatedBy = item.UpdatedBy;

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

        //public async Task<bool> Delete(int id)
        //{
        //    return await DeleteBase(id, tableDelete: "Partner");
        //}
    }
}
