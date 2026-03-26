using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Linq;
using VS.Human.Business.Model;
using VS.Human.Item;
using VS.Human.Rep;
using VS.Human.Rep.Model;

namespace VS.Human.Business.Imp
{
    public class ScheduleInterviewBusiness : BaseBusiness, IScheduleInterviewBussiness
    {
        private const string InterviewTemplateCode = "INTERVIEW_SCHEDULE";
        private const int DefaultInterviewDurationMinutes = 60;

        private readonly IEmailConfigBusiness _emailConfigBusiness;
        private readonly IEmailService _emailService;
        private readonly INotificationBusiness _notificationBusiness;
        private readonly ILogger<ScheduleInterviewBusiness> _logger;

        public ScheduleInterviewBusiness(
            IUnitOfWork unitOfWork,
            IHttpContextAccessor httpContextAccessor,
            IEmailConfigBusiness emailConfigBusiness,
            IEmailService emailService,
            INotificationBusiness notificationBusiness,
            ILogger<ScheduleInterviewBusiness> logger)
            : base(unitOfWork, httpContextAccessor)
        {
            _emailConfigBusiness = emailConfigBusiness;
            _emailService = emailService;
            _notificationBusiness = notificationBusiness;
            _logger = logger;
        }

        public async Task<InterviewScheduleSaveResult> SaveInterviewSchedule(ScheduleInterviewAdd item, int actorUserId)
        {
            var result = new InterviewScheduleSaveResult();
            if (item == null)
            {
                result.Message = "Du lieu lich phong van khong hop le";
                return result;
            }

            if (item.RelId <= 0)
            {
                result.Message = "Ung vien khong hop le";
                return result;
            }

            if (!item.ScheduleDate.HasValue)
            {
                result.Message = "Vui long chon ngay gio phong van";
                return result;
            }

            var isNew = item.Id <= 0;
            ScheduleInterview? existingSchedule = null;
            if (!isNew)
            {
                existingSchedule = await _unitOfWork.ScheduleInterviewRep.GetById(item.Id);
                if (existingSchedule == null || existingSchedule.Id <= 0)
                {
                    result.Message = "Khong tim thay lich phong van";
                    return result;
                }
            }

            var candidate = await _unitOfWork.CandidateRep.GetById(item.RelId);
            if (candidate == null || candidate.Id <= 0)
            {
                result.Message = "Khong tim thay ung vien";
                return result;
            }

            if (item.SendEmail && string.IsNullOrWhiteSpace(GetCandidateRecipientEmail(candidate)))
            {
                result.Message = "Ung vien chua co email hop le. Vui long cap nhat email trong ho so ung vien truoc khi lap lich de gui thu moi tu dong.";
                return result;
            }

            item.CreatedBy = isNew ? (item.CreatedBy > 0 ? item.CreatedBy : actorUserId) : existingSchedule!.CreatedBy;
            item.UpdatedBy = actorUserId;

            var bookingPlan = await BuildBookingPlanAsync(item, existingSchedule, candidate);
            if (!bookingPlan.Success)
            {
                result.Message = bookingPlan.ErrorMessage;
                return result;
            }

            if (bookingPlan.OverrideAddressInfo != null)
            {
                item.AddressInfo = bookingPlan.OverrideAddressInfo;
            }

            var scheduleId = await _unitOfWork.ScheduleInterviewRep.SaveAndGetId(item);
            if (scheduleId <= 0)
            {
                result.Message = "Khong the luu lich phong van";
                return result;
            }

            item.Id = scheduleId;
            result.Success = true;
            result.ScheduleId = scheduleId;

            if (bookingPlan.DeleteExistingBooking && bookingPlan.ExistingBookingId.HasValue)
            {
                var deleted = await _unitOfWork.MeetingRoomRep.DeleteBooking(bookingPlan.ExistingBookingId.Value, actorUserId);
                if (!deleted)
                {
                    _logger.LogWarning(
                        "Failed to delete meeting booking linked to interview schedule. ScheduleId={ScheduleId}, BookingId={BookingId}",
                        scheduleId,
                        bookingPlan.ExistingBookingId.Value);
                }
            }

            if (bookingPlan.BookingRequest != null)
            {
                bookingPlan.BookingRequest.ScheduleInterviewId = scheduleId;
                var bookingId = await _unitOfWork.MeetingRoomRep.SaveBooking(bookingPlan.BookingRequest, actorUserId);
                if (bookingId > 0)
                {
                    result.MeetingRoomBooked = true;
                    result.MeetingRoomName = bookingPlan.RoomName;
                }
                else
                {
                    _logger.LogWarning(
                        "Interview schedule saved but meeting room booking failed. ScheduleId={ScheduleId}, CandidateId={CandidateId}",
                        scheduleId,
                        candidate.Id);
                }
            }

            await NotifyCandidateAsync(candidate, item, isNew);
            if (item.SendEmail)
            {
                var emailResult = await TrySendCandidateEmailAsync(candidate, item, bookingPlan.RoomName, isNew);
                result.EmailSent = emailResult.Success;
                result.EmailError = emailResult.Error;
            }

            result.Message = BuildSuccessMessage(isNew, result);
            return result;
        }

