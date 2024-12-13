namespace QueryRulesEngine.Features.MetadataKeys.RemoveApproverMetadataKey
{
    public sealed record RemoveApproverMetadataKeyResponse
    (
        int HierarchyId,
        string KeyName
    );
}
