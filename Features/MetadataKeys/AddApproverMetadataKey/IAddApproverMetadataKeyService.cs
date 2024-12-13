using QueryRulesEngine.Persistence;

public interface IAddApproverMetadataKeyService
{
    Task<Result<AddApproverMetadataKeyResponse>> ExecuteAsync(AddApproverMetadataKeyRequest request, CancellationToken cancellationToken = default);
}