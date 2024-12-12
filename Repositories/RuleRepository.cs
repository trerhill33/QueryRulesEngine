using QueryRulesEngine.Entities;
using QueryRulesEngine.Persistence;
using QueryRulesEngine.QueryEngine.Common.Models;
using QueryRulesEngine.QueryEngine.Persistence;
using QueryRulesEngine.Repositories.Interfaces;

namespace QueryRulesEngine.Repositories
{
    public sealed class RuleRepository(
        IUnitOfWork<int> unitOfWork,
        IReadOnlyRepositoryAsync<int> readOnlyRepository,
        IQueryPersistenceService queryPersistenceService) : IRuleRepository
    {
        public async Task<bool> IsUniqueRuleNumberAsync(int hierarchyId, int levelNumber, string ruleNumber, CancellationToken cancellationToken)
        {
            var rulePrefix = $"level.{levelNumber}.rule.";
            var rules = await readOnlyRepository.FindAllByPredicateAndTransformAsync<MetadataKey, string>(
                mk => mk.HierarchyId == hierarchyId && mk.KeyName.StartsWith(rulePrefix),
                mk => mk.KeyName,
                cancellationToken);
            return !rules.Any(r => r.Contains(ruleNumber));
        }

        public async Task CreateRuleAsync(int hierarchyId, int levelNumber, string ruleNumber, QueryMatrix queryMatrix, CancellationToken cancellationToken)
        {
            var persistedQuery = queryPersistenceService.ConvertToStorageFormat(queryMatrix);
            var ruleKey = $"level.{levelNumber}.rule.{ruleNumber}.query:{persistedQuery}";

            var metadataKey = new MetadataKey
            {
                HierarchyId = hierarchyId,
                KeyName = ruleKey
            };

            await unitOfWork.Repository<MetadataKey>().AddAsync(metadataKey, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);
        }
    }
}
