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

            var history = new ContractHistory
            {
                ContractId = newId,
                EmployeeId = item.EmployeeId,
                Action = "CREATE",
                ContractTypeCode = item.ContractTypeCode,
                StartDate = item.StartDate,
                EndDate = item.EndDate,
                Status = item.Status,
                FileUrl = item.FileUrl,
                Note = item.Note,
                CreatedBy = userId
            };

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

            var history = new ContractHistory
            {
                ContractId = current.Id,
                EmployeeId = current.EmployeeId,
                Action = "UPDATE",
                ContractTypeCode = current.ContractTypeCode,
                StartDate = current.StartDate,
                EndDate = current.EndDate,
                Status = current.Status,
                FileUrl = current.FileUrl,
                Note = current.Note,
                CreatedBy = userId
            };

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

            var history = new ContractHistory
            {
                ContractId = current.Id,
                EmployeeId = current.EmployeeId,
                Action = "DELETE",
                ContractTypeCode = current.ContractTypeCode,
                StartDate = current.StartDate,
                EndDate = current.EndDate,
                Status = current.Status,
                FileUrl = current.FileUrl,
                Note = current.Note,
                CreatedBy = userId
            };

            await _unitOfWork.ContractRep.AddHistory(history);
            return true;
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
    }
}
