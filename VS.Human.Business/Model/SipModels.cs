namespace VS.Human.Business.Model
{
    public class SipServerSaveRequest
    {
        public int? Id { get; set; }
        public string? Name { get; set; }
        public string? Host { get; set; }
        public int Port { get; set; }
        public string? Domain { get; set; }
        public string? Transport { get; set; }
        public string? OutboundProxy { get; set; }
        public string? Note { get; set; }
        public int IsActive { get; set; } = 1;
    }

    public class SipLineSaveRequest
    {
        public int? Id { get; set; }
        public int? SipServerId { get; set; }
        public string? LineCode { get; set; }
        public string? SipUserName { get; set; }
        public string? SipPassword { get; set; }
        public string? AuthUser { get; set; }
        public string? DisplayName { get; set; }
        public string? Note { get; set; }
        public int IsActive { get; set; } = 1;
    }

    public class SipAssignRequest
    {
        public int LineId { get; set; }
        public int EmployeeId { get; set; }
    }

    public class SipRevokeRequest
    {
        public int LineId { get; set; }
    }
}
