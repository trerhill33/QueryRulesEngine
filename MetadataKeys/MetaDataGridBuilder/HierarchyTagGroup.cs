namespace QueryRulesEngine.MetadataKeys.MetaDataGridBuilder
{
    public record HierarchyTagGroup
    {
        public required string Tag { get; init; }
        public required IReadOnlyList<HierarchyInfo> Hierarchies { get; init; }
    }
}