        public async Task<bool> AddOrUpdate(ScheduleInterviewAdd item)
        {
            return await _unitOfWork.ScheduleInterviewRep.AddOrUpdate(item);
        }

        public async Task<bool> Delete(int id)
        {
            var deleted = await _unitOfWork.ScheduleInterviewRep.Delete(id);
            if (!deleted)
            {
                return false;
            }

            var linkedBooking = await _unitOfWork.MeetingRoomRep.GetBookingByScheduleInterviewId(id);
            if (linkedBooking != null && linkedBooking.Id > 0)
            {
                var actorUserId = 0;
                try
                {
                    actorUserId = GetUserId();
                }
                catch
                {
                    actorUserId = 0;
                }

                var deletedBooking = await _unitOfWork.MeetingRoomRep.DeleteBooking(linkedBooking.Id, actorUserId);
                if (!deletedBooking)
                {
                    _logger.LogWarning(
                        "Interview schedule deleted but linked meeting booking could not be removed. ScheduleId={ScheduleId}, BookingId={BookingId}",
                        id,
                        linkedBooking.Id);
                }
            }

            return true;
        }

        public async Task<BaseList> GetAll(ScheduleInterviewRquest request)
        {
            return await _unitOfWork.ScheduleInterviewRep.GetAll(request);
        }

        public async Task<bool> HasViewAccess(int scheduleId, int userId, string? roleCode)
        {
            return await _unitOfWork.ScheduleInterviewRep.HasViewAccess(scheduleId, userId, roleCode);
        }

        public async Task<bool> HasManageAccess(int scheduleId, int userId, string? roleCode)
        {
            return await _unitOfWork.ScheduleInterviewRep.HasManageAccess(scheduleId, userId, roleCode);
        }

        public async Task<BaseList> GetallRegional()
        {
            return await _unitOfWork.LocationRep.GetAll();
        }

        public async Task<ScheduleInterview> GetById(int id)
        {
            return await _unitOfWork.ScheduleInterviewRep.GetById(id);
        }

        private async Task<BookingPlan> BuildBookingPlanAsync(ScheduleInterviewAdd item, ScheduleInterview? existingSchedule, Candidate candidate)
        {
            var plan = new BookingPlan { Success = true };
            var existingBooking = item.Id > 0
                ? await _unitOfWork.MeetingRoomRep.GetBookingByScheduleInterviewId(item.Id)
                : null;

            var isOnline = item.InterviewMode == 2;
            var isCancelled = item.Status == 3;
            if (isOnline || isCancelled)
            {
                plan.DeleteExistingBooking = existingBooking != null && existingBooking.Id > 0;
                plan.ExistingBookingId = existingBooking?.Id;
                return plan;
            }

            if (!item.ScheduleDate.HasValue)
            {
                return plan;
            }

            var startTime = item.ScheduleDate.Value;
            var endTime = startTime.AddMinutes(DefaultInterviewDurationMinutes);

            var rooms = await _unitOfWork.MeetingRoomRep.GetRooms();
            if (rooms.Count == 0)
            {
                plan.Success = false;
                plan.ErrorMessage = "Khong co phong hop dang hoat dong de dat lich";
                return plan;
            }

            MeetingRoomItem? selectedRoom = null;
            if (existingBooking != null && existingBooking.RoomId > 0)
            {
                var roomStillAvailable = !await _unitOfWork.MeetingRoomRep.HasOverlap(existingBooking.RoomId, startTime, endTime, existingBooking.Id);
                if (roomStillAvailable)
                {
                    selectedRoom = rooms.FirstOrDefault(x => x.Id == existingBooking.RoomId);
                }
            }

            if (selectedRoom == null)
            {
                foreach (var room in rooms.OrderBy(x => x.Name))
                {
                    var hasOverlap = await _unitOfWork.MeetingRoomRep.HasOverlap(room.Id, startTime, endTime, existingBooking?.Id);
                    if (!hasOverlap)
                    {
                        selectedRoom = room;
                        break;
                    }
                }
            }

            if (selectedRoom == null)
            {
                plan.Success = false;
                plan.ErrorMessage = "Khong con phong hop trong trong khung gio nay";
                return plan;
            }

            var roomText = BuildRoomText(selectedRoom);
            plan.RoomName = roomText;
            plan.ExistingBookingId = existingBooking?.Id;
            plan.OverrideAddressInfo = MergeAddressInfo(roomText, item.AddressInfo);
            plan.BookingRequest = new MeetingBookingRequest
            {
                Id = existingBooking?.Id ?? 0,
                RoomId = selectedRoom.Id,
                Title = BuildMeetingTitle(candidate, item),
                Note = BuildMeetingNote(candidate, item, roomText),
                StartTime = startTime,
                EndTime = endTime
            };

            return plan;
        }

