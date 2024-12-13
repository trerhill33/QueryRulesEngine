namespace QueryRulesEngine.MetadataKeys.MetaDataGridBuilder
{
    public record HierarchyMetadataValue
    {
        public required int HierarchyId { get; init; }
        public required string Value { get; init; }
    }
}
