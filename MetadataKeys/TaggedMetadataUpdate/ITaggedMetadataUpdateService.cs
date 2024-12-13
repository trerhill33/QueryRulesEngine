using QueryRulesEngine.Persistence;

namespace QueryRulesEngine.MetadataKeys.TaggedMetadataUpdate
{
    public interface ITaggedMetadataUpdateService
    {
        Task<Result<int>> UpdateMetadataValueByTagAsync(string approverId, string metadataKey, string tag, string value, CancellationToken cancellationToken = default);
    }
}