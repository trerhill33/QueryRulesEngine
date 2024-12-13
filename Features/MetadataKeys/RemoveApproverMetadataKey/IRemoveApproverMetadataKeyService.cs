using QueryRulesEngine.Persistence;

namespace QueryRulesEngine.Features.MetadataKeys.RemoveApproverMetadataKey
{
    public interface IRemoveApproverMetadataKeyService
    {
        Task<Result<RemoveApproverMetadataKeyResponse>> ExecuteAsync(RemoveApproverMetadataKeyRequest request, CancellationToken cancellationToken = default);
    }
}