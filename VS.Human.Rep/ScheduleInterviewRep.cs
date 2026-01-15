using Microsoft.Extensions.Configuration;
using VS.Human.Item;
using VS.Human.Rep.Model;

namespace VS.Human.Rep
{
    public class ScheduleInterviewRep : RepositoryBase<ScheduleInterview>, IScheduleInterviewRep
    {

        public ScheduleInterviewRep(IConfiguration configuration)
            : base(configuration)
        {

            tableName = "ScheduleInterview";
            sqlGetALl = "sp_ScheduleInterview_getAll";
        }

        private async Task<bool> Update(ScheduleInterview item)
        {
            var parameter = new
            {
                item.Id,
                item.RelId,
                item.RelCode,
                item.Type,
                item.ScheduleDate,
                item.AddressInfo,
                item.Noted,
                item.Status,
                item.InterviewerId,
                item.InterviewMode,
                item.InterviewResult,
                item.UpdatedBy
            };
            return await this.ExecuteSQL("sp_ScheduleInterview_update", parameter);
        }
        private async Task<bool> Add(ScheduleInterview item)
        {
            var parameter = new
            {
                item.RelId,
                item.RelCode,
                item.AddressInfo,
                item.ScheduleDate,
                item.Noted,
                item.Type,
                item.Status,
                item.InterviewerId,
                item.InterviewMode,
                item.InterviewResult,
                item.CreatedBy
            };
            return await this.ExecuteSQL("sp_ScheduleInterview_insert", parameter);
        }

        public async Task<bool> AddOrUpdate(ScheduleInterview item)
        {
            if (item.Id > 0)
            {
                var itemUpdate = await GetById(item.Id);
                if (itemUpdate != null && itemUpdate.Id > 0)
                {

                    return await Update(item);
                }
            }
            return await Add(item);
        }

        public async Task<BaseList> GetAll(ScheduleInterviewRquest request)
        {
            var result = await GetBaseAll<ScheduleInterviewIndexModel>(request,
            new
            {
                request.Token,
                request.From,
                request.To,
                request.Type,
                request.Status,
                request.InterviewerId,
                request.InterviewMode,
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
