
using Microsoft.Extensions.DependencyInjection;

namespace VS.Human.Rep
{
    public static class DependencyRegister
    {

        public static void ConfigRep(this IServiceCollection services)
        {
            // singleton
            services.AddSingleton<ILoginRep, LoginRep>();
            services.AddSingleton<IEmployeeRep, EmployeeRep>();
            services.AddSingleton<IGroupRep, GroupRep>();
            services.AddSingleton<IMasterDataRep, MasterDataRep>();
            services.AddSingleton<IJobItemRep, JobItemRep>();
            services.AddSingleton<ICandidateRep, CandidateRep>();
            services.AddSingleton<IOrderRep, OrderRep>();
            services.AddSingleton<IDashboardRep, DashboardRep>();
            services.AddSingleton<IParrentChildRep, ParrentChildRep>();
            services.AddSingleton<IOnboardMemberRep, OnboardMemberRep>();
            services.AddSingleton<IGlobalDataRep, GlobalDataRep>();
            services.AddSingleton<IUnitOfWork, UnitOfWork>();
            services.AddSingleton<IlocationRep, locationRep>();
            services.AddSingleton<IPartnerRep, PartnerRep>();
            services.AddSingleton<IReportTalkTimeGroupByDay, ReportTalkTimeGroupByDayRepository>();
            services.AddSingleton<IReportTalkTimeRepository, ReportTalkTimeRepository>();
            services.AddSingleton<IReportRepository, ReportRepository>();
            services.AddSingleton<IScheduleInterviewRep, ScheduleInterviewRep>();
            services.AddSingleton<IDocumentDataRep, DocumentDataRep>();
            services.AddSingleton<IHDLDItemRep, HDLDItemRep>();
            services.AddSingleton<ITaxtItemRep, TaxtItemRep>();
            services.AddSingleton<IRelationItemRep, RelationItemRep>();

            services.AddSingleton<IBHXHItemRep, BHXHItemRep>();
            services.AddSingleton<IPermissionRep, PermissionRep>();
            services.AddSingleton<ILeaveRep, LeaveRep>();
            services.AddSingleton<IInternalNewsRep, InternalNewsRep>();
            services.AddSingleton<IFormTemplateRep, FormTemplateRep>();
            services.AddSingleton<IMailGroupRep, MailGroupRep>();
            services.AddSingleton<INotificationRep, NotificationRep>();
            services.AddSingleton<IAttendanceRep, AttendanceRep>();
            services.AddSingleton<IEmailConfigRep, EmailConfigRep>();
            services.AddSingleton<IEmailSentLogRep, EmailSentLogRep>();
            services.AddSingleton<IWorkflowTimelineRep, WorkflowTimelineRep>();
            services.AddSingleton<ICallLogRep, CallLogRep>();
            services.AddSingleton<ISipRep, SipRep>();
            services.AddSingleton<IMeetingRoomRep, MeetingRoomRep>();
            services.AddSingleton<ILateEarlyRep, LateEarlyRep>();
            services.AddSingleton<ISupportRequestRep, SupportRequestRep>();
            services.AddSingleton<ILogHistoryRep, LogHistoryRep>();
            services.AddSingleton<IAuditLogRep, AuditLogRep>();
            services.AddSingleton<IUserThemeSettingRep, UserThemeSettingRep>();
            services.AddSingleton<ISystemBrandingRep, SystemBrandingRep>();
            services.AddSingleton<IContractRep, ContractRep>();
            services.AddSingleton<IReportAnalyticsRep, ReportAnalyticsRep>();

        }
    }
}
