using QueryRulesEngine.Persistence;

namespace QueryRulesEngine.Features.MetadataKeys.TaggedMetadataUpdate;

public interface ITaggedMetadataUpdateService
{
    Task<Result<int>> UpdateMetadataValueByTagAsync(TaggedMetadataUpdateRequest request, CancellationToken cancellationToken = default);
}