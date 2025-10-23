namespace VS.Human.Business
{
    public interface IGlobalDataBusiness
    {
        Task<int> GetCountSource();
        Task<int> GetCountSourceCTV();
    }
}
