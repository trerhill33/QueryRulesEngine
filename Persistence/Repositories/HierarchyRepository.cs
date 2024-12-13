using QueryRulesEngine.Persistence.Entities;
using QueryRulesEngine.Persistence.Repositories.Interfaces;

namespace QueryRulesEngine.Persistence.Repositories
{
    public sealed class HierarchyRepository(
        IUnitOfWork<int> unitOfWork,
        IReadOnlyRepositoryAsync<int> readOnlyRepository) : IHierarchyRepository
    {
        public async Task<bool> HierarchyExistsAsync(int hierarchyId, CancellationToken cancellationToken) =>
            await readOnlyRepository.FindByPredicateAndTransformAsync<Hierarchy, bool>(
                h => h.Id == hierarchyId,
                h => true,
                cancellationToken);

        public async Task<bool> IsUniqueHierarchyNameAsync(string name, CancellationToken cancellationToken) =>
            !await readOnlyRepository.FindByPredicateAndTransformAsync<Hierarchy, bool>(
                h => h.Name == name,
                h => true,
                cancellationToken);

        public async Task<Hierarchy> CreateHierarchyAsync(string name, string description, string tag, CancellationToken cancellationToken)
        {
            var newHierarchy = new Hierarchy { Name = name, Description = description, Tag = tag };
            await unitOfWork.Repository<Hierarchy>().AddAsync(newHierarchy, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);
            return newHierarchy;
        }
    }
}
