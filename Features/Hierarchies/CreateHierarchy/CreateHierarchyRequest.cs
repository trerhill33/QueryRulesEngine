namespace QueryRulesEngine.Features.Hierarchies.CreateHierarchy;

public record CreateHierarchyRequest
{
    public string Name { get; init; }
    public string Description { get; init; }
    public string Tag { get; init; }
}