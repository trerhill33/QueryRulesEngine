namespace QueryRulesEngine.Persistence
{

    public interface IWriteRepositoryAsync<TEntity, TId>
        where TEntity : AuditableEntity<TId>
    {
        Task<TEntity> AddAsync(
        TEntity entity,
        CancellationToken cancellationToken = default);
        Task<List<TEntity>> AddRangeAsync(
            List<TEntity> entities,
            CancellationToken cancellationToken = default);
        Task DeleteAsync(TEntity entity);
        Task DeleteByIdAsync(
            TId id,
            CancellationToken cancellationToken = default);
        void DeleteRange(IEnumerable<TEntity> entities);
        Task UpdateAsync(TEntity entity);
        Task UpdateRangeAsync(IEnumerable<TEntity> entities);
    }
}
