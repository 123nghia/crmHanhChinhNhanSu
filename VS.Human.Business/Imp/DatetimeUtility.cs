namespace VS.Human.Business.Imp
{
    public static class DatetimeUtility
    {
        public static DateTime? EndDateTime(this DateTime? item)
        {
            if (!item.HasValue)
                return new DateTime?();
            return new DateTime?(new DateTime(item.Value.Year, item.Value.Month, item.Value.Day, 23, 59, 59));
        }
    }
}
