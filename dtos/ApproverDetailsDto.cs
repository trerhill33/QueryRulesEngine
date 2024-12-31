namespace QueryRulesEngine.dtos;

public sealed record ApproverDetailsDto
{
    public required string TMID { get; init; }
    public required string Name { get; init; }
    public required string Email { get; init; }
    public required string Title { get; init; }
    public required string JobCode { get; init; }
    public required List<ApproverMetadataDto> Metadata { get; init; } = new();
}
