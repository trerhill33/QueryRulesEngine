using QueryRulesEngine.Persistence;

public interface IAddMetadataKeyService
{
    Task<Result<AddMetadataKeyResponse>> ExecuteAsync(AddMetadataKeyRequest request, CancellationToken cancellationToken = default);
}