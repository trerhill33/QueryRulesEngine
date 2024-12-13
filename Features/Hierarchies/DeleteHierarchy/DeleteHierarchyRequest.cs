namespace QueryRulesEngine.Features.Hierarchies.DeleteHierarchy
{
    public record DeleteHierarchyRequest
    {
        public required int HierarchyId { get; init; }
    }
}
