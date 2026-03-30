using System.Collections.Generic;
using VS.Human.Item;

namespace VS.Human.Rep.Model
{
    public class SupportRequestItem : BaseModel
    {
        public int RequesterId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string TargetDepartmentCode { get; set; } = string.Empty;
        public int? AssignedToId { get; set; }
        public int Status { get; set; }
        public string? ProcessorComment { get; set; }
        public DateTime? AssignedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public List<SupportRequestAttachment> Attachments { get; set; } = new List<SupportRequestAttachment>();
    }

    public class SupportRequestIndexModel : BaseIndexModel
    {
        public int RequesterId { get; set; }
        public string RequesterName { get; set; } = string.Empty;
        public string? RequesterDepartmentCode { get; set; }
        public string? RequesterDepartmentText { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string TargetDepartmentCode { get; set; } = string.Empty;
        public string? TargetDepartmentText { get; set; }
        public int? AssignedToId { get; set; }
        public string? AssignedToName { get; set; }
        public int Status { get; set; }
        public string? ProcessorComment { get; set; }
        public DateTime? AssignedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public bool CanProcess { get; set; }
        public List<SupportRequestAttachment> Attachments { get; set; } = new List<SupportRequestAttachment>();
    }

    public class SupportRequestAttachment
    {
        public int Id { get; set; }
        public int RequestId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public long? FileSize { get; set; }
        public DateTime CreateAt { get; set; }
    }

    public class SupportRequestHistory : BaseModel
    {
        public int RequestId { get; set; }
        public string Action { get; set; } = string.Empty;
        public int ActionBy { get; set; }
        public string ActionByName { get; set; } = string.Empty;
        public DateTime ActionTime { get; set; }
        public string Comment { get; set; } = string.Empty;
        public int StatusAfter { get; set; }
    }

    public class SupportRequestStatusUpdate
    {
        public int Id { get; set; }
        public int Status { get; set; }
        public string? Comment { get; set; }
    }
}
