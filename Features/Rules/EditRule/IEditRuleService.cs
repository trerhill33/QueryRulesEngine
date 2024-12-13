using QueryRulesEngine.Persistence;

namespace QueryRulesEngine.Features.Rules.EditRule
{
    public interface IEditRuleService
    {
        Task<Result<EditRuleResponse>> ExecuteAsync(EditRuleRequest request, CancellationToken cancellationToken = default);
    }
}