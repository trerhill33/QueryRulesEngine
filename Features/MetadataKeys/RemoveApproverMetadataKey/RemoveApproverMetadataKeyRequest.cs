namespace QueryRulesEngine.Features.MetadataKeys.RemoveApproverMetadataKey
{
    public record RemoveApproverMetadataKeyRequest
    {
        public required int HierarchyId { get; init; }
        public required string KeyName { get; init; }
    }
}
