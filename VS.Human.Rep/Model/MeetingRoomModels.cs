using System;

namespace VS.Human.Rep.Model
{
    public class MeetingRoomItem : BaseModel
    {
        public string Name { get; set; } = string.Empty;
        public string? Location { get; set; }
        public int? Capacity { get; set; }
        public int IsActive { get; set; }
    }

    public class MeetingBookingView
    {
        public int Id { get; set; }
        public int RoomId { get; set; }
        public int? ScheduleInterviewId { get; set; }
        public string RoomName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Note { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int CreatedBy { get; set; }
        public string? CreatedByName { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class MeetingBookingRequest
    {
        public int Id { get; set; }
        public int RoomId { get; set; }
        public int? ScheduleInterviewId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Note { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }

    public class MeetingBookingSaveResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public int? Id { get; set; }
    }
}
