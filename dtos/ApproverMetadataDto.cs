namespace QueryRulesEngine.dtos
{
    public record ApproverMetadataDto
    {
        public required string ApproverId { get; init; }
        public required string ApproverName { get; init; }
        public required int HierarchyId { get; init; }
        public string? Value { get; init; }
    }
}
