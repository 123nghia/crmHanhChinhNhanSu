using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VS.Human.Business.Imp;
using VS.Human.Item;
using VS.Human.Rep;
using VS.Human.Rep.Model;

namespace VS.Human.Business
{
    public class LateEarlyBusiness : BaseBusiness, ILateEarlyBusiness
    {
        public LateEarlyBusiness(IUnitOfWork unitOfWork, IHttpContextAccessor contextAccessor)
            : base(unitOfWork, contextAccessor)
        {
        }

        public async Task<BaseList> GetList(int? employeeId, int? status, DateTime? fromDate, DateTime? toDate, int page, int limit)
        {
            return await _unitOfWork.LateEarlyRep.GetAll(employeeId, status, fromDate, toDate, page, limit);
        }

        public async Task<LateEarlyIndexModel?> GetById(int id)
        {
            return await _unitOfWork.LateEarlyRep.GetById(id);
        }

        public async Task<int> CreateOrUpdate(LateEarlyAddUpdate model, int userId)
        {
            if (model.StartTime >= model.EndTime)
            {
                return -1;
            }

            if (model.RequestDate.Date != model.StartTime.Date || model.RequestDate.Date != model.EndTime.Date)
            {
                return -2;
            }

            if (model.EmployeeId <= 0)
            {
                return -3;
            }

            var status = 0;
            var employee = await _unitOfWork.EmployeeRep.GetById(model.EmployeeId);
            if (employee == null || employee.Id <= 0)
            {
                return -4;
            }

            if (!employee.ManagerId.HasValue || employee.ManagerId.Value <= 0)
            {
                status = 1; // Skip TL if not assigned
            }

            var savedId = await _unitOfWork.LateEarlyRep.Save(model, userId, status);
            return savedId;
        }

        public async Task<bool> ApproveWorkflow(int id, string action, int approverId, string roleCode, string? comment)
        {
            return await _unitOfWork.LateEarlyRep.ApproveWorkflow(id, action, approverId, roleCode, comment);
        }

        public async Task<List<LateEarlyHistory>> GetHistory(int requestId)
        {
            return await _unitOfWork.LateEarlyRep.GetHistory(requestId);
        }

        public async Task<bool> Delete(int id, int userId)
        {
            return await _unitOfWork.LateEarlyRep.Delete(id, userId);
        }
    }
}
