namespace VS.Human.Item
{
    public class BaseIndexModel
    {
        public int TotalRecord { get; set; }
        public DateTime CreateAt { get; set; }
        public int CreatedBy { get; set; }
        public DateTime? UpdateAt { get; set; }
        public int UpdatedBy { get; set; }

        public bool Deleted { get; set; }
        public int Id { get; set; }

        public string? AuthorName { get; set; }


        public string CreateAtDisplay
        {
            get
            {
                if (CreateAt == null)
                {
                    return string.Empty;
                }
                return CreateAt.ToString("dd/MM/yyyy");
            }
        }

    }
}
