using Dapper;
using Microsoft.Extensions.Configuration;
using System.Data;
using VS.Human.Item;
using VS.Human.Rep.Model;

namespace VS.Human.Rep
{
    public class ContractRep : RepositoryBase<Contract>, IContractRep
    {
        public ContractRep(IConfiguration configuration)
            : base(configuration)
        {
            tableName = "Contracts";
            sqlGetALl = "sp_Contract_getAll";
        }

        public async Task<BaseList> GetAll(ContractRequest request)
        {
            var page = request.Page <= 0 ? 1 : request.Page;
            var limit = request.Limit <= 0 ? 20 : request.Limit;
            if (limit > 200) limit = 200;
            var offset = (page - 1) * limit;

            var p = new DynamicParameters();
            p.Add("@Token", request.Token ?? string.Empty);
            p.Add("@Status", string.IsNullOrWhiteSpace(request.Status) ? null : request.Status);
            p.Add("@ContractTypeCode", string.IsNullOrWhiteSpace(request.ContractTypeCode) ? null : request.ContractTypeCode);
            p.Add("@EmployeeId", request.EmployeeId);
            p.Add("@FromDate", request.From);
            p.Add("@ToDate", request.To);
            p.Add("@offset", offset);
            p.Add("@limit", limit);

            return await GetBaseAll<ContractIndexModel>(request, p, "sp_Contract_getAll");
        }

        public async Task<Contract?> GetById(int id)
        {
            var p = new DynamicParameters();
            p.Add("@Id", id);
            return await GetFirstRecordBySql<Contract>("sp_Contract_getById", p);
        }

        public async Task<int> Add(Contract item)
        {
            var p = new DynamicParameters();
            p.Add("@EmployeeId", item.EmployeeId);
            p.Add("@ContractTypeCode", item.ContractTypeCode);
            p.Add("@StartDate", item.StartDate);
            p.Add("@EndDate", item.EndDate);
            p.Add("@Status", item.Status);
            p.Add("@FileUrl", item.FileUrl);
            p.Add("@Note", item.Note);
            p.Add("@CreatedBy", item.CreatedBy);
            return await ExecuteSQLScalar<int>("sp_Contract_insert", p);
        }

        public async Task<bool> Update(Contract item)
        {
            var p = new DynamicParameters();
            p.Add("@Id", item.Id);
            p.Add("@ContractTypeCode", item.ContractTypeCode);
            p.Add("@StartDate", item.StartDate);
            p.Add("@EndDate", item.EndDate);
            p.Add("@Status", item.Status);
            p.Add("@Note", item.Note);
            p.Add("@UpdatedBy", item.UpdatedBy);
            return await ExecuteSQL("sp_Contract_update", p);
        }

        public async Task<bool> Delete(int id, int userId)
        {
            var p = new DynamicParameters();
            p.Add("@Id", id);
            p.Add("@UpdatedBy", userId);
            return await ExecuteSQL("sp_Contract_delete", p);
        }

        public async Task<bool> AddHistory(ContractHistory history)
        {
            var p = new DynamicParameters();
            p.Add("@ContractId", history.ContractId);
            p.Add("@EmployeeId", history.EmployeeId);
            p.Add("@Action", history.Action);
            p.Add("@ContractTypeCode", history.ContractTypeCode);
            p.Add("@StartDate", history.StartDate);
            p.Add("@EndDate", history.EndDate);
            p.Add("@Status", history.Status);
            p.Add("@FileUrl", history.FileUrl);
            p.Add("@Note", history.Note);
            p.Add("@IsHrSigned", history.IsHrSigned);
            p.Add("@HrSignedAt", history.HrSignedAt);
            p.Add("@HrSignedBy", history.HrSignedBy);
            p.Add("@HrSignatureHash", history.HrSignatureHash);
            p.Add("@HrFileHash", history.HrFileHash);
            p.Add("@HrSignNote", history.HrSignNote);
            p.Add("@HrSignMethod", history.HrSignMethod);
            p.Add("@HrSignedIpAddress", history.HrSignedIpAddress);
            p.Add("@HrSignedUserAgent", history.HrSignedUserAgent);
            p.Add("@HrSignedByUserNameSnapshot", history.HrSignedByUserNameSnapshot);
            p.Add("@HrSignedByFullNameSnapshot", history.HrSignedByFullNameSnapshot);
            p.Add("@IsSignedInternal", history.IsSignedInternal);
            p.Add("@SignedAt", history.SignedAt);
            p.Add("@SignedBy", history.SignedBy);
            p.Add("@SignatureHash", history.SignatureHash);
            p.Add("@FileHash", history.FileHash);
            p.Add("@SignNote", history.SignNote);
            p.Add("@SignMethod", history.SignMethod);
            p.Add("@SignedIpAddress", history.SignedIpAddress);
            p.Add("@SignedUserAgent", history.SignedUserAgent);
            p.Add("@SignedFileArchivePath", history.SignedFileArchivePath);
            p.Add("@SignatureImagePath", history.SignatureImagePath);
            p.Add("@SignatureIntentText", history.SignatureIntentText);
            p.Add("@SignedByUserNameSnapshot", history.SignedByUserNameSnapshot);
            p.Add("@SignedByFullNameSnapshot", history.SignedByFullNameSnapshot);
            p.Add("@TermsAcceptedAt", history.TermsAcceptedAt);
            p.Add("@CreatedBy", history.CreatedBy);
            return await ExecuteSQL("sp_ContractHistory_insert", p);
        }

