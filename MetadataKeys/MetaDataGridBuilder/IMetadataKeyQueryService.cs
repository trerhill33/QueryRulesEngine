using QueryRulesEngine.Persistence;

namespace QueryRulesEngine.MetadataKeys.MetaDataGridBuilder
{
    public interface IMetadataKeyQueryService
    {
        Task<Result<MetadataGridResponse>> GetMetadataValuesForKeyAsync(string metadataKey, CancellationToken cancellationToken = default);
    }
}