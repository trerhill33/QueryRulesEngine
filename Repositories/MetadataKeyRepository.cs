using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QueryRulesEngine.Entities;
using QueryRulesEngine.Persistence;

namespace QueryRulesEngine.Repositories
{
    public interface IMetadataKeyRepository
    {
        Task<bool> MetadataKeyExistsAsync(int hierarchyId, string keyName, CancellationToken cancellationToken);

        Task<List<string>> GetMetadataKeyNamesAsync(int hierarchyId, IEnumerable<string> keyNames, CancellationToken cancellationToken);

        Task<int> GetNextRuleNumberAsync(int hierarchyId, int levelNumber, CancellationToken cancellationToken);

        Task AddMetadataKeyAsync(MetadataKey metadataKey, CancellationToken cancellationToken);

        Task<List<MetadataKey>> GetMetadataKeysAsync(int hierarchyId, CancellationToken cancellationToken);

    }

    public sealed class MetadataKeyRepository(IReadOnlyRepositoryAsync<int> readOnlyRepository, IUnitOfWork<int> unitOfWork) : IMetadataKeyRepository
    {
        private readonly IReadOnlyRepositoryAsync<int> _readOnlyRepository = readOnlyRepository;
        private readonly IUnitOfWork<int> _unitOfWork = unitOfWork;

        public async Task<bool> MetadataKeyExistsAsync(int hierarchyId, string keyName, CancellationToken cancellationToken)
        {
            return await _readOnlyRepository.FindByPredicateAndTransformAsync<MetadataKey, bool>(
                mk => mk.HierarchyId == hierarchyId && mk.KeyName == keyName,
                mk => true,
                cancellationToken);
        }

        public async Task<List<string>> GetMetadataKeyNamesAsync(int hierarchyId, IEnumerable<string> keyNames, CancellationToken cancellationToken)
        {
            return await _readOnlyRepository.FindAllByPredicateAndTransformAsync<MetadataKey, string>(
                mk => mk.HierarchyId == hierarchyId && keyNames.Contains(mk.KeyName),
                mk => mk.KeyName,
                cancellationToken);
        }

        public async Task<int> GetNextRuleNumberAsync(int hierarchyId, int levelNumber, CancellationToken cancellationToken)
        {
            var rulePrefix = $"level.{levelNumber}.rule.";
            var existingRules = await _readOnlyRepository.FindAllByPredicateAndTransformAsync<MetadataKey, string>(
                mk => mk.HierarchyId == hierarchyId && mk.KeyName.StartsWith(rulePrefix),
                mk => mk.KeyName,
                cancellationToken);

            if (existingRules.Count == 0)
            {
                return 1;
            }

            return existingRules
                .Select(r => ParseRuleNumber(r))
                .Max() + 1;
        }

        public async Task AddMetadataKeyAsync(MetadataKey metadataKey, CancellationToken cancellationToken)
        {
            var metadataKeyRepository = _unitOfWork.Repository<MetadataKey>();
            await metadataKeyRepository.AddAsync(metadataKey, cancellationToken);
        }

        public async Task<List<MetadataKey>> GetMetadataKeysAsync(int hierarchyId, CancellationToken cancellationToken)
        {
            return await _readOnlyRepository.FindAllByPredicateAndTransformAsync<MetadataKey, MetadataKey>(
                mk => mk.HierarchyId == hierarchyId,
                mk => mk,
                cancellationToken);
        }

        private int ParseRuleNumber(string keyName)
        {
            var parts = keyName.Split('.');
            if (parts.Length < 4)
            {
                throw new FormatException($"Invalid rule KeyName format: {keyName}");
            }

            if (int.TryParse(parts[3], out int ruleNumber))
            {
                return ruleNumber;
            }

            throw new FormatException($"Unable to parse rule number from KeyName: {keyName}");
        }
    }
}
