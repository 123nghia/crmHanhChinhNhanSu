using System.Globalization;

namespace VS.Human.Item
{
    public class EmployeeIndexModel : BaseIndexModel
    {
        public string? UserName { get; set; }
        public string? FullName { get; set; }
        public string? Phone { get; set; }
        public string? Noted { get; set; }
        public string? RoleCode { get; set; }
        public string? Pass { get; set; }
        public DateTime? Dob { get; set; }
        public int Status { get; set; }
        public int IsActive { get; set; }
        public string? AvatarFile { get; set; }
        public DateTime? Onboard { get; set; }

        public string? GroupName { get; set; }

        public string? LineCode { get; set; }
        public string TypeAccount { get; set; }
        public string DepartmentCode { get; set; }
        public string DocumentStatus { get; set; }
        public string PositionCode { get; set; }
        public string StatusText { get; set; }

        public string? TypeAccountText { get; set; }
        public string? DepartmentText { get; set; }
        public string? PositionText { get; set; }
        public string? DocumentStatusText { get; set; }
        public string? ReligionText { get; set; }
        public string? EducationLevelText { get; set; }
        public string? MaritalstatusText { get; set; }

        public string OnboardDateDisplay
        {
            get
            {
                if (Onboard.HasValue)
                {
                    return Onboard.Value.ToString("yyyy-MM-dd",  CultureInfo.InvariantCulture);
                }
                return string.Empty;
            }
        }
    }

    public class GroupIndexModel : BaseIndexModel
    {

        public string? Code { get; set; }
        public string? Name { get; set; }
        public int ManagerId { get; set; }
        public int Status { get; set; }
        public int IsActive { get; set; }
        public string? Noted { get; set; }

        public string ManagerName { get; set; }
    }

    public class CommonIndexModel : BaseIndexModel
    {

        public string? ShortName { get; set; }

        public string? Code { get; set; }
        public string? Name { get; set; }

        public int IsActive { get; set; }

        public string? Extra { get; set; }

        public int TypeData { get; set; }

        public int ApplyFor { get; set; }


    }


    public class RegionalIndexModel : BaseIndexModel
    {
        public string? Code { get; set; }
        public string? Name { get; set; }
    }

    public class ParrentChildIndexModel : BaseIndexModel
    {
        public string? Text { get; set; }
        public int Id { get; set; }

    }
    public class GroupMemberIndexModel : BaseIndexModel
    {

        public string? GroupId { get; set; }
        public string? MemberId { get; set; }
        public int Deleted { get; set; }

        public string? MemberName { get; set; }

    }


    public class JobIndexModel : BaseIndexModel
    {

        public string? Code { get; set; }
        public string? Name { get; set; }
        public int? Field { get; set; }
        public int? CareerId { get; set; }
        public string? Content { get; set; }
        public string? ShortDes { get; set; }
        public int? IsActive { get; set; }
        public string? Noted
        {
            get; set;
        }
        public int? Status { get; set; }

        public string CareerIdText { get; set; }
        public string FieldText { get; set; }

        public int? WarrantyDate { get; set; }

    }
    public class ManagerLeadIndex : BaseIndexModel
    {
        public string UserName { get; set; }

        public string FullName { get; set; }

        public int Id { get; set; }
    }


    public class ScheduleInterviewIndexModel : BaseIndexModel
    {

        public int? RelId { get; set; }

        public string? RelCode { get; set; }
        public int? Type { get; set; }

        public DateTime? ScheduleDate { get; set; }

        public string? AddressInfo { get; set; }

        public string? Noted { get; set; }

        public string ScheduleDateDisplay
        {
            get
            {
                if (ScheduleDate == null || !ScheduleDate.HasValue)
                {
                    return string.Empty;
                }
                return ScheduleDate.Value.ToString("dd/MM/yyyy");
            }
        }

    }


    public class DocumentDataIndexModel : BaseIndexModel
    {
        public int? RelId { get; set; }
        public string? RelCode { get; set; }
        public int? Type { get; set; }
        public string? Code { get; set; }
        public string? DisplayText { get; set; }
        public string? ValueFile { get; set; }
        public int? ParentId { get; set; }
        public bool IsFolder { get; set; }
        public int AccessLevel { get; set; }
        public string? AuthorName { get; set; }
    }

