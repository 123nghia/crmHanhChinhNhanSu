using crmHuman.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace crmHuman.Pages
{


    [Authorize]
    public class BaseModel2 : PageModel
    {
        public PermisionUser Permision { get; set; }



        public GlobalVar GlobalData { get; set; }

        public UserActive UserDataGlobal { get; set; }

        public string TitlePage { get; set; }

        public string KeyPage { get; set; }

        public string NameController { get; set; }

        public string TitleList { get; set; }

        public int? RecordSource { get; set; }



        public List<SelectDisplay> arrayRol =

            new List<SelectDisplay>()
        {
            new Model.SelectDisplay()
            {
                Code ="1", Name ="Admin"
            },
            new Model.SelectDisplay()
            {
            Code ="2", Name ="TC"
            },
            new Model.SelectDisplay()
            {
            Code ="9", Name ="HCNS"
            },
            new Model.SelectDisplay()
            {
            Code ="3", Name ="TL"
            },
             new Model.SelectDisplay()
            {
            Code ="8", Name ="BGÐ"
            }
        };

        public List<string> TableColumnText { get; set; }

        public string GetNameRoleCode(string value)
        {
            string temp = "";
            foreach (var item in arrayRol)
            {
                if (item.Code == value)
                {
                    temp = item.Name;
                    break;
                }
            }
            return temp;
        }

        public List<SelectDisplay> arrayStatus = new List<crmHuman.Model.SelectDisplay>()
        {
            new Model.SelectDisplay()
            {
                Code ="0", Name ="Không ho?t d?ng"
            },
            new Model.SelectDisplay()
            {
            Code ="1", Name ="Ho?t d?ng"
            }

        };

        public List<SelectDisplay> arrayDeleted = new List<crmHuman.Model.SelectDisplay>()
        {
            new Model.SelectDisplay()
            {
                Code ="0", Name ="Online"
            },
            new Model.SelectDisplay()
            {
            Code ="1", Name ="Deactivate"
            }

        };

        public string GetStatusDelete(bool code)
        {

            string temp = "";
            temp = "Online";
            foreach (var item in arrayDeleted)
            {
                var tempCondion = true;

                if (item.Code == "0")
                {
                    tempCondion = false;
                }
                if (tempCondion == code)
                {
                    temp = item.Name;
                    break;
                }
            }
            return temp;
        }

        public string GetNameStatus(int code)
        {
            if (code < 1)
            {
                return "Không ho?t d?ng";
            }
            return "Ho?t d?ng";


        }

        public string PhoneToXXX(string phone)
        {
            if (string.IsNullOrEmpty(phone))
            {
                return "";
            }
            var text = phone.Remove(0, 7);
            var textPhone = "xxxxxxx" + text;
            return textPhone;
        }
        public UserDataView UserData
        {
            get; set;
        }

        public BaseModel2()
        {
            UserData = new UserDataView()
            {
                UserId = -1
            };
            Permision = new PermisionUser();

            GlobalData = GlobalVar.GlobalData;
            UserDataGlobal = UserActive.DataActiveOnline;
        }


        private List<VS.Human.Rep.Model.PermissionConfigViewModel> _userPermissions;
        public List<VS.Human.Rep.Model.PermissionConfigViewModel> UserPermissions 
        { 
            get 
            {
                if (_userPermissions == null || !_userPermissions.Any())
                {
                    LoadAllPermissions();
                }
                return _userPermissions ?? new List<VS.Human.Rep.Model.PermissionConfigViewModel>();
            }
            set => _userPermissions = value;
        }

        public bool HasViewPermission(string pageCode)
        {
            if (UserData?.RoleCode == "1") return true; 
            return UserPermissions.Any(p => p.PageCode == pageCode && p.IsView);
        }

        public bool HasApprovePermission(string pageCode)
        {
            if (UserData?.RoleCode == "1") return true;
            return UserPermissions.Any(p => p.PageCode == pageCode && p.IsApprove);
        }

        private void LoadAllPermissions()
        {
            var identity = HttpContext?.User?.Identity as ClaimsIdentity;
            if (identity != null)
            {
                var roleCode = identity.Claims.FirstOrDefault(o => o.Type == "RoleCode")?.Value;
                Console.WriteLine($"[DEBUG Sidebar] HttpContext RoleCode: '{roleCode}'");
                if (!string.IsNullOrEmpty(roleCode))
                {
                    var permissionBusiness = HttpContext.RequestServices.GetService(typeof(VS.Human.Business.IPermissionBusiness)) as VS.Human.Business.IPermissionBusiness;
                    if (permissionBusiness != null)
                    {
                        _userPermissions = permissionBusiness.GetPermissionsByRoleSync(roleCode);
                        Console.WriteLine($"[DEBUG Sidebar] Loaded {_userPermissions?.Count ?? 0} permissions for role {roleCode}");
                        foreach (var p in _userPermissions ?? new List<VS.Human.Rep.Model.PermissionConfigViewModel>())
                        {
                            if (p.IsView) Console.WriteLine($"[DEBUG Sidebar] Page: {p.PageCode}, IsView: {p.IsView}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("[DEBUG Sidebar] IPermissionBusiness is NULL");
                    }
                }
            }
            else
            {
                Console.WriteLine("[DEBUG Sidebar] Identity is NULL");
            }
        }

        public void GetInfoUser()
        {
            var identity = HttpContext.User.Identity as ClaimsIdentity;

            if (identity != null)
            {
                var userClaims = identity.Claims;
                var idUser = identity.Claims.FirstOrDefault(o => o.Type == "userId")?.Value;
                var userName = userClaims.FirstOrDefault(o => o.Type == "UserName")?.Value;
                var roleCode = userClaims.FirstOrDefault(o => o.Type == "RoleCode")?.Value;
                var fullName = userClaims.FirstOrDefault(o => o.Type == "FullName")?.Value;
                var lineCode = userClaims.FirstOrDefault(o => o.Type == "LineCode")?.Value;
                if (UserData == null)
                {
                    UserData = new UserDataView();
                }
                UserData.UserName = userName;
                UserData.FullName = fullName;
                if (!string.IsNullOrEmpty(idUser)) UserData.UserId = int.Parse(idUser);
                UserData.RoleCode = roleCode;
                UserData.LineCode = lineCode;

                if (!string.IsNullOrEmpty(idUser))
                    UserDataGlobal.AddOrUpdate(idUser, userName, fullName);

                // Initialize permissions
                if (_userPermissions == null) LoadAllPermissions();

                if (!string.IsNullOrEmpty(KeyPage) && _userPermissions != null)
                {
                    var pagePerm = _userPermissions.FirstOrDefault(p => p.PageCode == KeyPage);
                    if (pagePerm != null)
                    {
                        Permision.View = pagePerm.IsView;
                        Permision.Add = pagePerm.IsAdd;
                        Permision.Edit = pagePerm.IsEdit;
                        Permision.Delete = pagePerm.IsDelete;
                        Permision.Approve = pagePerm.IsApprove; 
                    }
                }
            }
        }



    }
}

