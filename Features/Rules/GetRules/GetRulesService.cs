using QueryRulesEngine.Features.Rules.GetRules.Models;
using QueryRulesEngine.Persistence;
using QueryRulesEngine.Persistence.Entities;
using QueryRulesEngine.QueryEngine.Common.Models;
using QueryRulesEngine.QueryEngine.Persistence;
using System.Text.RegularExpressions;

namespace QueryRulesEngine.Features.Rules.GetRules;

public sealed class GetRulesService(
    IReadOnlyRepositoryAsync<int> readOnlyRepository,
    IQueryPersistenceService queryPersistenceService) : IGetRulesService
{
    private readonly IReadOnlyRepositoryAsync<int> _readOnlyRepository = readOnlyRepository;
    private readonly IQueryPersistenceService _queryPersistenceService = queryPersistenceService;
    public async Task<Result<GetRulesResponse>> ExecuteAsync(
        int hierarchyId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var metadataKeys = await GetHierarchyRuleKeys(hierarchyId, cancellationToken);

            if (metadataKeys?.Count == 0)
            {
                return await Result<GetRulesResponse>.FailAsync(
                    $"No rules found for hierarchy {hierarchyId}");
            }

            var rulesByLevel = await BuildRulesByLevel(metadataKeys, cancellationToken);

            var response = new GetRulesResponse
            {
                HierarchyId = hierarchyId,
                Levels = BuildLevels(rulesByLevel)
            };

            return await Result<GetRulesResponse>.SuccessAsync(response);
        }
        catch (Exception ex)
        {
            return await Result<GetRulesResponse>.FailAsync(
                $"Error retrieving hierarchy rules: {ex.Message}");
        }
    }

    private async Task<List<MetadataKey>?> GetHierarchyRuleKeys(
        int hierarchyId,
        CancellationToken cancellationToken)
    {
        return await _readOnlyRepository.FindAllByPredicateAsync<MetadataKey>(
            mk => mk.HierarchyId == hierarchyId &&
                  mk.KeyName.StartsWith(RulePatterns.LevelPrefix) &&
                  mk.KeyName.Contains(RulePatterns.RulePrefix),
            cancellationToken);
    }

    private async Task<Dictionary<int, List<HierarchyRule>>> BuildRulesByLevel(
        List<MetadataKey> metadataKeys,
        CancellationToken cancellationToken)
    {
        var rulesByLevel = new Dictionary<int, List<HierarchyRule>>();

        foreach (var key in metadataKeys)
        {
            var (level, ruleNumber) = ParseRuleIdentifiers(key.KeyName);
            if (!level.HasValue || !ruleNumber.HasValue) continue;

            var rule = await BuildHierarchyRule(key,  ruleNumber.Value, cancellationToken);
            AddRuleToLevel(rulesByLevel, level.Value, rule);
        }

        return rulesByLevel;
    }

    private static (int? level, int? ruleNumber) ParseRuleIdentifiers(string keyName)
    {
        var levelMatch = Regex.Match(keyName, @"level\.(\d+)");
        var ruleMatch = Regex.Match(keyName, @"rule\.(\d+)");

        if (!levelMatch.Success || !ruleMatch.Success)
            return (null, null);

        return (
            int.Parse(levelMatch.Groups[1].Value),
            int.Parse(ruleMatch.Groups[1].Value)
        );
    }

    private async Task<HierarchyRule> BuildHierarchyRule(
        MetadataKey key,
        int ruleNumber,
        CancellationToken cancellationToken)
    {
        var queryMatrix = _queryPersistenceService.ParseFromStorageFormat(key.KeyName);

        return new HierarchyRule
        {
            RuleNumber = ruleNumber,
            Configuration = await AnalyzeRuleConfiguration(queryMatrix, cancellationToken),
        };
    }

    private async Task<RuleConfiguration> AnalyzeRuleConfiguration(
        QueryMatrix matrix,
        CancellationToken cancellationToken)
    {
        return new RuleConfiguration
        {
            IsManagerRule = HasManagerCondition(matrix),
            IsCustomList = HasCustomListCondition(matrix),
            MetadataKeys = ExtractMetadataKeys(matrix),
            CustomListApprovers = await ExtractCustomListApprovers(matrix, cancellationToken)
        };
    }

    private static bool HasManagerCondition(QueryMatrix matrix)
    {
        return matrix.Conditions.Any(c => c.Field == RuleFieldPatterns.Employee.Manager);
    }

    private static bool HasCustomListCondition(QueryMatrix matrix)
    {
        return matrix.Conditions.Any(c =>
            c.Field == RuleFieldPatterns.Employee.TMID &&
            !c.Field.Contains("ReportsTo"));
    }

    private static IReadOnlyCollection<string> ExtractMetadataKeys(QueryMatrix matrix)
    {
        var keys = new HashSet<string>();
        ExtractKeysRecursive(matrix, keys);
        return [.. keys];
    }

    private static void ExtractKeysRecursive(QueryMatrix matrix, HashSet<string> keys)
    {
        // Check conditions at current level
        foreach (var condition in matrix.Conditions)
        {
            if (condition.Field.StartsWith(RuleFieldPatterns.MetadataPrefix.ApproverMetadataKey))
            {
                var key = condition.Field.Replace(RuleFieldPatterns.MetadataPrefix.ApproverMetadataKey, "");
                keys.Add(key);
            }
        }

        // Check nested matrices
        foreach (var nested in matrix.NestedMatrices)
        {
            ExtractKeysRecursive(nested, keys);
        }
    }

    private async Task<IReadOnlyCollection<CustomListApprover>> ExtractCustomListApprovers(
        QueryMatrix matrix,
        CancellationToken cancellationToken)
    {
        var approvers = new HashSet<CustomListApprover>();

        async Task ExtractApprovers(QueryMatrix m)
        {
            foreach (var condition in m.Conditions.Where(c =>
                c.Field == RuleFieldPatterns.Employee.TMID))
            {
                var tmid = condition.Value.Value?.ToString();
                if (string.IsNullOrEmpty(tmid)) continue;

                var employee = await _readOnlyRepository
                    .FindByPredicateAsNoTrackingAsync<Employee>(e => e.TMID == tmid, cancellationToken); 
                if (employee == null) continue;

                approvers.Add(new CustomListApprover
                {
                    TMID = employee.TMID,
                    FirstName = employee.FirstName,
                    LastName = employee.LastName
                });
            }

            foreach (var nested in m.NestedMatrices)
            {
                await ExtractApprovers(nested);
            }
        }

        await ExtractApprovers(matrix);
        return [.. approvers];
    }

    private static IReadOnlyCollection<HierarchyLevel> BuildLevels(
        Dictionary<int, List<HierarchyRule>> rulesByLevel)
    {
        return [.. rulesByLevel
            .Select(kvp => new HierarchyLevel
            {
                Level = kvp.Key,
                Rules = [.. kvp.Value.OrderBy(r => r.RuleNumber)]
            })
            .OrderBy(l => l.Level)];
    }

    private static void AddRuleToLevel(
        Dictionary<int, List<HierarchyRule>> rulesByLevel,
        int level,
        HierarchyRule rule)
    {
        if (!rulesByLevel.TryGetValue(level, out var rules))
        {
            rules = [];
            rulesByLevel[level] = rules;
        }
        rules.Add(rule);
    }
}