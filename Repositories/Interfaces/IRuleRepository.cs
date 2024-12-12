using QueryRulesEngine.dtos;

namespace QueryRulesEngine.Repositories.Interfaces
{
    public interface IRuleRepository
    {
        Task CreateRuleAsync(RuleDto rule, CancellationToken cancellationToken);
        Task<bool> ExistsAsync(RuleDto rule, CancellationToken cancellationToken);
        Task<List<string>> GetExistingMetadataKeysAsync(int hierarchyId, IEnumerable<string> keyNames, CancellationToken cancellationToken);
        Task<int> GetNextRuleNumberAsync(int hierarchyId, int levelNumber, CancellationToken cancellationToken);
        Task<RuleDto> GetRuleAsync(int hierarchyId, int levelNumber, string ruleNumber, CancellationToken cancellationToken);
        Task UpdateRuleAsync(RuleDto rule, CancellationToken cancellationToken);
    }
}