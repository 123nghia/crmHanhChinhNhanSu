namespace VS.Human.Rep.Model
{
    public class Candidate : BaseModel
    {
        public string? Code { get; set; }
        public int? Source { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Name { get; set; }
        public DateTime? Dob { get; set; }
        public string CVLink { get; set; } = string.Empty;
        public string? ShortDes { get; set; }
        public int? IsActive { get; set; }
        public string? Noted { get; set; }
        public int? Status { get; set; }
        public int? StatusHuman { get; set; }
        public int? ManagerId { get; set; }
        public int? Position { get; set; }
        public int? DepartmentId { get; set; }
        public string? NationalId { get; set; }
        public string? Address { get; set; }

        public string Referrer { get; set; } = string.Empty;
        public Candidate()
        {
            Source = 0;
        }
    }
}
