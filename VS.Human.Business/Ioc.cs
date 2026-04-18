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
            services.AddSingleton<ILeaveAttendanceImpactResolver, LeaveAttendanceImpactResolver>();
            services.AddSingleton<ILeaveAttendanceSyncService, LeaveAttendanceSyncService>();
            services.AddSingleton<IWorkflowTimelineBusiness, WorkflowTimelineBusiness>();
            services.AddSingleton<ILeaveBusiness, LeaveBusiness>();
            services.AddSingleton<ILeaveBalanceBusiness, LeaveBalanceBusiness>();
            services.AddSingleton<IInternalNewsBusiness, InternalNewsBusiness>();
            services.AddSingleton<IFormTemplateBusiness, FormTemplateBusiness>();
            services.AddSingleton<IMailGroupBusiness, MailGroupBusiness>();
            services.AddSingleton<INotificationBusiness, NotificationBusiness>();
            services.AddSingleton<IAttendanceBusiness, AttendanceBusiness>();
            services.AddSingleton<IEmailConfigBusiness, EmailConfigBusiness>();
            services.AddSingleton<IEmailSentLogBusiness, EmailSentLogBusiness>();
            services.AddSingleton<ISipBusiness, SipBusiness>();
            services.AddSingleton<IEmailService, EmailService>();
            services.AddSingleton<IMeetingRoomBusiness, MeetingRoomBusiness>();
            services.AddSingleton<ILateEarlyBusiness, LateEarlyBusiness>();
            services.AddSingleton<ISupportRequestBusiness, SupportRequestBusiness>();
            services.AddSingleton<ILogHistoryBusiness, LogHistoryBusiness>();
            services.AddSingleton<IAuditLogBusiness, AuditLogBusiness>();
            services.AddSingleton<IThemeSettingBusiness, ThemeSettingBusiness>();
            services.AddSingleton<IBrandingBusiness, BrandingBusiness>();
            services.AddSingleton<IContractBusiness, ContractBusiness>();
            services.AddSingleton<IReportAnalyticsBusiness, ReportAnalyticsBusiness>();


        }
    }
}
