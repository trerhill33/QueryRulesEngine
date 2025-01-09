namespace QueryRulesEngine.Persistence
{
    public interface IUnitOfWork<TId>
    {
        IWriteRepositoryAsync<TEntity, TId> Repository<TEntity>() where TEntity : AuditableEntity<TId>;
        Task<int> CommitAsync(CancellationToken cancellationToken = default);
        Task<int> CommitAndRemoveCacheAsync(CancellationToken cancellationToken = default, params string[] cacheKeys);
        Task ExecuteInTransactionAsync(Func<Task> transactionalOperations, CancellationToken cancellationToken = default);
    }
}