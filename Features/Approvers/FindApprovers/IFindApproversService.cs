using QueryRulesEngine.Persistence;

namespace QueryRulesEngine.Features.Approvers.FindApprovers
{
    public interface IFindApproversService
    {
        Task<Result<FindApproversResponse>> ExecuteAsync(FindApproversRequest request, CancellationToken cancellationToken = default);
    }
}