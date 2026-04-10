using System.Globalization;
using VS.Human.Item;

namespace VS.Human.Rep.Model
{
    public class SipServer : BaseModel
    {
        public string? Name { get; set; }
        public string? Host { get; set; }
        public int Port { get; set; }
        public string? Domain { get; set; }
        public string? Transport { get; set; }
        public string? OutboundProxy { get; set; }
        public string? Note { get; set; }
    }

    public class SipLine : BaseModel
    {
        public int? SipServerId { get; set; }
        public string? LineCode { get; set; }
        public string? SipUserName { get; set; }
        public string? SipPassword { get; set; }
        public string? AuthUser { get; set; }
        public string? DisplayName { get; set; }
        public int? EmployeeId { get; set; }
        public DateTime? AssignedAt { get; set; }
        public int? AssignedBy { get; set; }
        public DateTime? RevokedAt { get; set; }
        public int? RevokedBy { get; set; }
        public string? Note { get; set; }
    }

    public class SipLineViewModel : BaseIndexModel
    {
        public int? SipServerId { get; set; }
        public string? ServerName { get; set; }
        public string? ServerHost { get; set; }
        public int? ServerPort { get; set; }
        public string? ServerDomain { get; set; }
        public string? ServerTransport { get; set; }
        public string? ServerOutboundProxy { get; set; }
        public string? LineCode { get; set; }
        public string? SipUserName { get; set; }
        public string? SipPassword { get; set; }
        public string? AuthUser { get; set; }
        public string? DisplayName { get; set; }
        public int? EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public string? EmployeeUserName { get; set; }
        public DateTime? AssignedAt { get; set; }
        public string? Note { get; set; }
        public int IsActive { get; set; }

        public bool IsAssigned => EmployeeId.HasValue && EmployeeId.Value > 0;

        public string AssignedAtDisplay
        {
            get
            {
                if (!AssignedAt.HasValue)
                {
                    return string.Empty;
                }

                return AssignedAt.Value.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
            }
        }
    }

    public class SipEmployeeOption
    {
        public int Id { get; set; }
        public string? UserName { get; set; }
        public string? FullName { get; set; }
        public string? DepartmentCode { get; set; }
        public string? DepartmentText { get; set; }
        public string? LineCode { get; set; }
    }

    public class EmployeeSipAccountView
    {
        public bool HasAssignedLine { get; set; }
        public int? LineId { get; set; }
        public string? LineCode { get; set; }
        public string? SipUserName { get; set; }
        public string? SipPassword { get; set; }
        public string? AuthUser { get; set; }
        public string? DisplayName { get; set; }
        public DateTime? AssignedAt { get; set; }
        public string? LineNote { get; set; }

        public int? SipServerId { get; set; }
        public string? ServerName { get; set; }
        public string? ServerHost { get; set; }
        public int? ServerPort { get; set; }
        public string? ServerDomain { get; set; }
        public string? ServerTransport { get; set; }
        public string? ServerOutboundProxy { get; set; }
        public string? ServerNote { get; set; }

        public string AssignedAtDisplay
        {
            get
            {
                if (!AssignedAt.HasValue)
                {
                    return string.Empty;
                }

                return AssignedAt.Value.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
            }
        }
    }
}
