using QueryRulesEngine.Persistence;

namespace QueryRulesEngine.Hierarchies.CreateHierarchy
{
    public interface ICreateHierarchyService
    {
        Task<Result<CreateHierarchyResponse>> ExecuteAsync(CreateHierarchyRequest request, CancellationToken cancellationToken = default);
    }
}