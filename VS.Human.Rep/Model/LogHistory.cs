using System;

namespace VS.Human.Rep.Model
{
    public class LogHistory : BaseModel
    {
        public int UserId { get; set; }
        public string? UserName { get; set; }
        public string? FullName { get; set; }
        public string? RoleCode { get; set; }
        public DateTime LoginAt { get; set; }
        public DateTime? LogoutAt { get; set; }
        public string? Source { get; set; }
        public string? ClientIp { get; set; }
        public string? UserAgent { get; set; }
    }
}
