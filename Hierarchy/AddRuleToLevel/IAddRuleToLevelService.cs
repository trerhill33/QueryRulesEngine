using QueryRulesEngine.Persistence;

namespace QueryRulesEngine.Hierarchys.AddRuleToLevel
{
    public interface IAddRuleToLevelService
    {
        Task<Result<AddRuleToLevelResponse>> ExecuteAsync(AddRuleToLevelRequest request, CancellationToken cancellationToken = default);
    }
}