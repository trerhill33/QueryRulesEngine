using QueryRulesEngine.Persistence;

namespace QueryRulesEngine.Features.Hierarchies.DeleteHierarchy
{
    public interface IDeleteHierarchyService
    {
        Task<Result<DeleteHierarchyResponse>> ExecuteAsync(DeleteHierarchyRequest request, CancellationToken cancellationToken = default);
    }
}