using Microsoft.Extensions.Configuration;
using VS.Human.Item;

namespace VS.Human.Rep
{
    public class DashboardRep : RepositoryBaseNoTModel, IDashboardRep
    {

        public DashboardRep(IConfiguration configuration)
            : base(configuration)
        {
            tableName = "Partner";
            sqlGetALl = "sp_Partner_getAll";
        }




        public async Task<BaseList> GetDashboardStatus(OrderRequest request)
        {
            var sqlGet = "sp_Order_getAllReportStatusDashboardNotMarketting";

            if (request.RoleCode == "4" || request.RoleCode == "6" || request.RoleCode == "7")
            {
                sqlGet = "sp_Order_getAllReportStatusDashboard";
                var requestparram1 = new
                {
                    request.From,
                    request.To,
                    GroupId = -1,
                    MemberId = -1,
                    request.Status,
                    request.Job,
                    request.UserId

                };
                return await this.GetBaseAll<StatusDashboardReport>(request, requestparram1, sqlGet);
            }
            var requestparram = new
            {
                request.From,
                request.To,
                request.Status,
                request.GroupId,
                request.MemberId,
                request.UserId,
                request.Job



            };
            return await this.GetBaseAll<StatusDashboardReport>(request,
             requestparram, sqlGet);

        }


        public async Task<BaseList> GetAllCV(OrderRequest request)
        {
            var sqlGet = "sp_GetAllCVNotMarketing";
            if (request.RoleCode == "4" || request.RoleCode == "6" || request.RoleCode == "7")
            {
                sqlGet = "sp_GetAllCVInfo";
            }

            var requestparram = new
            {
                request.From,
                request.To,
                request.GroupId,
                request.MemberId,
                request.Status,
                request.UserId,
                request.Job

            };
            return await GetBaseAll<CandidateIndexModel>(request,
            requestparram, sqlGet);
        }


        public async Task<BaseList> GetAllCountCVGroupByDate(OrderRequest request)
        {
            var sqlGet = "sp_AllCountCVGroupByDate";


            var requestparram = new
            {
                request.From,
                request.To,
                request.GroupId,
                request.MemberId,
                request.Status,
                request.UserId,
                request.Job

            };
            return await GetBaseAll<CountCVIndexModel>(request,
            requestparram, sqlGet);
        }
        public async Task<BaseList> GetAllCountCVApplyGroupByDate(OrderRequest request)
        {
            var sqlGet = "sp_GetAllCountCVApplyGroupByDate";


            var requestparram = new
            {
                request.From,
                request.To,
                request.GroupId,
                request.MemberId,
                request.Status,
                request.UserId,
                request.Job

            };
            return await GetBaseAll<CountCVIndexModel>(request,
            requestparram, sqlGet);
        }
        public async Task<BaseList> GetAllCountCVOnboardGroupByDate(OrderRequest request)
        {
            var sqlGet = "sp_GetAllCountCVOnboardGroupByDate";

            var requestparram = new
            {
                request.From,
                request.To,
                request.GroupId,
                request.MemberId,
                request.Status,
                request.UserId,
                request.Job

            };
            return await GetBaseAll<CountCVIndexModel>(request,
            requestparram, sqlGet);
        }

        public async Task<BaseList> GetALLOrderInfo(OrderRequest request)
        {
            var sqlGet = "sp_getALLOrderInfoNotMarketing";
            if (request.RoleCode == "4" || request.RoleCode == "6" || request.RoleCode == "7")
            {
                sqlGet = "sp_getALLOrderInfo";
            }

            var requestparram = new
            {
                request.From,
                request.To,
                request.GroupId,
                request.MemberId,
                request.Status,
                request.UserId,
                request.Job

            };
            return await GetBaseAll<OrderIndexModel>(request,
            requestparram, sqlGet);
        }

        public async Task<BaseList> GetAllOnboardCV(OrderRequest request)
        {
            var sqlGet = "sp_getAllOnboardCV";
            if (request.RoleCode == "4" || request.RoleCode == "6" || request.RoleCode == "7")
            {
                sqlGet = "sp_getAllOnboardCV";
            }

            var requestparram = new
            {
                request.From,
                request.To,
                request.GroupId,
                request.MemberId,
                request.Status,
                request.UserId,
                request.Job

            };
            return await GetBaseAll<MemberOnboardIndexModel>(request,
            requestparram, sqlGet);
        }



        public async Task<BaseList> GetAllOrderApply(OrderRequest request)
        {
            var sqlGet = "sp_GetAllOrderApply";


            var requestparram = new
            {
                request.From,
                request.To,
                request.GroupId,
                request.MemberId,
                request.Status,
                request.UserId,
                request.Job

            };
            return await GetBaseAll<OrderIndexModel>(request,
            requestparram, sqlGet);
        }

