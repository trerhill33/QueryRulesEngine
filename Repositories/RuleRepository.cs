using QueryRulesEngine.dtos;
using QueryRulesEngine.Entities;
using QueryRulesEngine.Persistence;
using QueryRulesEngine.QueryEngine.Persistence;
using QueryRulesEngine.Repositories.Interfaces;
using System.Data;

public sealed class RuleRepository(
    IUnitOfWork<int> unitOfWork,
    IReadOnlyRepositoryAsync<int> readOnlyRepository,
    IQueryPersistenceService queryPersistenceService) : IRuleRepository
{
    public async Task<bool> ExistsAsync(
        RuleDto rule,
        CancellationToken cancellationToken)
    {
        var ruleKeyPattern = $"level.{rule.LevelNumber}.rule.{rule.RuleNumber}.query:";
        return await readOnlyRepository.FindByPredicateAndTransformAsync<MetadataKey, bool>(
            mk => mk.HierarchyId == rule.HierarchyId && mk.KeyName.StartsWith(ruleKeyPattern),
            mk => true,
            cancellationToken);
    }

    public async Task CreateRuleAsync(
        RuleDto rule,
        CancellationToken cancellationToken)
    {
        var persistedQuery = queryPersistenceService.ConvertToStorageFormat(rule.QueryMatrix);
        var ruleKeyName = $"level.{rule.LevelNumber}.rule.{rule.RuleNumber}.query:{persistedQuery}";

        var metadataKey = new MetadataKey
        {
            HierarchyId = rule.HierarchyId,
            KeyName = ruleKeyName
        };

        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var repository = unitOfWork.Repository<MetadataKey>();
            await repository.AddAsync(metadataKey, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);
        }, cancellationToken);
    }

    public async Task UpdateRuleAsync(
    RuleDto rule,
    CancellationToken cancellationToken)
    {
        var persistedQuery = queryPersistenceService.ConvertToStorageFormat(rule.QueryMatrix);
        var ruleKeyName = $"level.{rule.LevelNumber}.rule.{rule.RuleNumber}.query:{persistedQuery}";

        var metadataKey = await readOnlyRepository.FindByPredicateAsync<MetadataKey>(
            mk =>
            mk.HierarchyId == rule.HierarchyId
            && mk.KeyName.StartsWith($"level.{rule.LevelNumber}.rule.{rule.RuleNumber}"),
            cancellationToken)
            ?? throw new InvalidOperationException($"Rule with Level {rule.RuleNumber} not found.");

        metadataKey.KeyName = ruleKeyName;

        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await unitOfWork.Repository<MetadataKey>().UpdateAsync(metadataKey);
            await unitOfWork.CommitAsync(cancellationToken);
        }, cancellationToken);
    }

    public async Task<int> GetNextRuleNumberAsync(
        int hierarchyId,
        int levelNumber,
        CancellationToken cancellationToken)
    {
        var rulePrefix = $"level.{levelNumber}.rule.";
        var existingRules = await readOnlyRepository.FindAllByPredicateAndTransformAsync<MetadataKey, string>(
            mk => mk.HierarchyId == hierarchyId && mk.KeyName.StartsWith(rulePrefix),
            mk => mk.KeyName,
            cancellationToken);

        if (!existingRules.Any())
        {
            return 1;
        }

        var maxRuleNumber = existingRules
            .Select(ParseRuleNumber)
            .Where(n => n.HasValue)
            .Max();

        return maxRuleNumber.GetValueOrDefault() + 1;
    }

    public async Task<List<string>> GetExistingMetadataKeysAsync(
        int hierarchyId,
        IEnumerable<string> keyNames,
        CancellationToken cancellationToken)
    {
        return await readOnlyRepository.FindAllByPredicateAndTransformAsync<MetadataKey, string>(
            mk => mk.HierarchyId == hierarchyId && keyNames.Contains(mk.KeyName),
            mk => mk.KeyName,
            cancellationToken);
    }

    private static int? ParseRuleNumber(string keyName)
    {
        try
        {
            // Format: "level.{levelNumber}.rule.{ruleNumber}.query:{queryString}"
            var parts = keyName.Split('.');
            if (parts.Length < 4)
            {
                return null;
            }

            // Get the rule number part and remove any query string
            var ruleNumberPart = parts[3];
            var queryIndex = ruleNumberPart.IndexOf(".query:", StringComparison.OrdinalIgnoreCase);
            if (queryIndex >= 0)
            {
                ruleNumberPart = ruleNumberPart[..queryIndex];
            }

            if (int.TryParse(ruleNumberPart, out var ruleNumber))
            {
                return ruleNumber;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<RuleDto> GetRuleAsync(
        int hierarchyId,
        int levelNumber,
        string ruleNumber,
        CancellationToken cancellationToken)
    {
        var metadataKey = await readOnlyRepository.FindByPredicateAsync<MetadataKey>(
            mk => mk.HierarchyId == hierarchyId &&
                  mk.KeyName.StartsWith($"level.{levelNumber}.rule.{ruleNumber}.query:"),
            cancellationToken);

        if (metadataKey == null)
            return null;

        var queryStartIndex = metadataKey.KeyName.IndexOf("query:") + 6;
        var persistedQuery = metadataKey.KeyName[queryStartIndex..];
        var queryMatrix = queryPersistenceService.ParseFromStorageFormat(persistedQuery);

        return new RuleDto
        {
            HierarchyId = hierarchyId,
            LevelNumber = levelNumber,
            RuleNumber = ruleNumber,
            QueryMatrix = queryMatrix
        };
    }


}