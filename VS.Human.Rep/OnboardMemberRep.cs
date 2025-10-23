using Microsoft.Extensions.Configuration;
using VS.Human.Item;
using VS.Human.Rep.Model;

namespace VS.Human.Rep
{
    public class OnboardMemberRep : RepositoryBase<OnboardMember>, IOnboardMemberRep
    {

        public OnboardMemberRep(IConfiguration configuration)
            : base(configuration)
        {

            tableName = "OnboardMember";
        }

        private async Task<bool> Update(OnboardMember item)
        {
            var parameter = new
            {
                item.Id,
                item.CVLink,
                item.Source,
                item.Status,
                item.IsActive,
                item.UpdatedBy,
                item.ShortDes,
                item.Noted
            };
            return await this.ExecuteSQL("sp_candidate_update", parameter);
        }
        private async Task<bool> Add(OnboardMember item)
        {
            var parameter = new
            {
                item.JobId,
                item.Status,
                item.SystemStatus,
                item.CandidateId,
                item.PartnerId,
                item.ProjectId,
                item.OrderCode,
                item.Source,
                item.ShortDes,
                item.OnboardDate,
                item.Assignee,
                item.Noted,
                item.CreatedBy,
                item.CVLink,
                item.Warrantydate,
                item.Dpd

            };
            return await this.ExecuteSQL("sp_OnboardMember_insert", parameter);
        }

        public async Task<bool> AddOrUpdate(OnboardMember item)
        {
            if (item.Id > 0)
            {
                var itemUpdate = await GetById(item.Id);
                if (itemUpdate != null)
                {

                }
            }
            return await Add(item);
        }

        public async Task<bool> UpdateOnboard(dynamic item)
        {

            return await this.ExecuteSQL("sp_OnboardMember_update", item);
        }

        public async Task<bool> UpdateOnboardStatus()
        {

            return await this.ExecuteSQL("sp_UpdateOnboardStatus", new
            {

            });
        }


        public async Task<BaseList> GetAll(OrderRequest request)
        {
            var sqlText = "sp_onboard_getAll";
            if (request.RoleCode == "4")
            {
                sqlText = "sp_onboard_getAllMarketting";
            }
            var result = await GetBaseAll<OnboardMemberIndexModel>(request,
            new
            {

                request.UserId,
                request.Limit,
                request.Status,
                request.From,
                request.To,
                request.Job,
                request.GroupId,
                request.MemberId,
                request.Page
            }, sqlText);
            return result;
        }
        public async Task<bool> AddCandidateWidthOrder(dynamic request)
        {
            var sqlText = "sp_AddCandidateAndOrder";

            return await this.ExecuteSQL(sqlText, request);
        }


    }
}
