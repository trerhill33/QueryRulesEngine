using QueryRulesEngine.QueryEngine.Common.Models;

public interface IRuleRepository
{
    Task CreateRuleAsync(int hierarchyId, int levelNumber, string ruleNumber, QueryMatrix queryMatrix, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(int hierarchyId, int levelNumber, string ruleNumber, CancellationToken cancellationToken);
    Task<List<string>> GetExistingMetadataKeysAsync(int hierarchyId, IEnumerable<string> keyNames, CancellationToken cancellationToken);
    Task<int> GetNextRuleNumberAsync(int hierarchyId, int levelNumber, CancellationToken cancellationToken);
}