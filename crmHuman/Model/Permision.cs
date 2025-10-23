namespace crmHuman.Model
{
    public class Permision
    {
    }
}
public class PermisionUser
{
    public bool? Delete { get; set; }
    public bool? Edit { get; set; }
    public bool? View { get; set; }

    public bool? Add { get; set; }

    public bool? ViewList { get; set; }

    public bool? SearchGrop {get;set;}

    public string KeyPage { get; set; }

    public bool? Assignee {get;set;}

     public bool? ImportMasketting {get;set;}
    public PermisionUser()
    {
        Delete = true;
        Edit = true;
        View = true;
        Add = true;
        ViewList = true;
        KeyPage = "";
        SearchGrop = true;
        Assignee=false;
        ImportMasketting = false;
    }
}