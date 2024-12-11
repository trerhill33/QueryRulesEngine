﻿using ApprovalHierarchyManager.Application.Features.ApprovalHierarchy.CreateHierarchy.Models;
using FluentValidation;
using QueryRulesEngine.Entities;
using QueryRulesEngine.Persistence;

namespace ApprovalHierarchyManager.Application.Features.ApprovalHierarchy.CreateHierarchy.Validators;

public sealed class CreateHierarchyValidator
    : AbstractValidator<CreateHierarchyRequest>
{
    private readonly IReadOnlyRepositoryAsync<int> _readOnlyRepository;

    public CreateHierarchyValidator(IReadOnlyRepositoryAsync<int> readOnlyRepository)
    {
        _readOnlyRepository = readOnlyRepository;

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
    {
        return !(await _readOnlyRepository.FindByPredicateAndTransformAsync<Hierarchy, bool>(
            h => h.Name == name,
            h => true,
            cancellationToken));
    }
}