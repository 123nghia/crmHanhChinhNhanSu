using VS.Human.Rep.Model;

namespace VS.Human.Business
{
    public interface IBrandingBusiness
    {
        Task<SystemBranding?> GetActiveAsync();
        Task<bool> SaveLogoAsync(string logoPath, int updatedBy);
    }
}
