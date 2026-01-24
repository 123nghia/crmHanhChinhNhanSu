using VS.Human.Rep.Model;

namespace VS.Human.Rep
{
    public interface ISystemBrandingRep
    {
        Task<SystemBranding?> GetActiveAsync();
        Task<bool> SaveAsync(SystemBranding branding);
    }
}
