using Dapper;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using VS.Human.Rep.Model;

namespace VS.Human.Rep
{
    public class MeetingRoomRep : RepositoryBase<MeetingRoomItem>, IMeetingRoomRep
    {
        public MeetingRoomRep(IConfiguration configuration) : base(configuration)
        {
        }

        public async Task<List<MeetingRoomItem>> GetRooms()
        {
            var sql = @"
                SELECT Id, Name, Location, Capacity, IsActive
                FROM MeetingRooms
                WHERE ISNULL(Deleted, 0) = 0 AND ISNULL(IsActive, 1) = 1
                ORDER BY Name";

            using var con = GetConnection();
            var result = await con.QueryAsync<MeetingRoomItem>(sql);
            return result?.ToList() ?? new List<MeetingRoomItem>();
        }

        public async Task<List<MeetingBookingView>> GetBookings(DateTime from, DateTime to, int? roomId)
        {
            var sql = @"
                SELECT 
                    b.Id,
                    b.RoomId,
                    r.Name AS RoomName,
                    b.Title,
                    b.Note,
                    b.StartTime,
                    b.EndTime,
                    b.CreatedBy,
                    e.FullName AS CreatedByName,
                    b.CreatedAt
                FROM MeetingBookings b
                INNER JOIN MeetingRooms r ON b.RoomId = r.Id
                LEFT JOIN Employees e ON b.CreatedBy = e.Id
                WHERE ISNULL(b.Deleted, 0) = 0
                    AND b.StartTime < @ToDate
                    AND b.EndTime > @FromDate
                    AND (@RoomId IS NULL OR b.RoomId = @RoomId)
                ORDER BY b.StartTime ASC";

            using var con = GetConnection();
            var result = await con.QueryAsync<MeetingBookingView>(sql, new { FromDate = from, ToDate = to, RoomId = roomId });
            return result?.ToList() ?? new List<MeetingBookingView>();
        }

        public async Task<MeetingBookingView?> GetBookingById(int id)
        {
            var sql = @"
                SELECT 
                    b.Id,
                    b.RoomId,
                    r.Name AS RoomName,
                    b.Title,
                    b.Note,
                    b.StartTime,
                    b.EndTime,
                    b.CreatedBy,
                    e.FullName AS CreatedByName,
                    b.CreatedAt
                FROM MeetingBookings b
                INNER JOIN MeetingRooms r ON b.RoomId = r.Id
                LEFT JOIN Employees e ON b.CreatedBy = e.Id
                WHERE ISNULL(b.Deleted, 0) = 0 AND b.Id = @Id";

            using var con = GetConnection();
            return await con.QueryFirstOrDefaultAsync<MeetingBookingView>(sql, new { Id = id });
        }

        public async Task<bool> HasOverlap(int roomId, DateTime startTime, DateTime endTime, int? ignoreId = null)
        {
            var sql = @"
                SELECT COUNT(1)
                FROM MeetingBookings
                WHERE ISNULL(Deleted, 0) = 0
                    AND RoomId = @RoomId
                    AND (@IgnoreId IS NULL OR Id <> @IgnoreId)
                    AND @StartTime < EndTime
                    AND @EndTime > StartTime";

            using var con = GetConnection();
            var count = await con.ExecuteScalarAsync<int>(sql, new { RoomId = roomId, StartTime = startTime, EndTime = endTime, IgnoreId = ignoreId });
            return count > 0;
        }

        public async Task<int> SaveBooking(MeetingBookingRequest request, int userId)
        {
            using var con = GetConnection();
            if (request.Id <= 0)
            {
                var sql = @"
                    INSERT INTO MeetingBookings (RoomId, Title, Note, StartTime, EndTime, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, Deleted)
                    VALUES (@RoomId, @Title, @Note, @StartTime, @EndTime, GETDATE(), @UserId, GETDATE(), @UserId, 0);
                    SELECT CAST(SCOPE_IDENTITY() as int);";

                return await con.ExecuteScalarAsync<int>(sql, new
                {
                    request.RoomId,
                    request.Title,
                    request.Note,
                    request.StartTime,
                    request.EndTime,
                    UserId = userId
                });
            }

            var updateSql = @"
                UPDATE MeetingBookings
                SET RoomId = @RoomId,
                    Title = @Title,
                    Note = @Note,
                    StartTime = @StartTime,
                    EndTime = @EndTime,
                    UpdatedAt = GETDATE(),
                    UpdatedBy = @UserId
                WHERE Id = @Id";

            await con.ExecuteAsync(updateSql, new
            {
                request.Id,
                request.RoomId,
                request.Title,
                request.Note,
                request.StartTime,
                request.EndTime,
                UserId = userId
            });

            return request.Id;
        }

        public async Task<bool> DeleteBooking(int id, int userId)
        {
            var sql = @"
                UPDATE MeetingBookings
                SET Deleted = 1,
                    UpdatedAt = GETDATE(),
                    UpdatedBy = @UserId
                WHERE Id = @Id";

            return await ExecuteSQL(sql, new { Id = id, UserId = userId }, CommandType.Text);
        }
    }
}
