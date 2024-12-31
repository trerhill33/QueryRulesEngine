namespace QueryRulesEngine.dtos;

public sealed record ApproverMetaDto
{
    public required string MetaDescription { get; init; }
    public required string MetaValue { get; init; }
}
