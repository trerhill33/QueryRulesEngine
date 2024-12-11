using QueryRulesEngine.Persistence;
using QueryRulesEngine.Rules.RemoveRule;

namespace QueryRulesEngine.Hierarchys.RemoveRule
{
    public interface IRemoveRuleService
    {
        Task<Result<RemoveRuleResponse>> ExecuteAsync(RemoveRuleRequest request, CancellationToken cancellationToken = default);
    }
}