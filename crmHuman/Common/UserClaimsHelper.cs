using System.Security.Claims;
using crmHuman.Model;

namespace crmHuman.Common
{
    /// <summary>
    /// Helper class for extracting user claims
    /// Refactored from duplicate code in BaseModel, BaseModel2, Index, IndexAdmin
    /// </summary>
    public static class UserClaimsHelper
    {
        public static UserDataView? ExtractUserData(ClaimsIdentity? identity)
        {
            if (identity == null)
                return null;

            var userClaims = identity.Claims;
            var idUser = identity.Claims.FirstOrDefault(o => o.Type == "userId")?.Value;
            var userName = userClaims.FirstOrDefault(o => o.Type == "UserName")?.Value;
            var roleCode = userClaims.FirstOrDefault(o => o.Type == "RoleCode")?.Value;
            var fullName = userClaims.FirstOrDefault(o => o.Type == "FullName")?.Value;
            var lineCode = userClaims.FirstOrDefault(o => o.Type == "LineCode")?.Value;

            if (string.IsNullOrEmpty(idUser))
                return null;

            return new UserDataView
            {
                UserId = int.TryParse(idUser, out var id) ? id : -1,
                UserName = userName ?? string.Empty,
                FullName = fullName ?? string.Empty,
                RoleCode = roleCode ?? string.Empty,
                LineCode = lineCode ?? string.Empty
            };
        }

        public static void UpdateUserActiveStatus(ClaimsIdentity? identity)
        {
            if (identity == null)
                return;

            var idUser = identity.Claims.FirstOrDefault(o => o.Type == "userId")?.Value;
            var userName = identity.Claims.FirstOrDefault(o => o.Type == "UserName")?.Value;
            var fullName = identity.Claims.FirstOrDefault(o => o.Type == "FullName")?.Value;

            if (!string.IsNullOrEmpty(idUser) && !string.IsNullOrEmpty(userName) && !string.IsNullOrEmpty(fullName))
            {
                UserActive.DataActiveOnline.AddOrUpdate(idUser, userName, fullName);
            }
        }
    }
}

