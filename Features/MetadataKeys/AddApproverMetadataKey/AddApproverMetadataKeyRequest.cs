public record AddApproverMetadataKeyRequest
{
    public required int HierarchyId { get; init; }
    public required string KeyName { get; init; }
}
