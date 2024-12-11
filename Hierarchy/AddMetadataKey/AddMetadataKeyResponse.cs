using System.Collections.Generic;

public record AddMetadataKeyResponse
{
    public required int HierarchyId { get; init; }
    public required string KeyName { get; init; }
}

