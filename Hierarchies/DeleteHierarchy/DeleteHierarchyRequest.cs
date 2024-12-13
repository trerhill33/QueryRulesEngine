namespace QueryRulesEngine.Hierarchies.DeleteHierarchy
{
    public record DeleteHierarchyRequest
    {
        public required int HierarchyId { get; init; }
    }
}
