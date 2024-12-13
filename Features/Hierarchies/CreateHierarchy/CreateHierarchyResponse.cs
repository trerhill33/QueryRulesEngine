namespace QueryRulesEngine.Features.Hierarchies.CreateHierarchy;

public record CreateHierarchyResponse
{
    public int Id { get; init; }
    public string Name { get; init; }
    public string Description { get; init; }
    public string Tag { get; init; }
}