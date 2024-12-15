using QueryRulesEngine.Persistence;
using QueryRulesEngine.Persistence.Entities;

namespace QueryRulesEngine.Features.MetadataKeys.TaggedMetadataUpdate;

public class TaggedMetadataUpdateService(
    IUnitOfWork<int> unitOfWork,
    IReadOnlyRepositoryAsync<int> readOnlyRepository) : ITaggedMetadataUpdateService
{
    private readonly IUnitOfWork<int> _unitOfWork = unitOfWork;
    private readonly IReadOnlyRepositoryAsync<int> _readOnlyRepository = readOnlyRepository;

    public async Task<Result<int>> UpdateMetadataValueByTagAsync(
        TaggedMetadataUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Find all hierarchies with the given tag
            var hierarchyIds = await _readOnlyRepository
                .FindAllByPredicateAndTransformAsync<Hierarchy, int>(
                    h => h.Tag == request.Tag,
                    h => h.Id,
                    cancellationToken);

            if (hierarchyIds.Count == 0)
            {
                return await Result<int>.FailAsync(
                    $"No hierarchies found with tag: {request.Tag}",
                    ResultStatus.Error);
            }

            // 2. Update metadata for all matching hierarchies
            var updatedCount = 0;
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                foreach (var hierarchyId in hierarchyIds)
                {
                    var metadata = new Metadata
                    {
                        HierarchyId = hierarchyId,
                        ApproverId = request.ApproverId,
                        Key = request.MetadataKey,
                        Value = request.Value
                    };

                    await _unitOfWork.Repository<Metadata>().AddAsync(metadata, cancellationToken);
                    updatedCount++;
                }
                await _unitOfWork.CommitAsync(cancellationToken);
            }, cancellationToken);

            return Result<int>.Success(updatedCount);
        }
        catch (Exception ex)
        {
            return await Result<int>.FailAsync(
                $"Error updating metadata values: {ex.Message}",
                ResultStatus.Error);
        }
    }
}
