using QueryRulesEngine.Persistence;

namespace QueryRulesEngine.MetadataKeys.SyncApproverMetadataKeys
{
    public interface ISyncApproverMetadataKeysService
    {
        Task<Result<SyncApproverMetadataKeysResponse>> ExecuteAsync(SyncApproverMetadataKeysRequest request, CancellationToken cancellationToken = default);
    }
}