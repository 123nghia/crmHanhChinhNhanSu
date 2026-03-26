using Microsoft.AspNetCore.Http;
using VS.Human.Rep;
using VS.Human.Rep.Model;
using VS.Human.Item;

namespace VS.Human.Business.Imp
{
    public class ContractBusiness : BaseBusiness, IContractBusiness
    {
        public ContractBusiness(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor)
            : base(unitOfWork, httpContextAccessor)
        {
        }

        public async Task<BaseList> GetAll(ContractRequest request)
        {
            return await _unitOfWork.ContractRep.GetAll(request);
        }

        public async Task<Contract?> GetById(int id)
        {
            return await _unitOfWork.ContractRep.GetById(id);
        }

        public async Task<bool> Add(Contract item, int userId)
        {
            item.CreatedBy = userId;
            item.UpdatedBy = userId;
            if (string.IsNullOrWhiteSpace(item.Status))
            {
                item.Status = "Active";
            }

            var newId = await _unitOfWork.ContractRep.Add(item);
            if (newId <= 0)
            {
                return false;
            }

            item.Id = newId;
            var history = BuildHistory(item, "CREATE", userId);

            await _unitOfWork.ContractRep.AddHistory(history);
            return true;
        }

        public async Task<bool> Update(Contract item, int userId)
        {
            var current = await _unitOfWork.ContractRep.GetById(item.Id);
            if (current == null || current.Id <= 0)
            {
                return false;
            }

            current.ContractTypeCode = item.ContractTypeCode;
            current.StartDate = item.StartDate;
            current.EndDate = item.EndDate;
            current.Status = item.Status;
            current.Note = item.Note;
            current.UpdatedBy = userId;

            var updated = await _unitOfWork.ContractRep.Update(current);
            if (!updated)
            {
                return false;
            }

            var history = BuildHistory(current, "UPDATE", userId);

            await _unitOfWork.ContractRep.AddHistory(history);
            return true;
        }

        public async Task<bool> Delete(int id, int userId)
        {
            var current = await _unitOfWork.ContractRep.GetById(id);
            if (current == null || current.Id <= 0)
            {
                return false;
            }

            var deleted = await _unitOfWork.ContractRep.Delete(id, userId);
            if (!deleted)
            {
                return false;
            }

            var history = BuildHistory(current, "DELETE", userId);

            await _unitOfWork.ContractRep.AddHistory(history);
            return true;
        }

        public async Task<bool> SignInternal(int contractId, int signedBy, DateTime signedAt, string signatureHash, string fileHash, string? signNote, string signMethod, string? signedByUserNameSnapshot, string? signedByFullNameSnapshot, string? signedIpAddress, string? signedUserAgent, string? signedFileArchivePath, string? signatureImagePath, string? signatureIntentText, DateTime? termsAcceptedAt)
        {
            var current = await _unitOfWork.ContractRep.GetById(contractId);
            if (current == null || current.Id <= 0)
            {
                return false;
            }

            if (current.IsSignedInternal)
            {
                return false;
            }

            if (!current.IsHrSigned)
            {
                return false;
            }

            var signed = await _unitOfWork.ContractRep.SignInternal(contractId, signedBy, signedAt, signatureHash, fileHash, signNote, signMethod, signedByUserNameSnapshot, signedByFullNameSnapshot, signedIpAddress, signedUserAgent, signedFileArchivePath, signatureImagePath, signatureIntentText, termsAcceptedAt);
            if (!signed)
            {
                return false;
            }

            current.IsSignedInternal = true;
            current.SignedAt = signedAt;
            current.SignedBy = signedBy;
            current.SignatureHash = signatureHash;
            current.FileHash = fileHash;
            current.SignNote = signNote;
            current.SignMethod = signMethod;
            current.SignedByUserNameSnapshot = signedByUserNameSnapshot;
            current.SignedByFullNameSnapshot = signedByFullNameSnapshot;
            current.SignedIpAddress = signedIpAddress;
            current.SignedUserAgent = signedUserAgent;
            current.SignedFileArchivePath = signedFileArchivePath;
            current.SignatureImagePath = signatureImagePath;
            current.SignatureIntentText = signatureIntentText;
            current.TermsAcceptedAt = termsAcceptedAt;
            current.UpdatedBy = signedBy;

            var history = BuildHistory(current, "SIGN_INTERNAL", signedBy);
            await _unitOfWork.ContractRep.AddHistory(history);
            return true;
        }

