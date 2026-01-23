using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using VS.Human.Business;
using VS.Human.Rep.Model;

namespace crmHuman.Pages.Meeting
{
    public class IndexModel : BaseModel2
    {
        private readonly IMeetingRoomBusiness _meetingRoomBusiness;

        public IndexModel(IMeetingRoomBusiness meetingRoomBusiness)
        {
            _meetingRoomBusiness = meetingRoomBusiness;
            KeyPage = "MeetingRoom";
            TitlePage = "Đặt phòng họp";
        }

        public List<MeetingRoomItem> Rooms { get; set; } = new List<MeetingRoomItem>();
        public List<MeetingBookingView> Bookings { get; set; } = new List<MeetingBookingView>();
        public DateTime WeekStart { get; set; }
        public DateTime WeekEnd { get; set; }
        public bool CanManage { get; set; }

        public async Task OnGetAsync(DateTime? date = null)
        {
            GetInfoUser();

            var baseDate = date?.Date ?? DateTime.Today;
            WeekStart = GetWeekStart(baseDate);
            WeekEnd = WeekStart.AddDays(6);

            Rooms = await _meetingRoomBusiness.GetRooms();
            Bookings = await _meetingRoomBusiness.GetBookings(WeekStart, WeekEnd.AddDays(1), null);
            CanManage = IsFullAccessRole(UserData?.RoleCode);
        }

        public async Task<IActionResult> OnGetDataAsync(DateTime from, DateTime to, int? roomId)
        {
            GetInfoUser();
            var fromDate = NormalizeFrom(from);
            var toDate = NormalizeTo(to);

            var bookings = await _meetingRoomBusiness.GetBookings(fromDate, toDate, roomId);
            return new JsonResult(bookings);
        }

        public async Task<IActionResult> OnPostSaveAsync([FromBody] MeetingBookingRequest request)
        {
            GetInfoUser();

            if (request == null)
            {
                return new JsonResult(new { success = false, message = "Du lieu khong hop le" });
            }

            if (request.Id > 0)
            {
                var existing = await _meetingRoomBusiness.GetBookingById(request.Id);
                if (existing == null || existing.Id <= 0)
                {
                    return new JsonResult(new { success = false, message = "Khong tim thay lich" });
                }

                if (!CanEditBooking(existing))
                {
                    return new JsonResult(new { success = false, message = "Khong co quyen sua" });
                }
            }
            else
            {
                if (!(Permision.Add ?? false))
                {
                    return new JsonResult(new { success = false, message = "Khong co quyen them" });
                }
            }

            var result = await _meetingRoomBusiness.SaveBooking(request, UserData.UserId);
            return new JsonResult(new { success = result.Success, message = result.Message, id = result.Id });
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            GetInfoUser();
            if (id <= 0)
            {
                return new JsonResult(new { success = false, message = "Id khong hop le" });
            }

            var existing = await _meetingRoomBusiness.GetBookingById(id);
            if (existing == null || existing.Id <= 0)
            {
                return new JsonResult(new { success = false, message = "Khong tim thay lich" });
            }

            if (!CanEditBooking(existing))
            {
                return new JsonResult(new { success = false, message = "Khong co quyen xoa" });
            }

            var result = await _meetingRoomBusiness.DeleteBooking(id, UserData.UserId);
            return new JsonResult(new { success = result });
        }

        private bool CanEditBooking(MeetingBookingView booking)
        {
            if (booking == null) return false;
            if (IsFullAccessRole(UserData?.RoleCode)) return true;
            return booking.CreatedBy == UserData.UserId;
        }

        private static bool IsFullAccessRole(string? roleCode)
        {
            return roleCode == "1" || roleCode == "8" || roleCode == "9";
        }

        private static DateTime GetWeekStart(DateTime date)
        {
            var diff = (7 + (int)date.DayOfWeek - (int)DayOfWeek.Monday) % 7;
            return date.AddDays(-diff).Date;
        }

        private static DateTime NormalizeFrom(DateTime from)
        {
            return from.TimeOfDay == TimeSpan.Zero ? from.Date : from;
        }

        private static DateTime NormalizeTo(DateTime to)
        {
            if (to.TimeOfDay == TimeSpan.Zero)
            {
                return to.Date.AddDays(1);
            }
            return to;
        }
    }
}
