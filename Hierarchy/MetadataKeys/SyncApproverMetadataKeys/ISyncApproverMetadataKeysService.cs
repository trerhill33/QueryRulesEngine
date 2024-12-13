using QueryRulesEngine.Hierarchys.MetadataKeys.SyncApproverMetadataKeys;
using QueryRulesEngine.Hierarchyss.MetadataKeys.SyncApproverMetadataKeys;
using QueryRulesEngine.Persistence;

namespace QueryRulesEngine.Hierarchys.MetadataKeys.SyncApproverMetadataKeys
{
    public interface ISyncApproverMetadataKeysService
    {
        Task<Result<SyncApproverMetadataKeysResponse>> ExecuteAsync(SyncApproverMetadataKeysRequest request, CancellationToken cancellationToken = default);
    }
}