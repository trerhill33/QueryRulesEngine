using System.Collections.Generic;

public record AddApproverMetadataKeyResponse
{
    public required int HierarchyId { get; init; }
    public required string KeyName { get; init; }
}

