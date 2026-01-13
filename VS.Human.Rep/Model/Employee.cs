namespace VS.Human.Rep.Model
{
    public class Employee : BaseModel
    {
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public string? LineCode { get; set; }
        public string? FingerprintCode { get; set; }
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
        public string TypeAccount { get; set; } = string.Empty;
        public string DepartmentCode { get; set; } = string.Empty;
        public string DocumentStatus { get; set; } = string.Empty;
        public string PositionCode { get; set; } = string.Empty;
        public int? GroupId { get; set; }

        public string? NationalId { get; set; }

        public string? CVLink { get; set; }

        public int? ManagerId { get; set; }
        public DateTime? NationalDate { get; set; }
        public string? NationalPlace { get; set; }
        public string? PermanentAddress { get; set; }
        public string? TemporaryAddress { get; set; }
        public string BankAccount { get; set; } = string.Empty;

        public string BankName { get; set; } = string.Empty;
        public string EducationLevel { get; set; } = string.Empty;
        public string Maritalstatus { get; set; } = string.Empty;
        public string DocumentCheck { get; set; } = string.Empty;

        public string StatusWork { get; set; } = string.Empty;

        // Các trường mới bổ sung từ yêu cầu
        public string? Gender { get; set; }              // Giới tính
        public string? PlaceOfBirth { get; set; }        // Nơi sinh
        public string? Religion { get; set; }           // Tôn giáo
        public string? PersonalEmail { get; set; }      // Email cá nhân
        public string? BeneficiaryName { get; set; }     // Tên chủ tài khoản
        public string? EmergencyContact { get; set; }    // Liên hệ khẩn cấp

    }
}