        private async Task NotifyCandidateAsync(Candidate candidate, ScheduleInterviewAdd item, bool isNew)
        {
            if (candidate == null || candidate.Id <= 0 || !item.ScheduleDate.HasValue)
            {
                return;
            }

            var scheduleText = item.ScheduleDate.Value.ToString("HH:mm dd/MM/yyyy");
            var message = isNew
                ? $"Ban co lich phong van moi vao luc {scheduleText}."
                : $"Lich phong van cua ban vao luc {scheduleText} da duoc cap nhat.";

            await _notificationBusiness.CreateNotification(
                candidate.Id,
                message,
                "/Candidate/Dashboard",
                isNew ? "InterviewScheduled" : "InterviewUpdated",
                item.UpdatedBy > 0 ? item.UpdatedBy : item.CreatedBy);
        }

        private async Task<(bool Success, string? Error)> TrySendCandidateEmailAsync(Candidate candidate, ScheduleInterviewAdd item, string? roomName, bool isNew)
        {
            var toEmail = GetCandidateRecipientEmail(candidate);
            if (string.IsNullOrWhiteSpace(toEmail))
            {
                return (false, "Ung vien chua co email hop le.");
            }

            await _emailConfigBusiness.EnsureDefaultTemplates(item.UpdatedBy > 0 ? item.UpdatedBy : item.CreatedBy);

            var interviewer = item.InterviewerId.HasValue && item.InterviewerId.Value > 0
                ? await _unitOfWork.EmployeeRep.GetById(item.InterviewerId.Value)
                : null;
            var appliedPosition = await ResolveCandidatePositionTextAsync(candidate);

            var tokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["CandidateName"] = candidate.Name ?? string.Empty,
                ["EmployeeName"] = candidate.Name ?? string.Empty,
                ["AppliedPosition"] = appliedPosition,
                ["CandidatePosition"] = appliedPosition,
                ["PositionText"] = appliedPosition,
                ["InterviewAction"] = isNew ? "da duoc len lich" : "da duoc cap nhat",
                ["InterviewRound"] = GetInterviewRoundText(item.Type),
                ["InterviewMode"] = GetInterviewModeText(item.InterviewMode),
                ["ScheduleDate"] = item.ScheduleDate?.ToString("dd/MM/yyyy HH:mm") ?? string.Empty,
                ["AddressInfo"] = item.AddressInfo ?? string.Empty,
                ["InterviewerName"] = interviewer?.FullName ?? string.Empty,
                ["RoomName"] = roomName ?? string.Empty,
                ["Noted"] = item.Noted ?? string.Empty
            };

            var sendResult = await _emailService.SendTemplateWithErrorAsync(
                InterviewTemplateCode,
                new[] { toEmail },
                tokens,
                null,
                null,
                null,
                item.UpdatedBy > 0 ? item.UpdatedBy : item.CreatedBy);
            if (!sendResult.Success)
            {
                _logger.LogWarning(
                    "Interview email failed. CandidateId={CandidateId}, ScheduleId={ScheduleId}, Error={Error}",
                    candidate.Id,
                    item.Id,
                    sendResult.Error ?? "Unknown error");

                return (false, "Khong gui duoc mail moi. Vui long kiem tra cau hinh mail trong phan quan tri.");
            }

