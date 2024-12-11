using System;

namespace ApprovalHierarchyManager.Application.Features.ApprovalHierarchy.CreateHierarchy.Models;

public record CreateHierarchyResponse
{
    public int Id { get; init; }
    public string Name { get; init; }
    public string Description { get; init; }
}