    public class CandidateIndexModel : BaseIndexModel
    {
        public string? Code { get; set; }
        public string? Name { get; set; }
        public DateTime? Dob { get; set; }
        public string? Email { get; set; }
        public string AvatarLink { get; set; }
        public string CVLink { get; set; }
        public string? ShortDes { get; set; }
        public int? IsActive { get; set; }
        public string? Phone { get; set; }
        public string? Source { get; set; }
        public string? Noted
        {
            get; set;
        }

        public int? Status { get; set; }
        public int? StatusHuman { get; set; }
        public int? ManagerId { get; set; }
        public string? ManagerName { get; set; }

        public string? SourceName { get; set; }
        public int? Position { get; set; }
        public int? DepartmentId { get; set; }

        public string DepartmentName { get; set; }

        public string PostionName { get; set; }

        public string DocumentStatusName { get; set; }

        public string DocumentStatusText
        {

            get
            {
                return "Đang bổ sung";
            }
        }

        public string StatusName
        {
            get; set;
        }

    }


    public class CountCVIndexModel : BaseIndexModel
    {
        public int? Total { get; set; }
        public int? Assignee { get; set; }
        public string? UserName { get; set; }
        public DateTime? DateCreate { get; set; }
    }


    public class CandidateOfMemberIndexModel : BaseIndexModel
    {

        public string? Name { get; set; }

        public string? Phone { get; set; }

        public string? Noted
        {
            get; set;
        }
        public int? Status { get; set; }

    }
    public class OnboardMemberIndexModel : BaseIndexModel
    {

        public int OrderId { get; set; }
        public string? Code { get; set; }
        public int? JobId { get; set; }
        public int? Status { get; set; }
        public int? SystemStatus { get; set; }
        public int? CandidateId { get; set; }
        public int? PartnerId { get; set; }
        public int? ProjectId { get; set; }
        public string? OrderCode { get; set; }
        public int? Source { get; set; }
        public string? CVLink { get; set; }
        public string? ShortDes { get; set; }
        public DateTime? OnboardDate { get; set; }
        public int? Assignee { get; set; }
        public DateTime? Warrantydate { get; set; }
        public int? Dpd { get; set; }
        public string? PositionText { get; set; }

        public string? UserNameText { get; set; }

        public string? CandidateName { get; set; }

        public string? JobName
        {
            get; set;
        }

        public string? AssigeeName { get; set; }
        public string? StatusText { get; set; }
        public string? ExtraColor { get; set; }

        public string? PartnerName { get; set; }

        public string? ProjectName { get; set; }

        public string? SystemStatusText { get; set; }

        public int? Result { get; set; }

        public string ResultText
        {
            get
            {
                if (Result == null || Result == 0)
                {
                    return "Trong bảo hành";
                }

                if (Result == 1)
                {
                    return "Pass";
                }
                else
                {
                    return "Failed";
                }
            }
        }

        public string? Noted
        {
            get; set;
        }
    }

    public class MemberOnboardIndexModel : OrderIndexModel
    {
        public int? SystemStatus { get; set; }
        public int? PartnerId { get; set; }
        public string? OrderCode { get; set; }
        public DateTime? OnboardDate { get; set; }
        public DateTime? Warrantydate { get; set; }
        public string? SystemStatusText { get; set; }
        public int OrderId { get; set; }
        public int? Dpd { get; set; }

    }
    public class OrderIndexModel : BaseIndexModel
    {

        public string? CandidateFullName { get; set; }
        public string? Code { get; set; }
        public string? Status { get; set; }
        public int? CandidateId { get; set; }
        public int? JobId { get; set; }
        public string? CVLink { get; set; }
        public string? ShortDes { get; set; }

        public string? ExtraColor { get; set; }

        public string? PositionText { get; set; }

        public string? UserNameText { get; set; }

        public string? AssigeeName { get; set; }
        public string? StatusText { get; set; }
        public string? Source { get; set; }
        public int? ProjectId { get; set; }

        public string? PartnerName { get; set; }
        public string? ProjectName { get; set; }
        public string? Noted
        {
            get; set;
        }
        public DateTime? DateGet { get; set; }


        public int? Assignee { get; set; }

        public int? IsClose { get; set; }
        public int? Result { get; set; }

        public int? Isapply { get; set; }

        public string? SourceName { get; set; }

        public string Regional { get; set; }
        public string SchoolName { get; set; }
        public int RankLevel { get; set; }
        public int Gender { get; set; }
        public string Introduction { get; set; }
        public int Experience { get; set; }
    }


