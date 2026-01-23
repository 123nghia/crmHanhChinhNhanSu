using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VS.Human.Business.Imp;
using VS.Human.Rep;
using VS.Human.Rep.Model;

namespace VS.Human.Business
{
    public class MeetingRoomBusiness : BaseBusiness, IMeetingRoomBusiness
    {
        public MeetingRoomBusiness(IUnitOfWork unitOfWork, IHttpContextAccessor contextAccessor)
            : base(unitOfWork, contextAccessor)
        {
        }

        public async Task<List<MeetingRoomItem>> GetRooms()
        {
            return await _unitOfWork.MeetingRoomRep.GetRooms();
        }

        public async Task<List<MeetingBookingView>> GetBookings(DateTime from, DateTime to, int? roomId)
        {
            return await _unitOfWork.MeetingRoomRep.GetBookings(from, to, roomId);
        }

        public async Task<MeetingBookingView?> GetBookingById(int id)
        {
            return await _unitOfWork.MeetingRoomRep.GetBookingById(id);
        }

        public async Task<MeetingBookingSaveResult> SaveBooking(MeetingBookingRequest request, int userId)
        {
            if (request.RoomId <= 0)
            {
                return new MeetingBookingSaveResult { Success = false, Message = "Phong hop khong hop le" };
            }

            if (string.IsNullOrWhiteSpace(request.Title))
            {
                return new MeetingBookingSaveResult { Success = false, Message = "Vui long nhap tieu de" };
            }

            if (request.EndTime <= request.StartTime)
            {
                return new MeetingBookingSaveResult { Success = false, Message = "Gio ket thuc phai lon hon gio bat dau" };
            }

            if (request.StartTime.Date != request.EndTime.Date)
            {
                return new MeetingBookingSaveResult { Success = false, Message = "Thoi gian dat phong phai trong cung 1 ngay" };
            }

            var hasOverlap = await _unitOfWork.MeetingRoomRep.HasOverlap(request.RoomId, request.StartTime, request.EndTime, request.Id > 0 ? request.Id : null);
            if (hasOverlap)
            {
                return new MeetingBookingSaveResult { Success = false, Message = "Phong da duoc dat trong khung gio nay" };
            }

            var id = await _unitOfWork.MeetingRoomRep.SaveBooking(request, userId);
            return new MeetingBookingSaveResult { Success = id > 0, Id = id, Message = id > 0 ? null : "Khong the luu" };
        }

        public async Task<bool> DeleteBooking(int id, int userId)
        {
            return await _unitOfWork.MeetingRoomRep.DeleteBooking(id, userId);
        }
    }
}
