using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VS.Human.Rep.Model;

namespace VS.Human.Rep
{
    public interface IMeetingRoomRep
    {
        Task<List<MeetingRoomItem>> GetRooms();
        Task<List<MeetingBookingView>> GetBookings(DateTime from, DateTime to, int? roomId);
        Task<MeetingBookingView?> GetBookingById(int id);
        Task<MeetingBookingView?> GetBookingByScheduleInterviewId(int scheduleInterviewId);
        Task<bool> HasOverlap(int roomId, DateTime startTime, DateTime endTime, int? ignoreId = null);
        Task<int> SaveBooking(MeetingBookingRequest request, int userId);
        Task<bool> DeleteBooking(int id, int userId);
    }
}
