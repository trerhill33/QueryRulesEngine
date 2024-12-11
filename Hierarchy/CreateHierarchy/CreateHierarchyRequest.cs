namespace ApprovalHierarchyManager.Application.Features.ApprovalHierarchy.CreateHierarchy.Models;

public record CreateHierarchyRequest
{
    public string Name { get; init; }
    public string Description { get; init; }
}