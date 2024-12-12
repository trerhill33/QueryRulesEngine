namespace QueryRulesEngine.Repositories.Interfaces
{
    public interface ILevelRepository
    {
        Task CreateDefaultLevelsAsync(int hierarchyId, CancellationToken cancellationToken);
        Task<bool> LevelExistsAsync(int hierarchyId, int levelNumber, CancellationToken cancellationToken);
    }
}
