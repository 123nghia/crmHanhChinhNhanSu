namespace VS.Human.Rep.Model
{
    public class Employee : BaseModel
    {
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public string? LineCode { get; set; }
        public string? ColorCode { get; set; }
        public DateTime? Onboard { get; set; }
        public string? FullName { get; set; }
        public string? Phone { get; set; }
        public string? Noted { get; set; }
        public string? RoleCode { get; set; }
        public string? Pass { get; set; }
        public DateTime? Dob { get; set; }
        public int Status { get; set; }
        public int IsActive { get; set; }
        public string? AvatarFile { get; set; }
        public string TypeAccount { get; set; }
        public string DepartmentCode { get; set; }
        public string DocumentStatus { get; set; }
        public string PositionCode { get; set; }

        public string? NationalId { get; set; }

        public string? CVLink { get; set; }

        public int? ManagerId { get; set; }
        public DateTime? NationalDate { get; set; }
        public string? NationalPlace { get; set; }
        public string? PermanentAddress { get; set; }
        public string? TemporaryAddress { get; set; }
        public string BankAccount { get; set; }

        public string BankName { get; set; }
        public string EducationLevel { get; set; }
        public string Maritalstatus { get; set; }
        public string DocumentCheck { get; set; }

        public string StatusWork { get; set; }

    }
}
