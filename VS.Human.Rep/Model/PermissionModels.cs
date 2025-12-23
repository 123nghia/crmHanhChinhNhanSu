using VS.Human.Item;

namespace VS.Human.Rep.Model
{
    public class AppPage : BaseModel
    {
        // Code is the PK, but we might inherit Id from BaseModel. 
        // If BaseModel has int Id, we might ignore it or key mapped differently.
        // Let's stick to the relevant properties.
        public string Code { get; set; }
        public string Name { get; set; }
        public string Group { get; set; }
        public int OrderIndex { get; set; }
    }

    public class RoleAllow : BaseModel
    {
        public string RoleCode { get; set; }
        public string PageCode { get; set; }
        public bool IsView { get; set; }
        public bool IsAdd { get; set; }
        public bool IsEdit { get; set; }
        public bool IsDelete { get; set; }
        public bool IsApprove { get; set; }
    }

    // View Model for UI (combines Page info with Permission boolean)
    public class PermissionConfigViewModel : BaseIndexModel
    {
        public string PageCode { get; set; }
        public string PageName { get; set; }
        public string Group { get; set; }
        public bool IsView { get; set; }
        public bool IsAdd { get; set; }
        public bool IsEdit { get; set; }
        public bool IsDelete { get; set; }
        public bool IsApprove { get; set; }
        public int? Id { get; set; } // RoleAllow Id
    }
}