        public async Task<bool> SignInternal(int id, int signedBy, DateTime signedAt, string signatureHash, string fileHash, string? signNote, string signMethod, string? signedByUserNameSnapshot, string? signedByFullNameSnapshot, string? signedIpAddress, string? signedUserAgent, string? signedFileArchivePath, string? signatureImagePath, string? signatureIntentText, DateTime? termsAcceptedAt)
        {
            var p = new DynamicParameters();
            p.Add("@Id", id);
            p.Add("@SignedBy", signedBy);
            p.Add("@SignedAt", signedAt);
            p.Add("@SignatureHash", signatureHash);
            p.Add("@FileHash", fileHash);
            p.Add("@SignNote", signNote);
            p.Add("@SignMethod", signMethod);
            p.Add("@SignedByUserNameSnapshot", signedByUserNameSnapshot);
            p.Add("@SignedByFullNameSnapshot", signedByFullNameSnapshot);
            p.Add("@SignedIpAddress", signedIpAddress);
            p.Add("@SignedUserAgent", signedUserAgent);
            p.Add("@SignedFileArchivePath", signedFileArchivePath);
            p.Add("@SignatureImagePath", signatureImagePath);
            p.Add("@SignatureIntentText", signatureIntentText);
            p.Add("@TermsAcceptedAt", termsAcceptedAt);
            var rows = await ExecuteSQLScalar<int>("sp_Contract_signInternal", p);
            return rows > 0;
        }

        public async Task<bool> SignInternalHr(int id, int signedBy, DateTime signedAt, string signatureHash, string fileHash, string? signNote, string signMethod, string? hrSignedByUserNameSnapshot, string? hrSignedByFullNameSnapshot, string? hrSignedIpAddress, string? hrSignedUserAgent)
        {
            var p = new DynamicParameters();
            p.Add("@Id", id);
            p.Add("@SignedBy", signedBy);
            p.Add("@SignedAt", signedAt);
            p.Add("@SignatureHash", signatureHash);
            p.Add("@FileHash", fileHash);
            p.Add("@SignNote", signNote);
            p.Add("@SignMethod", signMethod);
            p.Add("@HrSignedByUserNameSnapshot", hrSignedByUserNameSnapshot);
            p.Add("@HrSignedByFullNameSnapshot", hrSignedByFullNameSnapshot);
            p.Add("@HrSignedIpAddress", hrSignedIpAddress);
            p.Add("@HrSignedUserAgent", hrSignedUserAgent);
            var rows = await ExecuteSQLScalar<int>("sp_Contract_signInternalHr", p);
            return rows > 0;
        }

        public async Task<bool> SetOriginalFileHash(int id, string hash)
        {
            var p = new DynamicParameters();
            p.Add("@Id", id);
            p.Add("@OriginalFileHash", hash);
            var rows = await ExecuteSQLScalar<int>("sp_Contract_setOriginalFileHash", p);
            return rows > 0;
        }

        public async Task<List<ContractHistory>> GetHistory(int contractId)
        {
            var p = new DynamicParameters();
            p.Add("@ContractId", contractId);
            return await GetDataList<ContractHistory>("sp_ContractHistory_getByContract", p);
        }

        public async Task<List<Contract>> GetExpiring(int days)
        {
            var p = new DynamicParameters();
            p.Add("@Days", days);
            return await GetDataList<Contract>("sp_Contract_getExpiring", p);
        }

        public async Task<List<ContractStatusCount>> GetStatusCounts()
        {
            const string sql = @"
SELECT ISNULL(Status, '') AS Status, COUNT(1) AS Total
FROM Contracts
WHERE ISNULL(Deleted, 0) = 0
GROUP BY Status
";

            using (var con = GetConnection())
            {
                var result = await con.QueryAsync<ContractStatusCount>(sql);
                return result.ToList();
            }
        }
    }
}
