namespace VS.Human.Rep
{
    public class UnitOfWork : IUnitOfWork
    {
        public IEmployeeRep EmployeeRep { get; set; }
        public IGroupRep GroupRep { get; set; }

        public IlocationRep LocationRep { get; set; }

        public IPartnerRep PartnerRep { get; set; }
        public IMasterDataRep MasterDataRep { get; set; }

        public IJobItemRep JobItemRep { get; set; }

        public ICandidateRep CandidateRep { get; set; }
        public IOrderRep OrderRep { get; set; }
        public IDashboardRep DashboardRep { get; set; }
        public IParrentChildRep ParrentChildRep { get; set; }
        public IOnboardMemberRep OnboardMemberRep { get; set; }
        public IGlobalDataRep GlobalDataRep { get; set; }

        public IReportTalkTimeRepository ReportTalkTimeRepository { get; set; }
        public IReportRepository ReportRepository { get; set; }
        public IReportTalkTimeGroupByDay ReportTalkTimeGroupByDay { get; set; }

        public IScheduleInterviewRep ScheduleInterviewRep { get; set; }
        public IDocumentDataRep DocumentDataRep { get; set; }
        public IBHXHItemRep BHXHItemRep { get; set; }
        public ITaxtItemRep TaxtItemRep { get; set; }

        public IRelationItemRep RelationItemRep { get; set; }
        public IHDLDItemRep HDLDItemRep { get; set; }
        public IPermissionRep PermissionRep { get; set; }
        public ILeaveRep LeaveRep { get; set; }
        public IInternalNewsRep InternalNewsRep { get; set; }
        public INotificationRep NotificationRep { get; set; }
        public IAttendanceRep AttendanceRep { get; set; }
        public IEmailConfigRep EmailConfigRep { get; set; }
        public IMeetingRoomRep MeetingRoomRep { get; set; }
        public ILateEarlyRep LateEarlyRep { get; set; }
        public UnitOfWork(
            IHDLDItemRep hDLDItemRep,
            IRelationItemRep relationItemRep,
            IBHXHItemRep bHXHItemRep,
            ITaxtItemRep taxtItemRep,
            IEmployeeRep employeeRep,
            IGroupRep groupRep,
            IPartnerRep partnerRep,
            IMasterDataRep masterDataRep,
            IJobItemRep jobItemRep,
            ICandidateRep candidateRep,
            IOrderRep orderRep,
            IDashboardRep dashboardRep,
            IParrentChildRep parrentChildRep,
            IOnboardMemberRep onboardMemberRep,
            IGlobalDataRep globalDataRep,
            IlocationRep locationRep,
            IReportTalkTimeRepository reportTalkTimeRepository,
            IReportRepository _reportRepository,
            IReportTalkTimeGroupByDay reportTalkTimeGroupByDay,
            IScheduleInterviewRep scheduleInterviewRep,
            IDocumentDataRep documentDataRep,
            IPermissionRep permissionRep,
            ILeaveRep leaveRep,
            IInternalNewsRep internalNewsRep,
            INotificationRep notificationRep,
            IAttendanceRep attendanceRep,
            IEmailConfigRep emailConfigRep,
            IMeetingRoomRep meetingRoomRep,
            ILateEarlyRep lateEarlyRep
            )
        {
            this.BHXHItemRep = bHXHItemRep;
            this.TaxtItemRep = taxtItemRep;
            this.HDLDItemRep = hDLDItemRep;
            this.RelationItemRep = relationItemRep;

            this.EmployeeRep = employeeRep;
            this.GroupRep = groupRep;
            this.PartnerRep = partnerRep;
            this.MasterDataRep = masterDataRep;
            this.JobItemRep = jobItemRep;
            this.CandidateRep = candidateRep;
            this.OrderRep = orderRep;
            this.DashboardRep = dashboardRep;
            this.ParrentChildRep = parrentChildRep;
            this.OnboardMemberRep = onboardMemberRep;
            this.GlobalDataRep = globalDataRep;
            ReportTalkTimeRepository = reportTalkTimeRepository;
            LocationRep = locationRep;
            ReportRepository = _reportRepository;
            ReportTalkTimeGroupByDay = reportTalkTimeGroupByDay;
            ScheduleInterviewRep = scheduleInterviewRep;
            DocumentDataRep = documentDataRep;
            PermissionRep = permissionRep;
            LeaveRep = leaveRep;
            InternalNewsRep = internalNewsRep;
            NotificationRep = notificationRep;
            AttendanceRep = attendanceRep;
            EmailConfigRep = emailConfigRep;
            MeetingRoomRep = meetingRoomRep;
            LateEarlyRep = lateEarlyRep;
        }
    }
}
