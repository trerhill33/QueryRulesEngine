namespace QueryRulesEngine.Repositories.Interfaces
{
    public interface IApproverMetadataRepository
    {
        // Only methods actually needed for business logic
        Task<bool> IsUniqueKeyForHierarchyAsync(int hierarchyId, string keyName, CancellationToken cancellationToken);
        Task CreateApproverMetadataKeyAsync(int hierarchyId, string keyName, CancellationToken cancellationToken);
    }
}
