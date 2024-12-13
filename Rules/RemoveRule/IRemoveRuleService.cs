using QueryRulesEngine.Persistence;

namespace QueryRulesEngine.Rules.RemoveRule
{
    public interface IRemoveRuleService
    {
        Task<Result<RemoveRuleResponse>> ExecuteAsync(RemoveRuleRequest request, CancellationToken cancellationToken = default);
    }
}