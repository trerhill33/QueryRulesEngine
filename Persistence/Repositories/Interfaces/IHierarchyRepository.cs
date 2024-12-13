using QueryRulesEngine.Persistence.Entities;

namespace QueryRulesEngine.Persistence.Repositories.Interfaces
{
    public interface IHierarchyRepository
    {
        Task<bool> HierarchyExistsAsync(int hierarchyId, CancellationToken cancellationToken);
        Task<bool> IsUniqueHierarchyNameAsync(string name, CancellationToken cancellationToken);
        Task<Hierarchy> CreateHierarchyAsync(string name, string description, string tag, CancellationToken cancellationToken);
    }
}