        public async Task<bool> SignInternalHr(int contractId, int signedBy, DateTime signedAt, string signatureHash, string fileHash, string? signNote, string signMethod, string? hrSignedByUserNameSnapshot, string? hrSignedByFullNameSnapshot, string? hrSignedIpAddress, string? hrSignedUserAgent)
        {
            var current = await _unitOfWork.ContractRep.GetById(contractId);
            if (current == null || current.Id <= 0)
            {
                return false;
            }

            if (current.IsHrSigned || current.IsSignedInternal)
            {
                return false;
            }

            var signed = await _unitOfWork.ContractRep.SignInternalHr(contractId, signedBy, signedAt, signatureHash, fileHash, signNote, signMethod, hrSignedByUserNameSnapshot, hrSignedByFullNameSnapshot, hrSignedIpAddress, hrSignedUserAgent);
            if (!signed)
            {
                return false;
            }

            current.IsHrSigned = true;
            current.HrSignedAt = signedAt;
            current.HrSignedBy = signedBy;
            current.HrSignatureHash = signatureHash;
            current.HrFileHash = fileHash;
            current.HrSignNote = signNote;
            current.HrSignMethod = signMethod;
            current.HrSignedByUserNameSnapshot = hrSignedByUserNameSnapshot;
            current.HrSignedByFullNameSnapshot = hrSignedByFullNameSnapshot;
            current.HrSignedIpAddress = hrSignedIpAddress;
            current.HrSignedUserAgent = hrSignedUserAgent;
            current.UpdatedBy = signedBy;

            var history = BuildHistory(current, "SIGN_INTERNAL_HR", signedBy);
            await _unitOfWork.ContractRep.AddHistory(history);
            return true;
        }

        public async Task<bool> SetOriginalFileHash(int contractId, string hash)
        {
            return await _unitOfWork.ContractRep.SetOriginalFileHash(contractId, hash);
        }

        public async Task<List<ContractHistory>> GetHistory(int contractId)
        {
            return await _unitOfWork.ContractRep.GetHistory(contractId);
        }

        public async Task<List<Contract>> GetExpiring(int days)
        {
            return await _unitOfWork.ContractRep.GetExpiring(days);
        }

        public async Task<List<ContractStatusCount>> GetStatusCounts()
        {
            return await _unitOfWork.ContractRep.GetStatusCounts();
        }

        private static ContractHistory BuildHistory(Contract contract, string action, int userId)
        {
            return new ContractHistory
            {
                ContractId = contract.Id,
                EmployeeId = contract.EmployeeId,
                Action = action,
                ContractTypeCode = contract.ContractTypeCode,
                StartDate = contract.StartDate,
                EndDate = contract.EndDate,
                Status = contract.Status,
                FileUrl = contract.FileUrl,
                Note = contract.Note,
                IsHrSigned = contract.IsHrSigned,
                HrSignedAt = contract.HrSignedAt,
                HrSignedBy = contract.HrSignedBy,
                HrSignatureHash = contract.HrSignatureHash,
                HrFileHash = contract.HrFileHash,
                HrSignNote = contract.HrSignNote,
                HrSignMethod = contract.HrSignMethod,
                HrSignedIpAddress = contract.HrSignedIpAddress,
                HrSignedUserAgent = contract.HrSignedUserAgent,
                HrSignedByUserNameSnapshot = contract.HrSignedByUserNameSnapshot,
                HrSignedByFullNameSnapshot = contract.HrSignedByFullNameSnapshot,
                IsSignedInternal = contract.IsSignedInternal,
                SignedAt = contract.SignedAt,
                SignedBy = contract.SignedBy,
                SignatureHash = contract.SignatureHash,
                FileHash = contract.FileHash,
                SignNote = contract.SignNote,
                SignMethod = contract.SignMethod,
                SignedIpAddress = contract.SignedIpAddress,
                SignedUserAgent = contract.SignedUserAgent,
                SignedFileArchivePath = contract.SignedFileArchivePath,
                SignatureImagePath = contract.SignatureImagePath,
                SignatureIntentText = contract.SignatureIntentText,
                SignedByUserNameSnapshot = contract.SignedByUserNameSnapshot,
                SignedByFullNameSnapshot = contract.SignedByFullNameSnapshot,
                TermsAcceptedAt = contract.TermsAcceptedAt,
                CreatedBy = userId
            };
        }
    }
}
