using QueryRulesEngine.dtos;
using System.Collections.Generic;

public record GetHierarchyDetailsResponse
{
    public int Id { get; init; }
    public string Name { get; init; }
    public string Description { get; init; }
    public List<MetadataKeyDto> MetadataKeys { get; init; } = new();
}
