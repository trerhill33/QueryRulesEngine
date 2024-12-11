using ApprovalHierarchyManager.Application.Features.ApprovalHierarchy.CreateHierarchy.Models;
using QueryRulesEngine.Persistence;

namespace QueryRulesEngine.Hierarchys.CreateHierarchy
{
    public interface ICreateHierarchyService
    {
        Task<Result<CreateHierarchyResponse>> ExecuteAsync(CreateHierarchyRequest request, CancellationToken cancellationToken = default);
    }
}