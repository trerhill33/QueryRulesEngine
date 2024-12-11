using QueryRulesEngine.Persistence;

namespace QueryRulesEngine.Rules.AddRuleToLevel
{
    public interface IAddRuleToLevelService
    {
        Task<Result<AddRuleToLevelResponse>> ExecuteAsync(AddRuleToLevelRequest request, CancellationToken cancellationToken = default);
    }
}