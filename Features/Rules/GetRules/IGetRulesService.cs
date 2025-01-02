using QueryRulesEngine.Features.Rules.GetRules.Models;
using QueryRulesEngine.Persistence;

namespace QueryRulesEngine.Features.Rules.GetRules
{
    public interface IGetRulesService
    {
        Task<Result<GetRulesResponse>> ExecuteAsync(int hierarchyId, CancellationToken cancellationToken = default);
    }
}