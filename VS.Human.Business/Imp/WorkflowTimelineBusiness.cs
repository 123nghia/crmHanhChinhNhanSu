using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using VS.Human.Rep;
using VS.Human.Rep.Model;

namespace VS.Human.Business.Imp
{
    public class WorkflowTimelineBusiness : BaseBusiness, IWorkflowTimelineBusiness
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public WorkflowTimelineBusiness(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor)
            : base(unitOfWork, httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<int> TrackAsync(WorkflowTimelineEvent timelineEvent)
        {
            if (timelineEvent == null)
            {
                return 0;
            }

            timelineEvent.EntityType = (timelineEvent.EntityType ?? string.Empty).Trim().ToUpperInvariant();
            timelineEvent.EventCode = (timelineEvent.EventCode ?? string.Empty).Trim().ToUpperInvariant();
            timelineEvent.OccurredAt = timelineEvent.OccurredAt == default ? DateTime.Now : timelineEvent.OccurredAt;

            var currentUser = ResolveCurrentUserSnapshot();
            timelineEvent.CreatedBy = timelineEvent.CreatedBy > 0 ? timelineEvent.CreatedBy : currentUser.UserId;
            timelineEvent.UpdatedBy = timelineEvent.UpdatedBy > 0 ? timelineEvent.UpdatedBy : currentUser.UserId;
            timelineEvent.TriggeredBy ??= currentUser.UserId > 0 ? currentUser.UserId : null;
            timelineEvent.TriggeredByName ??= currentUser.FullName ?? currentUser.UserName;

            return await _unitOfWork.WorkflowTimelineRep.AddAsync(timelineEvent);
        }

        public async Task<int> TrackLeaveAsync(
            LeaveIndexModel leave,
            string eventCode,
            string eventTitle,
            int? triggeredBy,
            string? triggeredByName = null,
            string? metadataJson = null,
            string? errorMessage = null,
            bool emailSent = false,
            bool emailFailed = false,
            int? emailLogId = null,
            bool notificationCreated = false,
            int? notificationId = null,
            string? attendanceSyncStatus = null)
        {
            if (leave == null || leave.Id <= 0)
            {
                return 0;
            }

            var owner = await ResolveLeaveOwnerAsync(leave);
            var slaHours = ResolveLeaveSlaHours(leave.Status);

            var timelineEvent = new WorkflowTimelineEvent
            {
                EntityType = WorkflowEntityTypes.Leave,
                EntityId = leave.Id,
                EventCode = eventCode,
                EventTitle = eventTitle,
                CurrentStatus = leave.Status,
                CurrentStatusText = ResolveLeaveStatusText(leave.Status),
                TriggeredBy = triggeredBy,
                TriggeredByName = triggeredByName,
                AssignedTo = owner.OwnerId,
                AssignedToName = owner.OwnerName,
                OccurredAt = DateTime.Now,
                DueAt = slaHours.HasValue ? DateTime.Now.AddHours(slaHours.Value) : null,
                SlaHours = slaHours,
                EmailSent = emailSent,
                EmailFailed = emailFailed,
                EmailLogId = emailLogId,
                NotificationCreated = notificationCreated,
                NotificationId = notificationId,
                AttendanceSyncStatus = attendanceSyncStatus,
                ErrorMessage = errorMessage,
                MetadataJson = metadataJson,
                CreatedBy = triggeredBy.GetValueOrDefault(),
                UpdatedBy = triggeredBy.GetValueOrDefault()
            };

            return await TrackAsync(timelineEvent);
        }

        public async Task<List<WorkflowTimelineEvent>> GetByEntityAsync(string entityType, int entityId)
        {
            return await _unitOfWork.WorkflowTimelineRep.GetByEntityAsync(entityType, entityId);
        }

        public async Task<WorkflowActionCenterSummary> GetActionCenterAsync(int userId, string? roleCode)
        {
            return await _unitOfWork.WorkflowTimelineRep.GetActionCenterAsync(userId, roleCode);
        }

        private async Task<(int? OwnerId, string? OwnerName)> ResolveLeaveOwnerAsync(LeaveIndexModel leave)
        {
            return leave.Status switch
            {
                0 => await ResolveManagerOwnerAsync(leave.EmployeeId),
                1 => (null, "Phòng HCNS"),
                2 => (null, "Ban Giám đốc"),
                _ => (null, null)
            };
        }

        private async Task<(int? OwnerId, string? OwnerName)> ResolveManagerOwnerAsync(int employeeId)
        {
            if (employeeId <= 0)
            {
                return (null, null);
            }

            var employee = await _unitOfWork.EmployeeRep.GetById(employeeId);
            if (employee == null || employee.Id <= 0)
            {
                return (null, null);
            }

            if (employee.GroupId.HasValue && employee.GroupId.Value > 0)
            {
                var group = await _unitOfWork.GroupRep.GetById(employee.GroupId.Value);
                if (group != null
                    && int.TryParse(group.ManagerId, out var groupManagerId)
                    && groupManagerId > 0
                    && groupManagerId != employeeId)
                {
                    var groupManager = await _unitOfWork.EmployeeRep.GetById(groupManagerId);
                    if (groupManager != null && groupManager.Id > 0)
                    {
                        return (groupManager.Id, groupManager.FullName ?? groupManager.UserName);
                    }
                }
            }

            if (employee.ManagerId.HasValue && employee.ManagerId.Value > 0 && employee.ManagerId.Value != employeeId)
            {
                var manager = await _unitOfWork.EmployeeRep.GetById(employee.ManagerId.Value);
                if (manager != null && manager.Id > 0)
                {
                    return (manager.Id, manager.FullName ?? manager.UserName);
                }
            }

            return (null, "Quản lý trực tiếp");
        }

        private static int? ResolveLeaveSlaHours(int status)
        {
            return status switch
            {
                0 => 24,
                1 => 12,
                2 => 24,
                _ => null
            };
        }

        private static string ResolveLeaveStatusText(int status)
        {
            return status switch
            {
                0 => "Chờ quản lý trực tiếp",
                1 => "Quản lý đã duyệt / Chờ HCNS",
                2 => "HCNS đã duyệt / Chờ BGD",
                3 => "Đã duyệt",
                4 => "Đã duyệt (duyệt thay)",
                5 => "Từ chối",
                6 => "Đã hủy",
                _ => status.ToString()
            };
        }

        private (int UserId, string? UserName, string? FullName) ResolveCurrentUserSnapshot()
        {
            var identity = _httpContextAccessor.HttpContext?.User?.Identity as ClaimsIdentity;
            var userIdText = identity?.Claims.FirstOrDefault(x => x.Type == "userId")?.Value;
            var userName = identity?.Claims.FirstOrDefault(x => x.Type == "UserName")?.Value;
            var fullName = identity?.Claims.FirstOrDefault(x => x.Type == "FullName")?.Value;
            var userId = int.TryParse(userIdText, out var parsedUserId) ? parsedUserId : 0;
            return (userId, userName, fullName);
        }
    }
}
