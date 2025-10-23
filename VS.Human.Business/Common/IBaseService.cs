using VS.Human.Business.Common;

namespace VS.Human.Business
{
    /// <summary>
    /// Base interface for all business services
    /// </summary>
    public interface IBaseService
    {
        // Common service methods can be added here
    }

    /// <summary>
    /// Generic CRUD service interface
    /// </summary>
    public interface ICrudService<TEntity, TAddRequest, TUpdateRequest, TId> : IBaseService
        where TEntity : class
        where TAddRequest : class
        where TUpdateRequest : class
    {
        Task<Result<TEntity>> GetByIdAsync(TId id);
        Task<Result> AddAsync(TAddRequest request);
        Task<Result> UpdateAsync(TUpdateRequest request);
        Task<Result> DeleteAsync(TId id);
    }
}