    public class OrderInfoIndexModel : OrderIndexModel
    {

        public int? Isapply { get; set; }

        public int? Ispush { get; set; }
        public string StatusName
        {
            get; set;
        }

        public int? LinhvucId { get; set; }


        public int? PartnerId { get; set; }

        public int? ApplyFor { get; set; }

    }


    public class OrderStatusIndexModel : BaseIndexModel
    {
        public string? Total { get; set; }

        public string? Name { get; set; }
        public int? Id { get; set; }




    }
    public class OrderImpactReportIndexModel : BaseIndexModel
    {
        public string? OrderCode { get; set; }

        public string? ObjectInfo { get; set; }
        public int? OldStatus { get; set; }
        public int? NewStatus { get; set; }

        public string? StatusName { get; set; }
        public string? Noted
        {
            get; set;
        }

        public string Content
        {
            get
            {

                var temp = "";
                if (string.IsNullOrEmpty(TxtTimer))
                {
                    temp += TxtTimer;
                }

                if (string.IsNullOrEmpty(TxtPlace))
                {
                    temp += TxtPlace;
                }

                return temp;
            }
        }

        public string? TxtTimer { get; set; }
        public DateTime? DateFrom { get; set; }

        public string? TxtPlace { get; set; }

        public string? AuthorName { get; set; }

        public string? UpdatedByName { get; set; }




    }
    public class OrderImpactIndexModel : BaseIndexModel
    {
        public string? OrderCode { get; set; }

        public string? ObjectInfo { get; set; }
        public int? OldStatus { get; set; }
        public int? NewStatus { get; set; }

        public string? StatusName { get; set; }
        public string? Noted
        {
            get; set;
        }

        public string? TxtTimer { get; set; }
        public DateTime? DateFrom { get; set; }

        public string? TxtPlace { get; set; }

        public string? CreatedByName { get; set; }

        public string? UpdatedByName { get; set; }




    }


    public class OrderLastestIndexModel : BaseIndexModel
    {

        public string? Code { get; set; }
        public string OrderCode { get; set; }

        public string? Status { get; set; }
        public string? CandidateName { get; set; }
        public int? JobId { get; set; }
        public string? JobName { get; set; }

        public string? UserNameText { get; set; }
        public string? StatusText { get; set; }
        public string? Noted
        {
            get; set;
        }
    }

    public class OrderImpactDashboardIndexModel : BaseIndexModel
    {

        public string? OrderCode { get; set; }

        public string? ObjectInfo { get; set; }
        public int? OldStatus { get; set; }
        public int? NewStatus { get; set; }

        public string? StatusName { get; set; }
        public string? Noted
        {
            get; set;
        }

        public string? TxtTimer { get; set; }
        public DateTime? DateFrom { get; set; }

        public string? TxtPlace { get; set; }

        public string? CreatedByName { get; set; }

        public string? UpdatedByName { get; set; }
    }

    public class ParamDashboard : BaseIndexModel
    {

        public int? CountOrderd { get; set; }

        public int? CountCandidate { get; set; }
        public ParamDashboard()
        {
            CountCandidate = 0;
            CountOrderd = 0;
        }
    }


    public class OrderMarkettingDashboard : BaseIndexModel
    {
        public string? Code { get; set; }
        public string? Status { get; set; }
        public int? CandidateId { get; set; }
        public int? JobId { get; set; }
        public string? CVLink { get; set; }
        public string? ShortDes { get; set; }

        public string? PositionText { get; set; }

        public string? UserNameText { get; set; }

        public string? AssigeeName { get; set; }
        public string? StatusText { get; set; }
        public string? Source { get; set; }
        public int? ProjectId { get; set; }

        public string? PartnerName { get; set; }
        public string? ProjectName { get; set; }
        public string? Noted
        {
            get; set;
        }
        public DateTime? DateGet { get; set; }


        public int? Assignee { get; set; }

        public int? Result { get; set; }
    }


    public class StatusDashboardReport : BaseIndexModel
    {

        public double? Total { get; set; }

        public string? Name { get; set; }


        public int? Id { get; set; }

    }

    public class OrderStatusDashboard : BaseIndexModel
    {
        public int? Total { get; set; }
        public int? StatusiD { get; set; }
        public string? StatusName { get; set; }


        public OrderStatusDashboard()
        {
            Total = 0;

        }
    }

    public class GlobaleDataIndex : BaseIndexModel
    {
        public int Total { get; set; }

    }



}


