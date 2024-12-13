namespace QueryRulesEngine.MetadataKeys.MetaDataGridBuilder
{
    public record ApproverMetadataRow
    {
        // Approver information
        public required string ApproverId { get; init; }
        public required string ApproverName { get; init; }

        // Values for each hierarchy this approver is in
        public required IReadOnlyList<HierarchyMetadataValue> HierarchyValues { get; init; }
    }

}
