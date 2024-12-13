using QueryRulesEngine.QueryEngine.Common.Models;

namespace QueryRulesEngine.Features.Approvers.FindApprovers

{
    public record FindApproversRequest(
    int HierarchyId,
    QueryMatrix QueryMatrix
    );
}
