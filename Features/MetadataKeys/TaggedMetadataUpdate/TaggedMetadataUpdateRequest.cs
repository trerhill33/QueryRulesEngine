namespace QueryRulesEngine.Features.MetadataKeys.TaggedMetadataUpdate;

public sealed record TaggedMetadataUpdateRequest
 (
    string ApproverId,
    string MetadataKey,
    string Tag,
    string Value    
);

