using System.Linq.Expressions;

namespace QueryRulesEngine.Persistence
{
    public interface IReadOnlyRepositoryAsync<TId>
    {
        Task<List<TResult>> FindAllByPredicateAndTransformAsync<TEntity, TResult>(
            Expression<Func<TEntity, bool>> predicate,
            Expression<Func<TEntity, TResult>> selector,
            CancellationToken cancellationToken = default) where TEntity : class;

        Task<List<TResult>> FindAllByPredicateAndTransformDistinctAsync<TEntity, TResult>(
            Expression<Func<TEntity, bool>> predicate,
            Expression<Func<TEntity, TResult>> selector,
            CancellationToken cancellationToken = default) where TEntity : class;

        Task<List<TResult>> FindAllByPredicateAsNoTrackingAndTransformAsync<TEntity, TResult>(
            Expression<Func<TEntity, bool>> predicate,
            Expression<Func<TEntity, TResult>> selector,
            CancellationToken cancellationToken = default) where TEntity : class;

        Task<TResult?> FindByPredicateAndTransformAsync<TEntity, TResult>(
            Expression<Func<TEntity, bool>> predicate,
            Expression<Func<TEntity, TResult>> selector,
            CancellationToken cancellationToken = default) where TEntity : class;

        Task<List<TEntity>?> FindAllByPredicateAsNoTrackingAsync<TEntity>(
            Expression<Func<TEntity, bool>> predicate,
            CancellationToken cancellationToken = default) where TEntity : class;

        Task<List<TEntity>?> FindAllByPredicateAsync<TEntity>(
            Expression<Func<TEntity, bool>> predicate,
            CancellationToken cancellationToken = default) where TEntity : class;

        Task<List<TEntity>> FindAllByPredicateIncludeAsync<TEntity>(
            Expression<Func<TEntity, bool>> predicate,
            Expression<Func<TEntity, object>>[] includes = null,
            CancellationToken cancellationToken = default) where TEntity : class;

        Task<TEntity?> FindByPredicateAsNoTrackingAsync<TEntity>(
            Expression<Func<TEntity, bool>> predicate,
            CancellationToken cancellationToken = default) where TEntity : class;

        Task<TEntity?> FindByPredicateAsync<TEntity>(
            Expression<Func<TEntity, bool>> predicate,
            CancellationToken cancellationToken = default) where TEntity : class;

        Task<TEntity> FindByPredicateIncludeAsync<TEntity>(
            Expression<Func<TEntity, bool>> predicate,
            Expression<Func<TEntity, object>>[] includes = null,
            CancellationToken cancellationToken = default) where TEntity : class;

        Task<TEntity?> GetByIdAsync<TEntity>(
            TId id,
            CancellationToken cancellationToken = default) where TEntity : class;

        Task<List<TEntity>> GetPagedResponseAsync<TEntity>(
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default) where TEntity : class;

        Task<List<TEntity>> GetAllAsNoTrackingAsync<TEntity>(
            CancellationToken cancellationToken = default) where TEntity : class;

        Task<List<TEntity>> GetAllAsync<TEntity>(
            CancellationToken cancellationToken = default) where TEntity : class;
    }
}
