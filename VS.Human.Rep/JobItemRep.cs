using Microsoft.Extensions.Configuration;
using VS.Human.Item;
using VS.Human.Rep.Model;

namespace VS.Human.Rep
{
    public class JobItemRep : RepositoryBase<JobItem>, IJobItemRep
    {

        public JobItemRep(IConfiguration configuration)
            : base(configuration)
        {

            tableName = "JobItem";
            sqlGetALl = "sp_jobItem_getAll";
        }

        private async Task<bool> Update(JobItem item)
        {
            var parameter = new
            {
                item.Id,
                item.Name,
                item.UpdatedBy,
                item.Field,
                item.CareerId,
                item.Content,
                item.ShortDes,
                item.Noted,
                item.WarrantyDate,
                item.Status,
                item.IsActive,
                item.ProjectId,
                item.PartnerId,
                item.Inputfile
            };
            return await this.ExecuteSQL("sp_jobItem_update", parameter);
        }
        private async Task<bool> Add(JobItem item)
        {
            var parameter = new
            {
                item.Name,
                item.Field,
                item.CareerId,
                item.Content,
                item.ShortDes,
                item.Noted,
                item.Status,
                item.CreatedBy,
                item.ProjectId,
                item.PartnerId,
                item.IsActive,
                item.WarrantyDate,
                item.Inputfile
            };
            return await this.ExecuteSQL("sp_jobItem_insert", parameter);
        }

        public async Task<bool> AddOrUpdate(JobItem item)
        {
            if (item.Id > 0)
            {
                var itemUpdate = await GetById(item.Id);
                if (itemUpdate != null)
                {
                    itemUpdate.Name = item.Name;
                    itemUpdate.Field = item.Field;
                    itemUpdate.CareerId = item.CareerId;
                    itemUpdate.Content = item.Content;
                    itemUpdate.ShortDes = item.ShortDes;
                    itemUpdate.Noted = item.Noted;
                    itemUpdate.WarrantyDate = item.WarrantyDate;

                    itemUpdate.IsActive = item.IsActive;
                    itemUpdate.UpdatedBy = item.UpdatedBy;
                    itemUpdate.Inputfile = item.Inputfile;
                    itemUpdate.PartnerId = item.PartnerId;
                    itemUpdate.ProjectId = item.ProjectId;
                    return await Update(itemUpdate);
                }
            }
            return await Add(item);
        }

        public async Task<BaseList> GetAll(JobRequest request)
        {
            var result = await GetBaseAll<JobIndexModel>(request,
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

        public async Task<BaseList> GetAll2(JobRequest2 request)
        {
            var result = await GetBaseAll<JobIndexModel>(request,
            new
            {
                request.PartnerId,
                request.ProjectId,
                request.LinhvucId

            }, "sp_jobItem_getAll2");
            return result;
        }



    }
}
