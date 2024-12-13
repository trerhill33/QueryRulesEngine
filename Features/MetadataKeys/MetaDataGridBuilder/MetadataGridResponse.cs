namespace QueryRulesEngine.Features.MetadataKeys.MetaDataGridBuilder
{
    public record MetadataGridResponse
    {
        // Pre-computed distinct hierarchies for easy grid column setup
        public required IReadOnlyList<HierarchyTagGroup> TagGroups { get; init; }
        // The actual grid data
        public required IReadOnlyList<ApproverMetadataRow> Data { get; init; }
    }
}
