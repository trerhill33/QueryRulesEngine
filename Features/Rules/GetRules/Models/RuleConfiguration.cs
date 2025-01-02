namespace QueryRulesEngine.Features.Rules.GetRules.Models;

public sealed record RuleConfiguration
{
    public bool IsManagerRule { get; init; }
    public bool IsCustomList { get; init; }
    public required IReadOnlyCollection<string> MetadataKeys { get; init; } = [];
    public required IReadOnlyCollection<CustomListApprover> CustomListApprovers { get; init; } = [];
}