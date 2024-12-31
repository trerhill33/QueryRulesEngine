namespace QueryRulesEngine.dtos;

public sealed record ApproversDto
{
    public required List<ApproverDetailsDto> ApprovalLevel1 { get; init; } = [];
    public required List<ApproverDetailsDto> ApprovalLevel2 { get; init; } = [];
}
