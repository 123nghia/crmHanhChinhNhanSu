namespace VS.Human.Business.Imp
{
    public interface IHandleReportBussiness
    {
        Task<int> CalTalkingTime(DateTime? dateGet = null);
        Task<int> CalTalkingTimeAutoBusiness(DateTime? dateGet);
        Task<bool> HandleData();

    }
}
