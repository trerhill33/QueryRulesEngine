using QueryRulesEngine.Entities;
using QueryRulesEngine.Persistence;
using QueryRulesEngine.Repositories.Interfaces;

namespace QueryRulesEngine.Repositories
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
            return !(await _readOnlyRepository.FindByPredicateAndTransformAsync<MetadataKey, bool>(
                mk => mk.HierarchyId == hierarchyId && mk.KeyName == fullKeyName,
                mk => true,
                cancellationToken));
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
    }
}
