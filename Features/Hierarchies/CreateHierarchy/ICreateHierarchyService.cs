using QueryRulesEngine.Persistence;

namespace QueryRulesEngine.Features.Hierarchies.CreateHierarchy
{
    public interface ICreateHierarchyService
    {
        Task<Result<CreateHierarchyResponse>> ExecuteAsync(CreateHierarchyRequest request, CancellationToken cancellationToken = default);
    }
}