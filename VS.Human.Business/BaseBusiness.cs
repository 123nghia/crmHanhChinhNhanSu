
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using VS.Human.Rep;

namespace VS.Human.Business.Imp
{
    public class BaseBusiness
    {
        protected readonly ILogger<LoginBusiness> _logger;

        protected readonly IUnitOfWork _unitOfWork;
        string start = "KPMG_EV";
        string end = "KPMG_PM";
        public int UserId { get; set; }
        private readonly IHttpContextAccessor _contextAccessor;

        public BaseBusiness(
            IUnitOfWork unitOfWork,
            IHttpContextAccessor contextAccessor
            )
        {

            _unitOfWork = unitOfWork;
            _contextAccessor = contextAccessor;
        }

        public static void SetUserId(int userset)
        {

        }
        private byte[] encryptData(string data)
        {
            System.Security.Cryptography.MD5CryptoServiceProvider md5Hasher =
                new System.Security.Cryptography.MD5CryptoServiceProvider();
            byte[] hashedBytes;
            System.Text.UTF8Encoding encoder = new System.Text.UTF8Encoding();
            hashedBytes = md5Hasher.ComputeHash(encoder.GetBytes(data));
            return hashedBytes;
        }
        public string getMD5(string data)
        {
            return BitConverter.ToString(encryptData(start + data + end)).Replace("-", "").ToLower();
        }

        public int GetUserId()
        {
            var identity = _contextAccessor.HttpContext.User.Identity as ClaimsIdentity;
            if (identity != null)
            {
                var userClaims = identity.Claims;
                var idUser = identity.Claims.FirstOrDefault(o => o.Type == "userId")?.Value;
                var userName = userClaims.FirstOrDefault(o => o.Type == "UserName")?.Value;
                var roleCode = userClaims.FirstOrDefault(o => o.Type == "RoleCode")?.Value;
                var fullName = userClaims.FirstOrDefault(o => o.Type == "FullName")?.Value;
                return int.Parse(idUser);
            }
            return -1;

        }


    }
}
