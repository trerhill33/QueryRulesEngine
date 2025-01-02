namespace QueryRulesEngine.Features.Rules.GetRules.Models;
public sealed record CustomListApprover
{
    public required string TMID { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
}