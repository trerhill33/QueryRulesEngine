using QueryRulesEngine.Persistence;

namespace QueryRulesEngine.Features.Approvers.CreateApprovers
{
    public interface ICreateApproversService
    {
        Task<Result<CreateApproversResponse>> ExecuteAsync(CreateApproversRequest request, CancellationToken cancellationToken = default);
    }
}