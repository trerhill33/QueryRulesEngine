namespace QueryRulesEngine.Repositories.Interfaces
{
    public interface IApproverMetadataRepository
    {
        Task<bool> IsUniqueKeyForHierarchyAsync(int hierarchyId, string keyName, CancellationToken cancellationToken);
        Task CreateApproverMetadataKeyAsync(int hierarchyId, string keyName, CancellationToken cancellationToken);
    }
}
