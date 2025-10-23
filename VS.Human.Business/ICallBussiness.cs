namespace VS.Human.Business
{
    public interface ICallBussiness
    {

        Task<bool> MakeCall(string phone, string type, int? IdRel, string chanel, int userId);

    }

}