            _logger.LogInformation(
                "Interview email sent. CandidateId={CandidateId}, ScheduleId={ScheduleId}, Recipient={Recipient}",
                candidate.Id,
                item.Id,
                toEmail);

            return (true, null);
        }

        private static string? GetCandidateRecipientEmail(Candidate candidate)
        {
            if (candidate == null || candidate.Id <= 0 || string.IsNullOrWhiteSpace(candidate.Email))
            {
                return null;
            }

            var trimmedEmail = candidate.Email.Trim();
            return MailboxAddress.TryParse(trimmedEmail, out var mailbox) ? mailbox.Address : null;
        }

        private async Task<string> ResolveCandidatePositionTextAsync(Candidate candidate)
        {
            if (candidate == null || candidate.Id <= 0 || !candidate.Position.HasValue || candidate.Position.Value <= 0)
            {
                return string.Empty;
            }

            var positionValue = candidate.Position.Value.ToString();
            var position = await _unitOfWork.MasterDataRep.GetByCode(positionValue, 2);
            if (position == null || position.Id <= 0 || string.IsNullOrWhiteSpace(position.Name))
            {
                position = await _unitOfWork.MasterDataRep.GetById(candidate.Position.Value);
            }

            if (position == null || position.Id <= 0 || string.IsNullOrWhiteSpace(position.Name))
            {
                return string.Empty;
            }

            return position.Name.Trim();
        }

        private static string BuildSuccessMessage(bool isNew, InterviewScheduleSaveResult result)
        {
            var parts = new List<string>
            {
                isNew ? "Da tao lich phong van" : "Da cap nhat lich phong van"
            };

            if (result.MeetingRoomBooked && !string.IsNullOrWhiteSpace(result.MeetingRoomName))
            {
                parts.Add($"Da dat phong hop {result.MeetingRoomName}");
            }

            if (result.EmailSent)
            {
                parts.Add("Da gui mail moi ung vien");
            }
            else if (!string.IsNullOrWhiteSpace(result.EmailError))
            {
                parts.Add(result.EmailError);
            }

            return string.Join(". ", parts) + ".";
        }

        private static string BuildMeetingTitle(Candidate candidate, ScheduleInterviewAdd item)
        {
            var candidateName = string.IsNullOrWhiteSpace(candidate.Name) ? $"Candidate {candidate.Id}" : candidate.Name.Trim();
            return $"Phong van - {candidateName} - {GetInterviewRoundText(item.Type)}";
        }

        private static string BuildMeetingNote(Candidate candidate, ScheduleInterviewAdd item, string roomName)
        {
            var noteParts = new List<string>
            {
                $"Ung vien: {candidate.Name ?? candidate.Code ?? candidate.Id.ToString()}",
                $"Vong: {GetInterviewRoundText(item.Type)}",
                $"Phong: {roomName}"
            };

            if (!string.IsNullOrWhiteSpace(item.Noted))
            {
                noteParts.Add($"Ghi chu: {item.Noted}");
            }

            return string.Join(" | ", noteParts);
        }

        private static string MergeAddressInfo(string roomText, string? currentAddressInfo)
        {
            if (string.IsNullOrWhiteSpace(currentAddressInfo))
            {
                return roomText;
            }

            if (currentAddressInfo.Contains(roomText, StringComparison.OrdinalIgnoreCase))
            {
                return currentAddressInfo;
            }

            return $"{roomText} - {currentAddressInfo.Trim()}";
        }

        private static string BuildRoomText(MeetingRoomItem room)
        {
            if (string.IsNullOrWhiteSpace(room.Location))
            {
                return room.Name;
            }

            return $"{room.Name} ({room.Location})";
        }

        private static string GetInterviewRoundText(int? type)
        {
            return type switch
            {
                1 => "HR",
                2 => "Technical",
                3 => "Final",
                0 => "Khac",
                _ => "Phong van"
            };
        }

        private static string GetInterviewModeText(int? mode)
        {
            return mode switch
            {
                2 => "Online",
                _ => "Offline"
            };
        }

        private sealed class BookingPlan
        {
            public bool Success { get; set; }
            public string? ErrorMessage { get; set; }
            public string? RoomName { get; set; }
            public string? OverrideAddressInfo { get; set; }
            public bool DeleteExistingBooking { get; set; }
            public int? ExistingBookingId { get; set; }
            public MeetingBookingRequest? BookingRequest { get; set; }
        }
    }
}
