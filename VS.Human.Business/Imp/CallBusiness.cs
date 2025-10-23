using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using VS.Human.Rep;

namespace VS.Human.Business.Imp
{
    public class CallBusiness : BaseBusiness, ICallBussiness
    {
        public CallBusiness(IUnitOfWork unitOfWork,
            IHttpContextAccessor httpContextAccessor)
            : base(unitOfWork, httpContextAccessor)
        {

        }

        public async Task<bool> MakeCall(string phone, string type, int? IdRel, string chanel, int userId)
        {
            var data = new StringContent(JsonConvert.SerializeObject(new
            {
                phoneNumber = phone,
                userId,
                lineCode = chanel
            }));
            data.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            var linkUrl = "http://192.168.1.9:3002";
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(linkUrl);
                var reponse = await client.PostAsync("api/client/makeCall", data);
                var result = await reponse.Content.ReadAsStringAsync();
            }
            return true;
        }
    }
}
