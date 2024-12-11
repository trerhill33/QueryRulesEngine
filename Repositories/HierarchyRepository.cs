using QueryRulesEngine.Entities;
using QueryRulesEngine.Persistence;

namespace QueryRulesEngine.Repositories
{
    public interface IHierarchyRepository
    {
        Task<bool> HierarchyExistsAsync(int hierarchyId, CancellationToken cancellationToken);
        Task<bool> LevelExistsAsync(int hierarchyId, int levelNumber, CancellationToken cancellationToken);
        Task<bool> IsUniqueRuleNumberAsync(int hierarchyId, int levelNumber, string ruleNumber, CancellationToken cancellationToken);
        Task<bool> IsUniqueMetadataKeyNameAsync(int hierarchyId, string keyName, CancellationToken cancellationToken);
        Task<bool> IsUniqueHierarchyNameAsync(string name, CancellationToken cancellationToken);
    }

    public sealed class HierarchyRepository : IHierarchyRepository
    {
        private readonly IReadOnlyRepositoryAsync<int> _readOnlyRepository;

        public HierarchyRepository(IReadOnlyRepositoryAsync<int> readOnlyRepository) => _readOnlyRepository = readOnlyRepository;

        public async Task<bool> HierarchyExistsAsync(int hierarchyId, CancellationToken cancellationToken)
        {
            return await _readOnlyRepository.FindByPredicateAndTransformAsync<Hierarchy, bool>(
                h => h.Id == hierarchyId,
                h => true,
                cancellationToken);
        }

        public async Task<bool> LevelExistsAsync(int hierarchyId, int levelNumber, CancellationToken cancellationToken)
        {
            var levelKey = $"level.{levelNumber}";
            return await _readOnlyRepository.FindByPredicateAndTransformAsync<MetadataKey, bool>(
                mk => mk.HierarchyId == hierarchyId && mk.KeyName == levelKey,
                mk => true,
                cancellationToken);
        }

        public async Task<bool> IsUniqueRuleNumberAsync(int hierarchyId, int levelNumber, string ruleNumber, CancellationToken cancellationToken)
        {
            var rulePrefix = $"level.{levelNumber}.rule.";
            var rules = await _readOnlyRepository.FindAllByPredicateAndTransformAsync<MetadataKey, string>(
                mk => mk.HierarchyId == hierarchyId && mk.KeyName.StartsWith(rulePrefix),
                mk => mk.KeyName,
                cancellationToken);

            return !rules.Any(r => r.Contains(ruleNumber));
        }

        public async Task<bool> IsUniqueMetadataKeyNameAsync(int hierarchyId, string keyName, CancellationToken cancellationToken)
        {
            return !(await _readOnlyRepository.FindByPredicateAndTransformAsync<MetadataKey, bool>(
                mk => mk.HierarchyId == hierarchyId && mk.KeyName == keyName,
                mk => true,
                cancellationToken));
        }

        public async Task<bool> IsUniqueHierarchyNameAsync(string name, CancellationToken cancellationToken)
        {
            return !(await _readOnlyRepository.FindByPredicateAndTransformAsync<Hierarchy, bool>(
                h => h.Name == name,
                h => true,
                cancellationToken));
        }
    }
}
