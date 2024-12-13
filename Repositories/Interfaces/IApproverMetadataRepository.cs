using QueryRulesEngine.dtos;

namespace QueryRulesEngine.Repositories.Interfaces
{
    public interface IApproverMetadataRepository
    {
        Task<bool> IsUniqueKeyForHierarchyAsync(int hierarchyId, string keyName, CancellationToken cancellationToken);
        Task CreateApproverMetadataKeyAsync(int hierarchyId, string keyName, CancellationToken cancellationToken);
        Task<List<string>> GetApproverMetadataKeysAsync(int hierarchyId, CancellationToken cancellationToken);
        Task<List<ApproverMetadataDto>> GetApproverMetadataValuesAsync(string metadataKey, IEnumerable<int> hierarchyIds, CancellationToken cancellationToken);
    }
}
