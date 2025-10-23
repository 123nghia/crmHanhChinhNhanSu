using Microsoft.Extensions.Configuration;
using VS.Human.Item;
using VS.Human.Rep.Model;

namespace VS.Human.Rep
{
    public class DocumentDataRep : RepositoryBase<DocumentData>, IDocumentDataRep
    {

        public DocumentDataRep(IConfiguration configuration)
            : base(configuration)
        {

            tableName = "DocumentData";
            sqlGetALl = "sp_DocumentData_getAll";
        }

        private async Task<bool> Update(DocumentData item)
        {
            var parameter = new
            {
                item.Id,
                item.ValueFile,
                item.CreatedBy
            };
            return await this.ExecuteSQL("sp_DocumentData_update", parameter);
        }
        private async Task<bool> Add(DocumentData item)
        {
            var parameter = new
            {
                item.RelId,
                item.RelCode,
                item.DisplayText,
                item.ValueFile,
                item.Code,

                item.CreatedBy
            };
            return await this.ExecuteSQL("sp_DocumentData_insert", parameter);
        }

        public async Task<bool> AddOrUpdate(DocumentData item)
        {
            if (item.Id > 0)
            {
                var itemUpdate = await GetById(item.Id);
                itemUpdate.ValueFile = item.ValueFile;
                itemUpdate.UpdatedBy = item.CreatedBy;
                if (itemUpdate != null)
                {

                    return await Update(itemUpdate);
                }
            }
            return await Add(item);
        }

        public async Task<BaseList> GetAll(DocumentDataRquest request)
        {
            var result = await GetBaseAll<DocumentDataIndexModel>(request,
            new
            {
                request.Token,
                request.From,
                request.To,
                request.RelId,
                request.RelCode,
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
