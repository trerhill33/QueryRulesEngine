namespace QueryRulesEngine.MetadataKeys.MetaDataGridBuilder
{
    public record HierarchyInfo
    {
        public required int Id { get; init; }
        public required string Name { get; init; }
        public required string Tag { get; init; }
    }
}
