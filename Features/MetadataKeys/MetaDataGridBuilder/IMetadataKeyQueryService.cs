using QueryRulesEngine.Persistence;

namespace QueryRulesEngine.Features.MetadataKeys.MetaDataGridBuilder
{
    public interface IMetadataKeyQueryService
    {
        Task<Result<MetadataGridResponse>> GetMetadataValuesForKeyAsync(string metadataKey, CancellationToken cancellationToken = default);
    }
}