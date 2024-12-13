using QueryRulesEngine.dtos;
using QueryRulesEngine.Persistence.Entities;
using QueryRulesEngine.Persistence.Repositories.Interfaces;

namespace QueryRulesEngine.Persistence.Repositories
{
    public sealed class ApproverMetadataRepository(
        IReadOnlyRepositoryAsync<int> readOnlyRepository,
        IUnitOfWork<int> unitOfWork) : IApproverMetadataRepository
    {
        private readonly IReadOnlyRepositoryAsync<int> _readOnlyRepository = readOnlyRepository;
        private readonly IUnitOfWork<int> _unitOfWork = unitOfWork;

        public async Task<bool> IsUniqueKeyForHierarchyAsync(int hierarchyId, string keyName, CancellationToken cancellationToken)
        {
            var fullKeyName = $"ApproverMetadataKey.{keyName}";
            return !await _readOnlyRepository.FindByPredicateAndTransformAsync<MetadataKey, bool>(
                mk => mk.HierarchyId == hierarchyId && mk.KeyName == fullKeyName,
                mk => true,
                cancellationToken);
        }

        public async Task CreateApproverMetadataKeyAsync(int hierarchyId, string keyName, CancellationToken cancellationToken)
        {
            var metadataKey = new MetadataKey
            {
                HierarchyId = hierarchyId,
                KeyName = $"ApproverMetadataKey.{keyName}"
            };

            await _unitOfWork.Repository<MetadataKey>().AddAsync(metadataKey, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);
        }

        public async Task<List<string>> GetApproverMetadataKeysAsync(int hierarchyId, CancellationToken cancellationToken)
        {
            return await readOnlyRepository.FindAllByPredicateAndTransformAsync<MetadataKey, string>(
                mk => mk.HierarchyId == hierarchyId && mk.KeyName.StartsWith("ApproverMetadataKey."),
                mk => mk.KeyName,
                cancellationToken);
        }
        public async Task<List<ApproverMetadataDto>> GetApproverMetadataValuesAsync(
        string metadataKey,
        IEnumerable<int> hierarchyIds,
        CancellationToken cancellationToken)
        {
            var fullKeyName = $"ApproverMetadataKey.{metadataKey}";
            // Get all approvers with their metadata and employee info for the specified hierarchies
            return await _readOnlyRepository.FindAllByPredicateAsNoTrackingAndTransformAsync<Approver, ApproverMetadataDto>(
                a => hierarchyIds.Contains(a.HierarchyId),
                a => new ApproverMetadataDto
                {
                    ApproverId = a.ApproverId,
                    ApproverName = a.Employee.Name,
                    HierarchyId = a.HierarchyId,
                    Value = a.Metadata
                        .Where(m => m.Key == fullKeyName)
                        .Select(m => m.Value)
                        .FirstOrDefault()
                },
                cancellationToken);
        }
    }
}
