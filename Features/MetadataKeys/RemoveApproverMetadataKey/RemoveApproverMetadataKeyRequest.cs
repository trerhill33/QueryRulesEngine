namespace QueryRulesEngine.Features.MetadataKeys.RemoveApproverMetadataKey;

public record RemoveApproverMetadataKeyRequest
(        
    int HierarchyId,
    string KeyName 
);
