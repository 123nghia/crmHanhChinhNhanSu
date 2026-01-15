using Microsoft.Extensions.Configuration;
using VS.Human.Item;
using VS.Human.Rep.Model;

namespace VS.Human.Rep
{
    public class CandidateRep : RepositoryBase<Candidate>, ICandidateRep
    {

        public CandidateRep(IConfiguration configuration)
            : base(configuration)
        {

            tableName = "Candidate";
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
                item.DepartmentId,
                item.Position,
                item.StatusHuman,
                item.ManagerId,
                item.Referrer,
                item.Noted,
                item.NationalId,
                item.Address,
                item.UserName,
                item.Pass,
                item.IsEmployee,
                item.EmployeeId
            };
            return await this.ExecuteSQL("sp_candidate_update", parameter);
        }
        private async Task<bool> Add(Candidate item)
        {

            item.Status = 91;

            var parameter = new
            {
                item.Name,
                item.ShortDes,
                item.Noted,
                item.Position,
                item.DepartmentId,
                item.Phone,
                item.Email,
                item.Dob,
                item.Source,
                item.ManagerId,
                item.Status,
                item.CreatedBy,
                item.CVLink,
                item.IsActive,
                item.NationalId,
                item.Address,
                item.UserName,
                item.Pass
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
                    itemUpdate.Name = item.Name;
                    itemUpdate.CVLink = item.CVLink;
                    itemUpdate.Status = item.Status;
                    itemUpdate.ShortDes = item.ShortDes;
                    itemUpdate.Noted = item.Noted;
                    itemUpdate.Status = item.Status;
                    itemUpdate.Email = item.Email;
                    itemUpdate.Source = item.Source;
                    itemUpdate.ManagerId = item.ManagerId;
                    itemUpdate.Dob = item.Dob;
                    itemUpdate.IsActive = item.IsActive;
                    itemUpdate.Phone = item.Phone;
                    itemUpdate.UpdatedBy = item.UpdatedBy;
                    itemUpdate.StatusHuman = item.StatusHuman;
                    itemUpdate.DepartmentId = item.DepartmentId;
                    itemUpdate.Position = item.Position;
                    itemUpdate.UpdatedBy = item.UpdatedBy;
                    itemUpdate.Referrer = item.Referrer;
                    itemUpdate.NationalId = item.NationalId;
                    itemUpdate.Address = item.Address;
                    if (!string.IsNullOrWhiteSpace(item.UserName))
                    {
                        itemUpdate.UserName = item.UserName;
                    }
                    if (!string.IsNullOrWhiteSpace(item.Pass))
                    {
                        itemUpdate.Pass = item.Pass;
                    }
                    if (item.IsEmployee.HasValue)
                    {
                        itemUpdate.IsEmployee = item.IsEmployee;
                    }
                    if (item.EmployeeId.HasValue)
                    {
                        itemUpdate.EmployeeId = item.EmployeeId;
                    }

                    return await Update(itemUpdate);
                }
            }
            return await Add(item);


        }

        public async Task<BaseList> GetAll(CandidateRequest request)
        {
            var sqlText = "sp_candidate_getAll";

            var result = await GetBaseAll<CandidateIndexModel>(request,
            new
            {
                request.Token,
                request.UserId,
                request.Limit,
                request.LoadAll,
                request.GroupId,
                request.MemberId,
                request.ManagerId,
                request.DocumentStatus,
                request.CandidateStatus,
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

        public async Task<Candidate> Login(string userName, string password)
        {
            var modelCheck = new
            {
                userName,
                password
            };
            var result = await ExecuteSQL<Candidate>("sp_candidate_login", modelCheck);
            return result;
        }

        public async Task<bool> ChangePassword(string password, int id)
        {
            var parameter = new
            {
                password,
                id
            };

            return await this.ExecuteSQL("sp_candidate_changePassword", parameter);
        }

        public async Task<Candidate> GetByUserName(string userName)
        {
            var parameter = new { userName };
            var sql = "SELECT TOP 1 * FROM Candidate WHERE UserName = @userName AND ISNULL(Deleted,0)=0";
            return await ExecuteSQL<Candidate>(sql, parameter);
        }

        public async Task<bool> Delete(int id)
        {
            return await this.DeleteBase(id, tableDelete: "", delete: 1);
        }

        public async Task<bool> Delete(int id, bool reactive)
        {
            return await this.DeleteBase(id, tableDelete: "", delete: reactive ? 0 : 1);
        }

    }
}
