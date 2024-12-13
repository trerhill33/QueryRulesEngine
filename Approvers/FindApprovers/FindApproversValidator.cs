using FluentValidation;
using QueryRulesEngine.QueryEngine.Common.Models;
using QueryRulesEngine.Repositories.Interfaces;

namespace QueryRulesEngine.Approvers.FindApprovers
{
    public sealed class FindApproversValidator : AbstractValidator<FindApproversRequest>
    {
        private readonly IHierarchyRepository _hierarchyRepository;

        public FindApproversValidator(IHierarchyRepository hierarchyRepository)
        {
            _hierarchyRepository = hierarchyRepository;

            RuleFor(x => x.HierarchyId)
                .MustAsync(HierarchyExistsAsync)
                .WithMessage("Hierarchy does not exist");

            RuleFor(x => x.QueryMatrix)
                .NotNull()
                .WithMessage("Query matrix is required")
                .Must(HasValidOperator)
                .WithMessage("Query matrix must have a valid logical operator")
                .Must(HasValidFields)
                .WithMessage("Query matrix can only contain Employee fields");
        }

        private async Task<bool> HierarchyExistsAsync(int hierarchyId, CancellationToken cancellationToken)
            => await _hierarchyRepository.HierarchyExistsAsync(hierarchyId, cancellationToken);

        private bool HasValidOperator(QueryMatrix queryMatrix)
            => queryMatrix?.LogicalOperator != null;

        private bool HasValidFields(QueryMatrix queryMatrix)
        {
            if (queryMatrix == null) return false;

            // Check all conditions use Employee fields
            var allFields = GetAllFields(queryMatrix);
            return allFields.All(field => field.StartsWith("Employee."));
        }

        private List<string> GetAllFields(QueryMatrix queryMatrix)
        {
            var fields = new List<string>();

            // Get fields from conditions
            fields.AddRange(queryMatrix.Conditions.Select(c => c.Field));

            // Get fields from nested matrices
            foreach (var nested in queryMatrix.NestedMatrices)
            {
                fields.AddRange(GetAllFields(nested));
            }

            return fields;
        }
    }
}
