namespace VS.Human.Business.Model
{
    public class InterviewScheduleSaveResult
    {
        public bool Success { get; set; }
        public int ScheduleId { get; set; }
        public bool EmailSent { get; set; }
        public string? EmailError { get; set; }
        public bool MeetingRoomBooked { get; set; }
        public string? MeetingRoomName { get; set; }
        public string? Message { get; set; }
    }
}
