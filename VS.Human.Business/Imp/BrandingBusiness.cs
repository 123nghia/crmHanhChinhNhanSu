using Microsoft.AspNetCore.Http;
using VS.Human.Rep;
using VS.Human.Rep.Model;

namespace VS.Human.Business.Imp
{
    public class BrandingBusiness : BaseBusiness, IBrandingBusiness
    {
        public BrandingBusiness(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor)
            : base(unitOfWork, httpContextAccessor)
        {
        }

        public async Task<SystemBranding?> GetActiveAsync()
        {
            try
            {
                return await _unitOfWork.SystemBrandingRep.GetActiveAsync();
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> SaveLogoAsync(string logoPath, int updatedBy)
        {
            if (string.IsNullOrWhiteSpace(logoPath))
            {
                return false;
            }

            try
            {
                var branding = new SystemBranding
                {
                    LogoPath = logoPath,
                    CreatedBy = updatedBy,
                    UpdatedBy = updatedBy
                };
                return await _unitOfWork.SystemBrandingRep.SaveAsync(branding);
            }
            catch
            {
                return false;
            }
        }
    }
}
