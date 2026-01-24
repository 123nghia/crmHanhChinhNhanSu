using System;

namespace VS.Human.Rep.Model
{
    public class AuditLog : BaseModel
    {
        public int? UserId { get; set; }
        public string? UserName { get; set; }
        public string? FullName { get; set; }
        public string? RoleCode { get; set; }
        public string Action { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string? QueryString { get; set; }
        public string? Payload { get; set; }
        public int? StatusCode { get; set; }
        public int? DurationMs { get; set; }
        public string? ClientIp { get; set; }
        public string? UserAgent { get; set; }
    }
}
