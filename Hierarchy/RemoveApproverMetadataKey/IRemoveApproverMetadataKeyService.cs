using QueryRulesEngine.Persistence;

namespace QueryRulesEngine.Hierarchys.RemoveApproverMetadataKey
{
    public interface IRemoveApproverMetadataKeyService
    {
        Task<Result<RemoveApproverMetadataKeyResponse>> ExecuteAsync(RemoveApproverMetadataKeyRequest request, CancellationToken cancellationToken = default);
    }
}