using QueryRulesEngine.Entities;
using QueryRulesEngine.Persistence;

namespace QueryRulesEngine.Repositories
{
    public sealed class HierarchyRepository(
        IUnitOfWork<int> unitOfWork,
        IReadOnlyRepositoryAsync<int> readOnlyRepository) : QueryRulesEngine.Repositories.Interfaces.IHierarchyRepository
    {
        public async Task<bool> HierarchyExistsAsync(int hierarchyId, CancellationToken cancellationToken) =>
            await readOnlyRepository.FindByPredicateAndTransformAsync<Hierarchy, bool>(
                h => h.Id == hierarchyId,
                h => true,
                cancellationToken);

        public async Task<bool> IsUniqueHierarchyNameAsync(string name, CancellationToken cancellationToken) =>
            !(await readOnlyRepository.FindByPredicateAndTransformAsync<Hierarchy, bool>(
                h => h.Name == name,
                h => true,
                cancellationToken));

        public async Task<Hierarchy> CreateHierarchyAsync(string name, string description, CancellationToken cancellationToken)
        {
            var hierarchy = new Hierarchy { Name = name, Description = description };
            await unitOfWork.Repository<Hierarchy>().AddAsync(hierarchy, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);
            return hierarchy;
        }
    }
}
