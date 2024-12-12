using QueryRulesEngine.QueryEngine.Common.Models;
using System.Data;

namespace QueryRulesEngine.Repositories.Interfaces
{
    public interface IRuleRepository
    {
        Task<bool> IsUniqueRuleNumberAsync(int hierarchyId, int levelNumber, string ruleNumber, CancellationToken cancellationToken);
        Task CreateRuleAsync(int hierarchyId, int levelNumber, string ruleNumber, QueryMatrix queryMatrix, CancellationToken cancellationToken);
    }
}