        public async Task<BaseList> GetAllOrderDraft(OrderRequest request)
        {
            var sqlGet = "sp_GetAllOrderDraft";
            var requestparram = new
            {
                request.From,
                request.To,
                request.GroupId,
                request.MemberId,
                request.Status,
                request.UserId,
                request.Job

            };
            return await GetBaseAll<OrderIndexModel>(request,
            requestparram, sqlGet);
        }
        public async Task<BaseList> GetALlOrderRemoveDup(OrderRequest request)
        {
            var sqlGet = "sp_GetALlOrderRemoveDup";
            var requestparram = new
            {
                request.From,
                request.To,
                request.GroupId,
                request.MemberId,
                request.Status,
                request.UserId,
                request.Job

            };
            return await GetBaseAll<OrderIndexModel>(request,
            requestparram, sqlGet);
        }




        public async Task<BaseList> GetALLOrder(OrderRequest request)
        {
            var sqlGet = "sp_getALLOrderMarketting";

            if (request.RoleCode == "4" || request.RoleCode == "6" || request.RoleCode == "7")
            {
                sqlGet = "sp_getALLOrderMarketting";
                var requestparram1 = new
                {
                    request.From,
                    request.To,
                    request.Status,
                    request.UserId
                };
                return await this.GetBaseAll<OrderMarkettingDashboard>(request, requestparram1, sqlGet);
            }
            var requestparram = new
            {
                request.From,
                request.To,
                request.Status,
                request.GroupId,
                request.MemberId,
                request.UserId,
                request.Limit
            };
            return await this.GetBaseAll<ParamDashboard>(request,
            requestparram, sqlGet);
        }
        public async Task<BaseList> GetInfoCount(OrderRequest request)
        {
            var sqlGet = "sp_getInfoCountOrder";

            if (request.RoleCode == "4" || request.RoleCode == "6" || request.RoleCode == "7")
            {
                sqlGet = "sp_getInfoCountOrderMarketting";
                var requestparram1 = new
                {
                    request.From,
                    request.To,
                    request.Status,
                    request.UserId

                };
                return await this.GetBaseAll<OrderMarkettingDashboard>(request, requestparram1, sqlGet);
            }
            var requestparram = new
            {
                request.From,
                request.To,
                request.Status,
                request.GroupId,
                request.MemberId,
                request.UserId,
                request.Limit
            };
            return await this.GetBaseAll<ParamDashboard>(request,
             requestparram, sqlGet);

        }
        public async Task<BaseList> GetInfoDashboard(OrderRequest request)
        {
            var sqlGet = "sp_getParramDashboard";

            if (request.RoleCode == "4" || request.RoleCode == "6" || request.RoleCode == "7")
            {
                sqlGet = "sp_getParramDashboardMarketting";
                var requestparram1 = new
                {
                    request.From,
                    request.To,
                    request.Status,
                    request.UserId

                };
                return await this.GetBaseAll<OrderMarkettingDashboard>(request, requestparram1, sqlGet);
            }
            var requestparram = new
            {
                request.From,
                request.To,
                request.Status,
                request.GroupId,
                request.MemberId,
                request.UserId,
                request.Limit
            };
            return await this.GetBaseAll<ParamDashboard>(request,
             requestparram, sqlGet);

        }

        public async Task<BaseList> GetTopImpactOrder(OrderRequest request)
        {

            var sqlText = "sp_getAllTopImpact";


            if (request.RoleCode == "4" || request.RoleCode == "6" || request.RoleCode == "7")
            {
                var requestObject = new
                {
                    request.From,
                    request.To,
                    request.GroupId,
                    request.MemberId,

                    request.UserId,
                    request.Limit


                };
                sqlText = "sp_getAllTopImpactMarketting";
                return await this.GetBaseAll<OrderImpactDashboardIndexModel>(request,
                             requestObject, sqlText);
            }

            else
            {
                var requestObject = new
                {
                    request.From,
                    request.To,
                    Status = -1,
                    request.GroupId,
                    request.MemberId,
                    request.UserId,
                    request.Limit


                };
                return await this.GetBaseAll<OrderImpactDashboardIndexModel>(request,
                requestObject, sqlText);
            }



        }

        public async Task<BaseList> GetTopOrder(OrderRequest request)
        {
            var sqlGet = "sp_Order_getAllLastest";
            if (request.RoleCode == "4" || request.RoleCode == "6" || request.RoleCode == "7")
            {
                sqlGet = "sp_Order_getAllLastestMarketting";
            }
            var result = await GetBaseAll<OrderLastestIndexModel>(request,
            new
            {
                request.From,
                request.To,
                request.Status,
                request.GroupId,
                request.MemberId,
                request.UserId,
                request.Job,
                request.Limit
            }, sqlGet);
            return result;
        }

        public async Task<BaseList> StatisticsReport(OrderRequest request)
        {
            var result = await this.GetBaseAll<OrderStatusDashboard>(request,
            new
            {
                request.From,
                request.To,
                request.GroupId,
                request.MemberId,
                request.UserId,

            }, "sp_Order_groupByStatus");
            return result;

        }
    }
}
