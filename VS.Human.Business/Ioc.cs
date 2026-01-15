using
Microsoft.Extensions.DependencyInjection;
using VS.Human.Business.Imp;
using VS.Human.Rep;
namespace VS.Human.Business
{
    public static class DependencyRegister
    {

        public static void Config(this IServiceCollection services)
        {

            services.ConfigRep();
            services.AddSingleton<ILoginBussiness, LoginBusiness>();
            services.AddSingleton<IEmpBusiness, EmployeeBusiness>();
            services.AddSingleton<IGroupBusiness, GroupBusiness>();
            services.AddSingleton<IPartnerBusiness, PartnerBusiness>();
            services.AddSingleton<ImasterDataBussiness, MasterDataBusiness>();
            services.AddSingleton<ICandidateBusiness, CandidateBusiness>();
            services.AddSingleton<IJobItemBusiness, JobItemBusiness>();
            services.AddSingleton<IOrderBussiness, OrderBusiness>();
            services.AddSingleton<ICallBussiness, CallBusiness>();
            services.AddSingleton<IDashboardBusinness, DashboardBusinness>();
            services.AddSingleton<IParrentChildBussiness, ParrentChildBussiness>();
            services.AddSingleton<IReoportBussiness, ReportBussiness>();
            services.AddSingleton<IOnboardMemberBusiness, OnboardMemberBusiness>();
            services.AddSingleton<IExportFileBussiness, ExportFileBussiness>();
            services.AddSingleton<IGlobalDataBusiness, GlobalDataBusinness>();
            services.AddSingleton<ICalculateTimeBusiness, CalculateTimeBusiness>();
            services.AddSingleton<IHandleReportBussiness, HandleReportBussiness>();
            services.AddSingleton<IReportCDRBussiness, ReportCDRBussiness>();
            services.AddSingleton<IScheduleInterviewBussiness, ScheduleInterviewBusiness>();
            services.AddSingleton<IDocumentDataBussiness, DocumentDataBussiness>();
            services.AddSingleton<IEmployeeExtraBusiness, EmployeeExtraBusiness>();
            services.AddSingleton<IEmployeeImportBusiness, EmployeeImportBusiness>();
            services.AddSingleton<IPermissionBusiness, PermissionBusiness>();
            services.AddSingleton<ILeaveBusiness, LeaveBusiness>();
            services.AddSingleton<ILeaveBalanceBusiness, LeaveBalanceBusiness>();
            services.AddSingleton<IInternalNewsBusiness, InternalNewsBusiness>();


        }
    }
}
