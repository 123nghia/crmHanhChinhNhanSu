using Microsoft.Extensions.Configuration;
using VS.Human.Item;
using VS.Human.Rep.Model;

namespace VS.Human.Rep
{
    public class OrderRep : RepositoryBase<Order>, IOrderRep
    {

        public OrderRep(IConfiguration configuration)
            : base(configuration)
        {

            tableName = "order";
            sqlGetALl = "sp_order_getAll";
        }

        private async Task<bool> Update(Order item)
        {

            var parameter = new
            {
                item.Id,
                item.ProjectId,
                item.Source,

                item.CVLink,
                item.Status,
                item.IsActive,
                item.CandidateId,
                item.JobId,
                item.UpdatedBy,
                item.PartnerId,
                item.ShortDes,
                item.Noted
            };
            return await this.ExecuteSQL("sp_Order_update", parameter);
        }
        private async Task<bool> Add(Order item)
        {

            item.Status = 7;
            var parameter = new
            {
                item.ShortDes,
                item.CandidateId,
                item.JobId,
                item.Source,
                item.PartnerId,
                item.Noted,
                item.Status,
                item.ProjectId,
                item.CreatedBy,
                item.CVLink,
                item.DateGet,
                item.Enable,
                item.Assignee
            };
            return await this.ExecuteSQL("sp_order_insert", parameter);
        }


        public async Task<OrderInfoIndexModel> GetOrderInfo(int id)
        {

            var sql = "select dbo.getDisplayMasterDataById(Status) as StatusName, dbo.getMasterdataApplyFor(Status) as ApplyFor , * , dbo.getLinhvucId(JobId) as linhvucId  from [Order]" + " WHERE Id = @id";
            return await ExecuteSQL2<OrderInfoIndexModel>(sql, new
            {
                Id = id
            });
        }
        public async Task<bool> Assignee(dynamic item)
        {
            return await this.ExecuteSQL("sp_order_Assingee", item);
        }


        public async Task<bool> OrderResultRequest(dynamic item)
        {
            return await this.ExecuteSQL("sp_order_changeResult", item);

        }

        public async Task<bool> ChangeReturnOrder(dynamic item)
        {
            return await this.ExecuteSQL("sp_order_returnOrder", item);

        }

        public async Task<bool> ChangePushCaseCTV(dynamic item)
        {
            return await this.ExecuteSQL("sp_order_PushCaseCTV", item);

        }


        public async Task<bool> AddOrUpdateInfoOrder(dynamic item)
        {
            return await this.ExecuteSQL("UpdateInfoOrder", item);
        }

        public async Task<bool> ChangeStatusApply(dynamic item)
        {
            return await this.ExecuteSQL("ChangeStatusApply", item);
        }

        public async Task<bool> AddOrUpdate(Order item)
        {
            if (item.Id > 0)
            {
                var itemUpdate = await GetById(item.Id);
                if (itemUpdate != null)
                {
                    itemUpdate.CVLink = item.CVLink;
                    itemUpdate.IsActive = item.IsActive;
                    itemUpdate.CandidateId = item.CandidateId;
                    itemUpdate.JobId = item.JobId;


                    itemUpdate.ShortDes = item.ShortDes;
                    itemUpdate.Noted = item.Noted;
                    itemUpdate.ProjectId = item.ProjectId;
                    itemUpdate.UpdatedBy = item.UpdatedBy;
                    itemUpdate.Source = item.Source;
                    itemUpdate.PartnerId = item.PartnerId;
                    return await Update(itemUpdate);
                }
            }

            if (item.Source == 0)
            {
                item.DateGet = DateTime.Now;
                item.Assignee = item.CreatedBy;
                item.Enable = true;
            }
            return await Add(item);
        }


        public async Task<Order> GetByCode(string OrderCode)
        {
            var _baseTable = tableName;
            var sql = "SELECT * FROM [Order] WHERE Code = @orderCode";
            return await ExecuteSQL<Order>(sql, new
            {
                orderCode = OrderCode
            });
        }
        public async Task<bool> AddImpact(OrderImpactHIstory item)
        {
            var parameter = new
            {
                item.OrderCode,
                item.NewStatus,
                item.Noted,
                item.TxtTimer,
                item.DateFrom,
                item.TxtPlace,
                item.CreatedBy
            };
            return await this.ExecuteSQL("sp_OrderImpactHIstory_insert", parameter);

        }


