namespace QueryRulesEngine.Hierarchyss.MetadataKeys.SyncApproverMetadataKeys
{
    public record SyncApproverMetadataKeysResponse(
        int HierarchyId,
        int MetadataRecordsAdded,
        int MetadataRecordsRemoved,
        List<string> AddedKeys,
        List<string> RemovedKeys
    );
}
