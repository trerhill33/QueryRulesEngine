using QueryRulesEngine.Persistence;
using QueryRulesEngine.Rules.EditRule;

namespace QueryRulesEngine.Hierarchys.EditRule
{
    public interface IEditRuleService
    {
        Task<Result<EditRuleResponse>> ExecuteAsync(EditRuleRequest request, CancellationToken cancellationToken = default);
    }
}