namespace crmHuman.Model
{
    /// <summary>
    /// DTO for quick update from editable grid
    /// Hỗ trợ cập nhật inline các trường cơ bản và mở rộng
    /// </summary>
    public class EmployeeQuickUpdate
    {
        public int? Id { get; set; }
        
        // Các trường cơ bản
        public string? FullName { get; set; }
        public string? RoleCode { get; set; }
        public string? PositionCode { get; set; }
        public string? DepartmentCode { get; set; }
        public int? GroupId { get; set; }
        public int? Status { get; set; }
        public string? StatusWork { get; set; }
        public string? DocumentStatus { get; set; }
        public DateTime? Onboard { get; set; }
        
        // Các trường mở rộng - Thông tin cá nhân
        public DateTime? Dob { get; set; }
        public string? Gender { get; set; }
        public string? PlaceOfBirth { get; set; }
        
        // CCCD
        public string? NationalId { get; set; }
        public DateTime? NationalDate { get; set; }
        public string? NationalPlace { get; set; }
        
        // Liên hệ
        public string? Phone { get; set; }
        public string? FingerprintCode { get; set; }
        public string? EmergencyContact { get; set; }
        public string? Email { get; set; }
        public string? PersonalEmail { get; set; }
        
        // Địa chỉ
        public string? PermanentAddress { get; set; }
        public string? TemporaryAddress { get; set; }
        
        // Học vấn, tình trạng
        public string? EducationLevel { get; set; }
        public string? Religion { get; set; }
        public string? Maritalstatus { get; set; }
        
        // Ngân hàng
        public string? BankAccount { get; set; }
        public string? BankName { get; set; }
        public string? BeneficiaryName { get; set; }
    }

    /// <summary>
    /// DTO for quick add from editable grid
    /// </summary>
    public class EmployeeQuickAdd
    {
        public string? FullName { get; set; }
        public string? RoleCode { get; set; }
        public string? PositionCode { get; set; }
        public string? DepartmentCode { get; set; }
        public int? GroupId { get; set; }
        public int? Status { get; set; }
        public string? StatusWork { get; set; }
        public string? DocumentStatus { get; set; }
        public DateTime? Onboard { get; set; }
        
        // Các trường mở rộng
        public DateTime? Dob { get; set; }
        public string? Gender { get; set; }
        public string? PlaceOfBirth { get; set; }
        public string? NationalId { get; set; }
        public DateTime? NationalDate { get; set; }
        public string? NationalPlace { get; set; }
        public string? Phone { get; set; }
        public string? FingerprintCode { get; set; }
        public string? EmergencyContact { get; set; }
        public string? Email { get; set; }
        public string? PersonalEmail { get; set; }
        public string? PermanentAddress { get; set; }
        public string? TemporaryAddress { get; set; }
        public string? EducationLevel { get; set; }
        public string? Religion { get; set; }
        public string? Maritalstatus { get; set; }
        public string? BankAccount { get; set; }
        public string? BankName { get; set; }
        public string? BeneficiaryName { get; set; }
    }
}
