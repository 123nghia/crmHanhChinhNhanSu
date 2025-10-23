namespace VS.Human.Item
{
    public class BaseRequest
    {
        public string? Token { get; set; }
        public bool? IsDeleted { get; set; }
        public int? UserId { get; set; }

        public string? UserName { get; set; }
        public int Page { get; set; }
        public int Limit { get; set; }
        public string? OrderBy { get; set; }
        public int? Status { get; set; }
        private DateTime? FromTimeAss { get; set; }
        public string? FromText
        {
            get
            {
                if (From.HasValue)
                {
                    return From.Value.ToString("yyyy-MM-dd");
                    //return From.Value.ToString("dd/MM/yyyy");
                }
                return "";
            }
        }
        public string? ToText
        {
            get
            {
                if (To.HasValue)
                {
                    return To.Value.ToString("yyyy-MM-dd");
                    //return To.Value.ToString("dd-MM-yyyy");
                }
                return "";
            }
        }
        public DateTime? From
        {

            get
            {
                return FromTimeAss;
            }
            set
            {
                if (value.HasValue)
                {
                    FromTimeAss = new DateTime(value.Value.Year, value.Value.Month, value.Value.Day, 0, 0, 0);

                }
                else
                {
                    FromTimeAss = null;
                }

            }
        }
        private DateTime? ToTimeAss { get; set; }
        public DateTime? To
        {
            get
            {
                return ToTimeAss;
            }
            set
            {
                if (value.HasValue)
                {
                    ToTimeAss = new DateTime(value.Value.Year, value.Value.Month, value.Value.Day, 23, 59, 59);

                }
                else
                {
                    ToTimeAss = null;
                }

            }
        }
        public BaseRequest()
        {
            Page = 1;
            FromTimeAss = DateTime.Now.AddMonths(-1);
            ToTimeAss = DateTime.Now;
            Limit = 10;
            Token = "";
        }
    }

    public class GroupMemberRequest : BaseRequest
    {
        public int GroupId { get; set; }
    }
}
