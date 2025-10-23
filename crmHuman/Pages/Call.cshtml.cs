using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VS.Human.Business;
using VS.Human.Business.Model;

namespace crmHuman.Pages
{
    [Authorize]
    public class CallModel : BaseModel2
    {
        private readonly ILogger<CallModel> _logger;
        private readonly ICallBussiness _empBusiness;
        public CallModel(ILogger<CallModel> logger,
            ICallBussiness empBusiness

            )
        {
            _logger = logger;
            _empBusiness = empBusiness;




        }

        public async Task<IActionResult> OnPostMakeCall(MakeCallAdd request)
        {

            var listEror = new List<object>();
            if (string.IsNullOrEmpty(request.Typecall))
            {
                var itemError = new
                {
                    name = "TypeCall",
                    Content = "Thiếu đối tượng gọi"
                };
                listEror.Add(itemError);
            }
            if (request.Idrel < 1)
            {
                var itemError = new
                {
                    name = "",
                    Content = "Thiếu đối tượng gọi"
                };
                listEror.Add(itemError);

            }



            if (listEror.Count > 0)
            {
                return new JsonResult(listEror)
                {
                    StatusCode = StatusCodes.Status400BadRequest
                };
            }
            GetInfoUser();
            if (string.IsNullOrEmpty(UserData.LineCode))
            {
                return new JsonResult(listEror)
                {
                    StatusCode = StatusCodes.Status400BadRequest
                };
            }
            var result = await _empBusiness.MakeCall(request.Phonecall, request.Typecall, request.Idrel, UserData.LineCode, UserData.UserId);

            var dataReponse = new
            {
                success = result,

            };
            return new JsonResult(dataReponse)
            {
                StatusCode = StatusCodes.Status200OK

            };
        }







    }
}
