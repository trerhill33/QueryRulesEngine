namespace QueryRulesEngine.Approvers.CreateApprovers
{
    public sealed record CreateApproversResponse(
        int HierarchyId,
        int ApproversCreated,
        List<string> CreatedApproverTMIds  // Successfully created approvers
    );
}
