namespace VS.Human.Business.Model
{
    public class AttendanceMachineOptions
    {
        public string? DbPath { get; set; }
        public string? Password { get; set; }
        public string? Provider { get; set; }

        public string? LogTable { get; set; }
        public string? LogUserIdColumn { get; set; }
        public string? LogTimeColumn { get; set; }

        public string? UserTable { get; set; }
        public string? UserIdColumn { get; set; }
        public string? UserFingerprintColumn { get; set; }
        public string? UserNameColumn { get; set; }
    }
}
