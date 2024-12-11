using QueryRulesEngine.Persistence;

public interface IGetHierarchyDetailsService
{
    Task<Result<GetHierarchyDetailsResponse>> ExecuteAsync(GetHierarchyDetailsRequest request, CancellationToken cancellationToken = default);
}