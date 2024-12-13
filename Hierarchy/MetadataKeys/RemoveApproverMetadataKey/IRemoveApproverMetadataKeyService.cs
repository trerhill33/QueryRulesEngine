using QueryRulesEngine.Persistence;

namespace QueryRulesEngine.Hierarchys.MetadataKeys.RemoveApproverMetadataKey
{
    public interface IRemoveApproverMetadataKeyService
    {
        Task<Result<RemoveApproverMetadataKeyResponse>> ExecuteAsync(RemoveApproverMetadataKeyRequest request, CancellationToken cancellationToken = default);
    }
}