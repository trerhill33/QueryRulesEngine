using QueryRulesEngine.Entities;
using QueryRulesEngine.Persistence;
using QueryRulesEngine.Repositories.Interfaces;

namespace QueryRulesEngine.Repositories
{
    public sealed class LevelRepository(
        IUnitOfWork<int> unitOfWork,
        IReadOnlyRepositoryAsync<int> readOnlyRepository) : ILevelRepository
    {
        public async Task CreateDefaultLevelsAsync(int hierarchyId, CancellationToken cancellationToken)
        {
            var defaultLevels = new[]
            {
            new MetadataKey { HierarchyId = hierarchyId, KeyName = "level.1" },
            new MetadataKey { HierarchyId = hierarchyId, KeyName = "level.2" }
        };

            var repository = unitOfWork.Repository<MetadataKey>();
            foreach (var level in defaultLevels)
            {
                await repository.AddAsync(level, cancellationToken);
            }
            await unitOfWork.CommitAsync(cancellationToken);
        }

        public async Task<bool> LevelExistsAsync(int hierarchyId, int levelNumber, CancellationToken cancellationToken)
        {
            var levelKey = $"level.{levelNumber}";
            return await readOnlyRepository.FindByPredicateAndTransformAsync<MetadataKey, bool>(
                mk => mk.HierarchyId == hierarchyId && mk.KeyName.StartsWith(levelKey),
                mk => true,
                cancellationToken);
        }
    }
}
