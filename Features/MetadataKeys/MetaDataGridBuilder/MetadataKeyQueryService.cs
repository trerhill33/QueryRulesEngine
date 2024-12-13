using QueryRulesEngine.Persistence;
using QueryRulesEngine.Persistence.Entities;
using QueryRulesEngine.Persistence.Repositories.Interfaces;

namespace QueryRulesEngine.Features.MetadataKeys.MetaDataGridBuilder
{
    public class MetadataKeyQueryService(
        IReadOnlyRepositoryAsync<int> readOnlyRepository,
        IApproverMetadataRepository approverMetadataRepository) : IMetadataKeyQueryService
    {
        private readonly IReadOnlyRepositoryAsync<int> _readOnlyRepository = readOnlyRepository;
        private readonly IApproverMetadataRepository _approverMetadataRepository = approverMetadataRepository;

        public async Task<Result<MetadataGridResponse>> GetMetadataValuesForKeyAsync(
            string metadataKey,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // 1. Get all hierarchies where this key exists
                var hierarchies = await _readOnlyRepository
                    .FindAllByPredicateAsNoTrackingAndTransformAsync<MetadataKey, HierarchyInfo>(
                        m => m.KeyName == metadataKey,
                        h => new HierarchyInfo
                        {
                            Id = h.HierarchyId,
                            Name = h.Hierarchy.Name,
                            Tag = h.Hierarchy.Tag
                        },
                        cancellationToken);

                if (hierarchies.Count == 0)
                {
                    return await Result<MetadataGridResponse>.FailAsync(
                        $"No hierarchies found with metadata key: {metadataKey}",
                        ResultStatus.Error);
                }

                // 2. Group hierarchies by tag
                var tagGroups = hierarchies
                    .GroupBy(h => h.Tag)
                    .Select(g => new HierarchyTagGroup
                    {
                        Tag = g.Key,
                        Hierarchies = [.. g]
                    })
                    .ToList();

                // 3. Get all approvers and their values for this key
                var approverData = await _approverMetadataRepository
                    .GetApproverMetadataValuesAsync(
                        metadataKey,
                        hierarchies.Select(h => h.Id),
                        cancellationToken);

                var rows = approverData
                    .GroupBy(d => d.ApproverId)
                    .Select(g => new ApproverMetadataRow
                    {
                        ApproverId = g.Key,
                        ApproverName = g.First().ApproverName,
                        HierarchyValues = g.Select(d => new HierarchyMetadataValue
                        {
                            HierarchyId = d.HierarchyId,
                            Value = d.Value ?? string.Empty
                        }).ToList()
                    })
                    .ToList();

                return Result<MetadataGridResponse>.Success(new MetadataGridResponse
                {
                    TagGroups = tagGroups,
                    Data = rows
                });
            }
            catch (Exception ex)
            {
                return await Result<MetadataGridResponse>.FailAsync(
                    $"Error retrieving metadata values: {ex.Message}",
                    ResultStatus.Error);
            }
        }
    }
}
