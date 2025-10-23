using Microsoft.AspNetCore.Http;
using VS.Human.Business.Model;
using VS.Human.Item;
using VS.Human.Rep;
using VS.Human.Rep.Model;

namespace VS.Human.Business.Imp
{
    public class OrderBusiness : BaseBusiness, IOrderBussiness
    {
        public OrderBusiness(IUnitOfWork unitOfWork,
            IHttpContextAccessor httpContextAccessor)
            : base(unitOfWork, httpContextAccessor)
        {

        }

        public async Task<bool> AddCandidateOrderMarketting(
             OrderCandidateMarkettingItemAdd item)
        {
            var userId = GetUserId();



            if (item.Id < 1)
            {
                item.Status = 46;
            }
            var requestAdd = new
            {
                item.Name,
                item.Email,
                item.PhoneNumber,
                item.Status,
                item.Dob,
                item.Source,
                item.NotedCan,
                item.CVLink,
                item.JobId,
                item.Document,
                item.NotedOrder,
                item.Regional,
                userId
            };
            if (item.Id > 0)
            {
                var requestUpdate = new
                {
                    item.Name,
                    item.Email,
                    item.PhoneNumber,
                    idCan = item.Id,
                    item.Status,
                    item.Dob,
                    item.Source,
                    item.NotedCan,
                    item.CVLink,
                    item.JobId,
                    item.Document,
                    item.NotedOrder,
                    item.Regional,
                    userId
                };
                return await _unitOfWork.OrderRep.UpdateCandidateOrder(requestUpdate);
            }
            return await _unitOfWork.OrderRep.AddCandidateOrder(requestAdd);

        }

        public async Task<bool> AddImpact(OrderImpactHIstoryAdd item)
        {
            item.CreatedBy = GetUserId();
            item.UpdatedBy = item.CreatedBy;
            var impactInfo = new OrderImpactHIstory();
            impactInfo.CreatedBy = GetUserId();
            impactInfo.UpdatedBy = item.CreatedBy;
            impactInfo.Noted = item.Noted;
            impactInfo.NewStatus = item.NewStatus;
            impactInfo.ObjectInfo = item.ObjectInfo;
            impactInfo.TxtPlace = item.TxtPlace;
            impactInfo.DateFrom = item.DateFrom;
            impactInfo.TxtTimer = item.TxtTimer;
            impactInfo.OrderCode = item.OrderCode;

            if (item.RadioOtherAdress != true && !string.IsNullOrEmpty(item.TxtPlace))
            {
                if (item.PartnerId > 0)
                {
                    var itemAddress = new ParrentChild()
                    {
                        CreateAt = DateTime.Now,
                        Id = -1,
                        Type = 1,
                        Text = item.TxtPlace,
                        RelId = item.PartnerId
                    };
                    await _unitOfWork.ParrentChildRep.AddOrUpdate(itemAddress);
                }
            }
            return await _unitOfWork.OrderRep.AddImpact(impactInfo);
        }
        public async Task<bool> Assignee(OrderAssingeeAdd item)
        {

            item.CreatedBy = GetUserId();
            return await _unitOfWork.OrderRep.Assignee(item);
        }
        public async Task<bool> OrderResultRequest(OrderResultRequest item)
        {

            item.CreatedBy = GetUserId();
            var itemHanlde = new
            {
                item.CreatedBy,
                item.Id,
                item.Result
            };
            return await _unitOfWork.OrderRep.OrderResultRequest(itemHanlde);
        }

        public async Task<bool> ChangeReturnOrder(OrderResultRequest item)
        {

            item.CreatedBy = GetUserId();
            var itemHanlde = new
            {
                item.CreatedBy,
                item.Id,
                item.Result
            };
            return await _unitOfWork.OrderRep.ChangeReturnOrder(itemHanlde);
        }


        public async Task<bool> ChangePushCaseCTV(OrderResultRequest item)
        {

            item.CreatedBy = GetUserId();
            var itemHanlde = new
            {
                item.CreatedBy,
                item.Id,
                item.Result
            };
            return await _unitOfWork.OrderRep.ChangePushCaseCTV(itemHanlde);
        }

        public async Task<bool> OrderResultRequest(OrderAssingeeAdd item)
        {

            item.CreatedBy = GetUserId();
            return await _unitOfWork.OrderRep.Assignee(item);
        }

        public async Task<bool> AddOrUpdate(OrderItemAdd item)
        {
            item.CreatedBy = GetUserId();
            item.UpdatedBy = item.CreatedBy;

            await _unitOfWork.OrderRep.AddOrUpdate(item);

            if (!string.IsNullOrEmpty(item.PhoneNumber))
            {
                var candidate = await _unitOfWork.CandidateRep.GetById(item.CandidateId.Value);
                if (candidate != null)
                {
                    candidate.Phone = item.PhoneNumber;
                    await _unitOfWork.CandidateRep.AddOrUpdate(candidate);
                }

            }
            return true;
        }

        public async Task<bool> AddOrUpdateInfoOrder(OrderInfoItemAdd item)
        {
            item.CreatedBy = GetUserId();
            item.Isapply = 0;
            //if (!item.Assignee.HasValue || (item.Assignee.Value < 1))
            //{
            //    item.Assignee = item.CreatedBy;
            //}
            await _unitOfWork.OrderRep.AddOrUpdateInfoOrder(item);
            return true;
        }

        public async Task<bool> ChangeStatusApply(dynamic item)
        {
            var itemChange = new
            {
                UpdateBy = GetUserId(),
                item.OrderId

            };
            await _unitOfWork.OrderRep.ChangeStatusApply(itemChange);
            return true;
        }

        public async Task<bool> Delete(int id)
        {
            return await _unitOfWork.OrderRep.Delete(id);
        }

        public async Task<bool> DeleteImpact(int id)
        {
            return await _unitOfWork.OrderRep.DeleteImpact(id);
        }

        public async Task<bool> DeleteOrder(int id)
        {
            var itemOrder = await _unitOfWork.OrderRep.GetById(id);
            if (itemOrder.Id < 1)
            {
                return false;
            }
            await _unitOfWork.OrderRep.DeleteReal(id);
            var candidate = await _unitOfWork.CandidateRep.GetById(itemOrder.CandidateId.HasValue ? itemOrder.CandidateId.Value : -1);
            if (candidate.Id < 1)
            {
                return false;
            }

            await _unitOfWork.CandidateRep.DeleteReal(candidate.Id);
            return true;
        }

        public async Task<BaseList> GetAll(OrderRequest request)
        {
            return await _unitOfWork.OrderRep.GetAll(request);
        }
        public async Task<BaseList> GetALlHistory(OrderRequest request)
        {
            return await _unitOfWork.OrderRep.GetALlHistory(request);
        }


        public async Task<BaseList> GetAllImpact(OrderRequest request)
        {
            return await _unitOfWork.OrderRep.GetAllImpact(request);
        }

        public async Task<Order> GetById(int id)
        {
            return await _unitOfWork.OrderRep.GetById(id);
        }
        public async Task<Order> GetByCode(string orderCode)
        {
            return await _unitOfWork.OrderRep.GetByCode(orderCode);
        }


        public async Task<OrderInfoIndexModel> GetInfo(int id)
        {
            return await _unitOfWork.OrderRep.GetOrderInfo(id);
        }
        public async Task<BaseList> GetAllOrderReport(OrderReportRequest request)
        {
            return await _unitOfWork.OrderRep.GetAllOrderReport(request);
        }

    }
}
