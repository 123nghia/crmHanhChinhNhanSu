using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VS.Human.Rep.Model;

namespace VS.Human.Business
{
    public interface IMeetingRoomBusiness
    {
        Task<List<MeetingRoomItem>> GetRooms();
        Task<List<MeetingBookingView>> GetBookings(DateTime from, DateTime to, int? roomId);
        Task<MeetingBookingView?> GetBookingById(int id);
        Task<MeetingBookingSaveResult> SaveBooking(MeetingBookingRequest request, int userId);
        Task<bool> DeleteBooking(int id, int userId);
    }
}
