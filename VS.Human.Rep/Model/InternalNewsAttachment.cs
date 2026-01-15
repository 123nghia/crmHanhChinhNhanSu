namespace VS.Human.Rep.Model
{
    public class InternalNewsAttachment
    {
        public int Id { get; set; }
        public int NewsId { get; set; }
        public string? FileName { get; set; }
        public string? FilePath { get; set; }
        public long? FileSize { get; set; }
        public DateTime CreateAt { get; set; }
        public int CreatedBy { get; set; }
        public DateTime UpdateAt { get; set; }
        public int UpdatedBy { get; set; }
        public bool Deleted { get; set; }
        public int IsActive { get; set; }
    }
}
