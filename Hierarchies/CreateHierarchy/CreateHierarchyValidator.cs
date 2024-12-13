using FluentValidation;
using QueryRulesEngine.Repositories.Interfaces;

namespace QueryRulesEngine.Hierarchies.CreateHierarchy;


public sealed class CreateHierarchyValidator : AbstractValidator<CreateHierarchyRequest>
{
    private readonly IHierarchyRepository _repository;

    public CreateHierarchyValidator(IHierarchyRepository repository)
    {
        _repository = repository;

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required")
            .MustAsync(BeUniqueName)
            .WithMessage("Hierarchy with this name already exists");

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .WithMessage("Description cannot exceed 500 characters");
    }

    private async Task<bool> BeUniqueName(string name, CancellationToken cancellationToken)
        => await _repository.IsUniqueHierarchyNameAsync(name, cancellationToken);
}