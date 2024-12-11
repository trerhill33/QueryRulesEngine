using System.ComponentModel.DataAnnotations;

namespace ApprovalHierarchyManager.Application.Features.Employee;

public record EmployeeDto
{
    public int Id { get; init; }
    public string TMID { get; init; }
    public string Name { get; init; }
    public string Email { get; init; }
    public string Title { get; init; }
    public string JobCode { get; init; }

    // Location Information
    public string LocationName { get; init; }
    public string LocationId { get; init; }
    public string LocationAddress1 { get; init; }
    public string LocationAddress2 { get; init; }
    public string LocationCity { get; init; }
    public string LocationState { get; init; }
    public int? LocationZip { get; init; }
    public string LocationPhoneAreaCode { get; init; }
    public string LocationPhone { get; init; }

    // Manager Information
    public int? ReportsToWorkerId { get; init; }
    public string ReportsToPositionId { get; init; }
    public string ReportsToJobCode { get; init; }
    public string ReportsToJobProfile { get; init; }
}


public record UpdateEmployeeRequest
{
    [Required]
    public string Name { get; init; }

    [Required]
    [EmailAddress]
    public string Email { get; init; }

    [Required]
    public string Title { get; init; }

    [Required]
    public string JobCode { get; init; }

    // Location Information
    public string LocationName { get; init; }
    public string LocationId { get; init; }
    public string LocationAddress1 { get; init; }
    public string LocationAddress2 { get; init; }
    public string LocationCity { get; init; }
    public string LocationState { get; init; }
    public int? LocationZip { get; init; }
    public string LocationPhoneAreaCode { get; init; }
    public string LocationPhone { get; init; }

    // Manager Information
    public int? ReportsToWorkerId { get; init; }
    public string ReportsToPositionId { get; init; }
    public string ReportsToJobCode { get; init; }
    public string ReportsToJobProfile { get; init; }
}

public record GetEmployeeResponse
{
    public bool Succeeded { get; init; }
    public string Message { get; init; }
    public EmployeeDto Data { get; init; }
}

public record GetEmployeesResponse
{
    public bool Succeeded { get; init; }
    public string Message { get; init; }
    public List<EmployeeDto> Data { get; init; }
}