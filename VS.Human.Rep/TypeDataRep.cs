using Microsoft.Extensions.Configuration;
using VS.Human.Item;
using VS.Human.Rep.Model;

namespace VS.Human.Rep
{
    public class TypeDataRep : RepositoryBase<TypeData>, ITypeDataRep
    {

        public TypeDataRep(IConfiguration configuration)
            : base(configuration)
        {

            tableName = "TypeData";
        }

        private async Task<bool> Update(Candidate item)
        {
            var parameter = new
            {
                item.Id,
                item.Name,
                item.Dob,
                item.Email,

                item.CVLink,
                item.Source,
                item.Phone,
                item.Status,
                item.IsActive,
                item.UpdatedBy,
                item.ShortDes,
                item.Noted
            };
            return await this.ExecuteSQL("sp_candidate_update", parameter);
        }
        private async Task<bool> Add(Candidate item)
        {
            var parameter = new
            {
                item.Name,
                item.ShortDes,
                item.Noted,
                item.Phone,
                item.Email,
                item.Dob,
                item.Source,
                item.Status,
                item.CreatedBy,

                item.CVLink,
                item.IsActive
            };
            return await this.ExecuteSQL("sp_candidate_insert", parameter);
        }

        public async Task<bool> AddOrUpdate(Candidate item)
        {
            if (item.Id > 0)
            {
                var itemUpdate = await GetById(item.Id);
                if (itemUpdate != null)
                {
                    //itemUpdate.Name = item.Name;
                    //itemUpdate.CVLink = item.CVLink;
                    //itemUpdate.AvatarLink = item.AvatarLink;
                    //itemUpdate.Status = item.Status;
                    //itemUpdate.ShortDes = item.ShortDes;
                    //itemUpdate.Noted = item.Noted;
                    //itemUpdate.Status = item.Status;
                    //itemUpdate.Email = item.Email;
                    //itemUpdate.Source = item.Source;
                    //item.Dob = item.Dob;
                    //itemUpdate.IsActive = item.IsActive;
                    //itemUpdate.Phone = item.Phone;
                    //itemUpdate.UpdatedBy = item.UpdatedBy;
                    //return await Update(itemUpdate);
                }
            }
            return await Add(item);
        }

        public async Task<BaseList> GetAll(CandidateRequest request)
        {
            var sqlText = "sp_candidate_getAll";
            if (request.RoleCode == "4" || request.RoleCode == "6" || request.RoleCode == "7")
            {
                sqlText = "sp_candidate_getAllMarketting";
            }
            var result = await GetBaseAll<CandidateIndexModel>(request,
            new
            {
                request.Token,
                request.UserId,
                request.Limit,
                request.LoadAll,
                request.GroupId,
                request.MemberId,

                request.Page

            }, sqlText);
            return result;
        }

        public async Task<BaseList> GetAlLCandidateOfMember(CandidateRequest request)
        {
            var sqlText = "sp_candidateOfMember_getAll";
            if (request.RoleCode == "4")
            {
                sqlText = "sp_candidateOfMember_getAllMarketting";
            }
            var result = await GetBaseAll<CandidateIndexModel>(request,
            new
            {
                request.Token,
                request.UserId,
                request.Limit,
                request.LoadAll,
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

        Task<Candidate> ITypeDataRep.GetById(int id)
        {
            throw new NotImplementedException();
        }
    }
}
