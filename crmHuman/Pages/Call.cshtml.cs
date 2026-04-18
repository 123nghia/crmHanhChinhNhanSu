using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VS.Human.Business;
using VS.Human.Business.Model;
using VS.Human.Rep.Model;

namespace crmHuman.Pages
{
    [Authorize]
    public class CallModel : BaseModel2
    {
        private readonly ILogger<CallModel> _logger;
        private readonly ICallBussiness _empBusiness;
        private readonly IEmpBusiness _employeeBusiness;
        public CallModel(ILogger<CallModel> logger,
            ICallBussiness empBusiness,
            IEmpBusiness employeeBusiness

            )
        {
            _logger = logger;
            _empBusiness = empBusiness;
            _employeeBusiness = employeeBusiness;




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
            var currentEmployee = await _employeeBusiness.GetById(UserData.UserId);
            var currentLineCode = currentEmployee?.LineCode?.Trim();
            if (string.IsNullOrEmpty(currentLineCode))
            {
                listEror.Add(new
                {
                    name = "LineCode",
                    Content = "Nhan vien chua duoc gan line SIP."
                });
                return new JsonResult(listEror)
                {
                    StatusCode = StatusCodes.Status400BadRequest
                };
            }
            var result = await _empBusiness.MakeCallDetailed(
                request.Phonecall,
                request.Typecall,
                request.Idrel,
                currentLineCode,
                UserData.UserId,
                Request.Headers["Referer"].ToString());

            var dataReponse = new
            {
                success = result.Success,
                callLogId = result.CallLogId,
                providerCallId = result.ProviderCallId,
                message = result.Success
                    ? "Da kich hoat client goi."
                    : (result.Error ?? "Khong the kich hoat client goi. Kiem tra cau hinh SIP va dich vu quay so.")
            };
            return new JsonResult(dataReponse)
            {
                StatusCode = StatusCodes.Status200OK

            };
        }

        public async Task<IActionResult> OnPostOutcome([FromBody] CallLogOutcomeUpdate request)
        {
            GetInfoUser();
            if (request == null || request.Id <= 0)
            {
                return new JsonResult(new { success = false, message = "Missing call log." })
                {
                    StatusCode = StatusCodes.Status400BadRequest
                };
            }

            var ok = await _empBusiness.UpdateCallOutcome(request, UserData.UserId);
            return new JsonResult(new { success = ok });
        }







    }
}
