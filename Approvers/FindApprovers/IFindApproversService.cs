using QueryRulesEngine.Persistence;

namespace QueryRulesEngine.Approvers.FindApprovers
{
    public interface IFindApproversService
    {
        Task<Result<FindApproversResponse>> ExecuteAsync(FindApproversRequest request, CancellationToken cancellationToken = default);
    }
}