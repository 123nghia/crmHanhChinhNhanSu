using Microsoft.AspNetCore.Http;
using VS.Human.Rep.Model;

namespace VS.Human.Business.Model
{

    public class PasswordAdd
    {
        public int? Id { get; set; }
        public bool? ResetPass { get; set; }
        public string? NewPassword { get; set; }


    }
    public class EmployeeAdd : Employee
    {

    }

    public class EmployeeInfoAdd : Employee
    {

        public string StatusWork { get; set; }

    }

    public class UploadFileAdd
    {
        public IFormFile FileRequest { get; set; }

        public string? Type { get; set; }
    }


    public class ImportSourceFileAdd
    {
        public IFormFile FileRequest { get; set; }


    }

    public class ConvertToEmployeeAdd
    {
        public int? Id { get; set; }
    }

    public class CheckDupAdd
    {
        public string Phone { get; set; }
        public string Email { get; set; }
    }
    public class PartnerAdd : Partner
    {
        public List<ParrentChildAdd> AddressList { get; set; }

        public List<ParrentChildAdd> ProjectList { get; set; }
        public PartnerAdd()
        {

        }
    }

    public class ParrentChildAdd
    {
        public string? Text { get; set; }
        public string? Type { get; set; }

        public string? RelId { get; set; }

        public int? Id { get; set; }


    }

    public class MasterDataAdd : MasterData
    {

        public MasterDataAdd()
        {
            ApplyFor = 2;
        }
    }


    public class ScheduleInterviewAdd : ScheduleInterview
    {

        public ScheduleInterviewAdd()
        {

        }
    }



    public class DocumentDataAdd : DocumentData
    {

        public DocumentDataAdd()
        {

        }
    }

    public class MasterData2Add : MasterData
    {

        public MasterData2Add()
        {
            ApplyFor = 2;
        }
    }

    public class JobItemAdd : JobItem
    {
        public JobItemAdd()
        {

        }
    }


    public class MakeCallAdd
    {
        public string Typecall { get; set; }
        public int Idrel { get; set; }
        public string Phonecall { get; set; }
        public MakeCallAdd()
        {
            Idrel = 0;
            Typecall = "";
            Phonecall = "";
        }
    }
    public class CandidateAdd : Candidate
    {
        public int ManagerId { get; set; }
        public CandidateAdd()
        {

        }
    }


    public class DocumentInfoUpdate : Employee
    {

        public int EmployeeId { get; set; }



        public DocumentInfoUpdate()
        {

        }
    }


    public class CandidateDetailUpdate : Candidate
    {

        public int CandidateId { get; set; }

        public int ManagerId { get; set; }

        public CandidateDetailUpdate()
        {

        }
    }

    public class CandidateProfileUpdate
    {
        public int CandidateId { get; set; }
        public string? Name { get; set; }
        public DateTime? Dob { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? CVLink { get; set; }
        public string? NationalId { get; set; }
        public string? Address { get; set; }
    }



    public class EmployeeDetailUpdate : Employee
    {

        public int CandidateId { get; set; }

        public EmployeeDetailUpdate()
        {

        }
    }


    public class DocumentDataAddRequest
    {

        public int RelId { get; set; }
        public int DataType { get; set; }
        public int UserId { get; set; }

        public List<DocumentDataItem> Data { get; set; }

        public DocumentDataAddRequest()
        {
            Data = new List<DocumentDataItem>();

        }
    }

    public class DocumentDataItem
    {

        public string Code { get; set; }

        public string DisplayText { get; set; }

        public string ValueFile { get; set; }
        public int Id { get; set; }
    }


    public class CandidateScheduleAdd : ScheduleInterview
    {


        public CandidateScheduleAdd()
        {

        }
    }
    public class CandidateOrderAdd
    {
        public CandidateOrderAdd()
        {

        }
        public int? JobId { get; set; }
        public string? ShortDes { get; set; }
        public string? CVLink { get; set; }

        public string? NotedCan
        {
            get; set;
        }

        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Name { get; set; }
        public DateTime? Dob { get; set; }

        public string? ShortDesOrder { get; set; }
        public int? IsActive { get; set; }

        public int? StatusAplly { get; set; }
        public int? CreateBy { get; set; }

        public int? CandidateId { get; set; }

    }
    public class OrderItemAdd : Order
    {
        public string? PhoneNumber { get; set; }
        public OrderItemAdd()
        {

        }
    }

    public class OrderInfoItemAdd
    {
        public string? PhoneNumber { get; set; }
        public string? FulName { get; set; }
        public string? Email { get; set; }
        public DateTime? Dob { get; set; }
        public int? RequestId { get; set; }

        public int? JobId { get; set; }
        public string? ShortDes { get; set; }

        public string? CVLink { get; set; }

        public string? Noted { get; set; }

        public int? CreatedBy { get; set; }

        public int? Source { get; set; }

        public int? Assignee { get; set; }

        public int? Isapply { get; set; }

        public string Regional { get; set; }
        public string SchoolName { get; set; }
        public int Experience { get; set; }
        public int RankLevel { get; set; }
        public int Gender { get; set; }
        public string Introduction { get; set; }
        public OrderInfoItemAdd()
        {
            Isapply = 0;
        }
    }


    public class ApplyToStatus
    {


        public int? OrderId { get; set; }
        public ApplyToStatus()
        {

        }
    }



    public class OrderCandidateMarkettingItemAdd
    {
        public string? PhoneNumber { get; set; }

        public string? Email { get; set; }

        public DateTime? Dob { get; set; }
        public string? Name { get; set; }
        public string? NotedCan { get; set; }

        public string? CVLink { get; set; }

        public int? JobId { get; set; }
        public string? Document { get; set; }

        public string? NotedOrder { get; set; }

        public int? Source { get; set; }


        public int? Status { get; set; }

        public int? Id { get; set; }

        public bool SaveOrder { get; set; }

        public string Regional { get; set; }
        public OrderCandidateMarkettingItemAdd()
        {

        }

    }

    public class OrderAssingeeAdd
    {
        public int? CreatedBy { get; set; }
        public int? Id { get; set; }

        public string? Assignee { get; set; }
        public OrderAssingeeAdd()
        {

        }
    }
    public class OrderResultRequest
    {
        public int? CreatedBy { get; set; }
        public int? Id { get; set; }

        public int? Result { get; set; }

    }

    public class OrderImpactHIstoryAdd : OrderImpactHIstory
    {
        public int? PartnerId { get; set; }
        public bool? RadioOtherAdress { get; set; }
        public OrderImpactHIstoryAdd()
        {

        }
    }


    public class OnboardMemberUpdate
    {
        public DateTime? DateOnboard { get; set; }
        public int? Result { get; set; }

        public string Note { get; set; }

        public int? OrderId { get; set; }
        public OnboardMemberUpdate()
        {

        }

    }
}