        public async Task<bool> AddCandidateOrder(dynamic item)
        {
            return await this.ExecuteSQL("sp_AddCandidateAndOrderMarketting", item);
        }
        public async Task<bool> UpdateCandidateOrder(dynamic item)
        {
            return await this.ExecuteSQL("sp_UpdateCandidateAndOrderMarketting", item);
        }

        public async Task<bool> ImportSourMarketting(dynamic request)
        {

            return await this.ExecuteSQL("sp_OrderImportSourceMarketting", request);

        }

        public async Task<bool> DeleteImpact(int id)
        {
            return await DeleteBase(id, 1, "orderImpact");
        }



        public async Task<BaseList> GetAllImpact(OrderRequest request)
        {
            var result = await GetBaseAll<OrderImpactIndexModel>(request,
           new
           {
               request.OrderCode
           }, "sp_Order_getAllImpact");
            return result;
        }
        public async Task<BaseList> GetAllImpactReport(OrderRequest request)
        {
            var sqlText = "sp_Impact_getAll";
            if (request.RoleCode == "4")
            {
                sqlText = "sp_Impact_getAllMarketting";
            }
            var result = await GetBaseAll<OrderImpactReportIndexModel>(request,
            new
            {
                request.Limit,
                request.From,
                request.To,
                request.Status,
                request.UserId,

                request.Page
            }, sqlText);
            return result;
        }

        public async Task<BaseList> GetAllOrderStatus(OrderRequest request)
        {

            var sqltext = "sp_Order_getAllReportStatus";

            if (request.RoleCode == "4")
            {
                sqltext = "sp_Order_getAllReportStatusMarketting";
                return await GetBaseAll<OrderStatusIndexModel>(request, new
                {
                    request.UserId,
                    request.Status,
                    request.From,
                    request.To

                }, sqltext);
            }
            return await GetBaseAll<OrderStatusIndexModel>(request, new
            {
                request.UserId,
                request.Status,
                request.From,
                request.To,
                request.GroupId,
                request.MemberId
            }, sqltext);


        }

        public async Task<BaseList> GetALlHistory(OrderRequest request)
        {
            var sqlText = "";
            sqlGetALl = "sp_order_getAllHistory";
            sqlText = sqlGetALl;
            var result = await GetBaseAll<OrderIndexModel>(request,
              new
              {
                  request.Phone,
                  request.Email
              }, sqlText);
            return result;
        }

        public async Task<BaseList> GetAll(OrderRequest request)
        {
            var sqlText = "";
            if (request.Marketting == 1 || request.CTV == true)
            {
                sqlGetALl = "sp_Order_MarkettingGetAll";
            }
            else
            {
                sqlGetALl = "sp_order_getAll";
            }

            if (request.Tracking == true)
            {
                sqlGetALl = "sp_Order_getAllTracking";
            }
            else
            {
            }
            sqlText = sqlGetALl;
            var result = await GetBaseAll<OrderIndexModel>(request,
             new
             {
                 request.UserId,
                 request.Token,
                 request.From,
                 request.To,
                 request.Job,
                 request.Page,
                 request.Isapply,
                 request.Limit,
                 request.Status,
                 request.GroupId,
                 request.MemberId,
                 request.Marketting,
                 request.IsReturn,
                 request.IsClose,
                 request.IsPush,
                 request.DisplayAll,
                 request.CTV
             }, sqlText);
            return result;
        }

        public async Task<BaseList> GetAllOrderReport(OrderReportRequest request)
        {
            var sqlText = "";
            if (request.Marketting == 1)
            {
                sqlGetALl = "sp_Order_MarkettingReportGetAll";
            }
            else
            {
                sqlGetALl = "sp_orderReport_getAll";
            }

            if (request.Tracking == true)
            {
                sqlGetALl = "sp_OrderReport_getAllTracking";

            }
            else
            {

            }
            sqlText = sqlGetALl;
            var result = await GetBaseAll<OrderIndexModel>(request,
              new
              {
                  request.UserId,
                  request.Token,
                  request.From,
                  request.To,
                  request.Job,
                  request.Page,
                  request.Isapply,
                  request.Limit,
                  request.Status,
                  request.GroupId,
                  request.MemberId,
                  request.Marketting,
                  request.IsReturn,
                  request.IsClose

              }, sqlText);
            return result;
        }


    }
}
