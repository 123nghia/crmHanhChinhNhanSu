namespace VS.Human.Rep
{
    public interface IGlobalDataRep
    {
        Task<int> GetCountSource();
        Task<int> GetCountSourceCTV();

    }
}
