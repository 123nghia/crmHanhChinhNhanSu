using VS.Human.Rep.Model;

namespace VS.Human.Business
{
    public enum LeaveAttendanceImpactKind
    {
        None = 0,
        Apply = 1,
        Rollback = 2,
        Recalculate = 3
    }

    public sealed class LeaveAttendanceImpactResult
    {
        public LeaveAttendanceImpactKind Kind { get; init; }
        public int LeaveId { get; init; }
        public DateTime? RangeFrom { get; init; }
        public DateTime? RangeTo { get; init; }
        public bool WasEffectiveBefore { get; init; }
        public bool IsEffectiveAfter { get; init; }
        public string Reason { get; init; } = string.Empty;

        public bool RequiresAttendanceSync => Kind != LeaveAttendanceImpactKind.None
            && RangeFrom.HasValue
            && RangeTo.HasValue
            && LeaveId > 0;

        public static LeaveAttendanceImpactResult None(int leaveId, string reason)
        {
            return new LeaveAttendanceImpactResult
            {
                Kind = LeaveAttendanceImpactKind.None,
                LeaveId = leaveId,
                Reason = reason
            };
        }
    }

    public interface ILeaveAttendanceImpactResolver
    {
        LeaveAttendanceImpactResult Resolve(LeaveIndexModel? before, LeaveIndexModel? after, string action);
    }

    public sealed class LeaveAttendanceImpactResolver : ILeaveAttendanceImpactResolver
    {
        public LeaveAttendanceImpactResult Resolve(LeaveIndexModel? before, LeaveIndexModel? after, string action)
        {
            var leaveId = after?.Id ?? before?.Id ?? 0;
            if (leaveId <= 0)
            {
                return LeaveAttendanceImpactResult.None(0, "Leave id is missing.");
            }

            var wasEffective = IsAttendanceEffective(before);
            var isEffective = IsAttendanceEffective(after);

            if (!wasEffective && !isEffective)
            {
                return LeaveAttendanceImpactResult.None(leaveId, "Leave is not attendance-effective before or after change.");
            }

            if (!wasEffective && isEffective)
            {
                return BuildResult(LeaveAttendanceImpactKind.Apply, leaveId, after, after, wasEffective, isEffective, action);
            }

            if (wasEffective && !isEffective)
            {
                return BuildResult(LeaveAttendanceImpactKind.Rollback, leaveId, before, before, wasEffective, isEffective, action);
            }

            if (HasEffectiveAttendanceChange(before, after))
            {
                return BuildResult(LeaveAttendanceImpactKind.Recalculate, leaveId, before, after, wasEffective, isEffective, action);
            }

            return LeaveAttendanceImpactResult.None(leaveId, "Effective leave state did not change.");
        }

        private static LeaveAttendanceImpactResult BuildResult(
            LeaveAttendanceImpactKind kind,
            int leaveId,
            LeaveIndexModel? before,
            LeaveIndexModel? after,
            bool wasEffective,
            bool isEffective,
            string action)
        {
            var (from, to) = ResolveRange(before, after);
            return new LeaveAttendanceImpactResult
            {
                Kind = kind,
                LeaveId = leaveId,
                RangeFrom = from,
                RangeTo = to,
                WasEffectiveBefore = wasEffective,
                IsEffectiveAfter = isEffective,
                Reason = $"{action}: {kind}"
            };
        }

        private static (DateTime? from, DateTime? to) ResolveRange(LeaveIndexModel? before, LeaveIndexModel? after)
        {
            var dates = new List<DateTime>();
            AddRangeDates(dates, before);
            AddRangeDates(dates, after);

            if (dates.Count == 0)
            {
                return (null, null);
            }

            return (dates.Min().Date, dates.Max().Date);
        }

        private static void AddRangeDates(List<DateTime> dates, LeaveIndexModel? leave)
        {
            if (leave == null || leave.Id <= 0)
            {
                return;
            }

            var from = leave.FromDate.Date;
            var to = leave.ToDate.Date;
            if (from > to)
            {
                (from, to) = (to, from);
            }

            dates.Add(from);
            dates.Add(to);
        }

        private static bool IsAttendanceEffective(LeaveIndexModel? leave)
        {
            return leave != null && (leave.Status == 3 || leave.Status == 4);
        }

        private static bool HasEffectiveAttendanceChange(LeaveIndexModel? before, LeaveIndexModel? after)
        {
            if (before == null || after == null)
            {
                return before?.Id != after?.Id;
            }

            return before.EmployeeId != after.EmployeeId
                || before.FromDate.Date != after.FromDate.Date
                || before.ToDate.Date != after.ToDate.Date
                || !string.Equals(before.LeaveTypeCode?.Trim(), after.LeaveTypeCode?.Trim(), StringComparison.OrdinalIgnoreCase)
                || Math.Round(before.NumDays ?? 0m, 2) != Math.Round(after.NumDays ?? 0m, 2);
        }
    }
}
