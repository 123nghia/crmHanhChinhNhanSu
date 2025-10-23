using System.Collections;

namespace VS.Human.Rep.Model
{
    public class GroupItem : BaseModel
    {
        public string? Code { get; set; }
        public string? Name { get; set; }
        public string? ManagerId { get; set; }
        public int Status { get; set; }
        public int IsActive { get; set; }
        public string? Noted { get; set; }
        public IEnumerable? ListMember { get; set; }

        public IEnumerable? ManagerList { get; set; }

        public IEnumerable? ListMeberNotGroup { get; set; }


    }
}
