using QueryRulesEngine.Entities;

namespace QueryRulesEngine.Repositories.Interfaces
{
    public interface IHierarchyRepository
    {
        Task<bool> HierarchyExistsAsync(int hierarchyId, CancellationToken cancellationToken);
        Task<bool> IsUniqueHierarchyNameAsync(string name, CancellationToken cancellationToken);
        Task<Hierarchy> CreateHierarchyAsync(string name, string description, CancellationToken cancellationToken);
    }
}
