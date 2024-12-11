using System.Collections.Generic;

public record AddMetadataKeyRequest
{
    public required int HierarchyId { get; init; }
    public required string KeyName { get; init; }
}
