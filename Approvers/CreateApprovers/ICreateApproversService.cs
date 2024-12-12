using QueryRulesEngine.Persistence;

namespace QueryRulesEngine.Approvers.CreateApprovers
{
    public interface ICreateApproversService
    {
        Task<Result<CreateApproversResponse>> ExecuteAsync(CreateApproversRequest request, CancellationToken cancellationToken = default);
    }
}