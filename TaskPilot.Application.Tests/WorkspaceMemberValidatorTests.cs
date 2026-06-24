using TaskPilot.Application.Features.WorkspaceMembers.Dtos;
using TaskPilot.Application.Features.WorkspaceMembers.Validators;
using TaskPilot.Domain.Entities;

namespace TaskPilot.Application.Tests;

public class WorkspaceMemberValidatorTests
{
    [Fact]
    public void AddWorkspaceMemberRequestValidator_rejects_invalid_user_id()
    {
        var validator = new AddWorkspaceMemberRequestValidator();

        var result = validator.Validate(new AddWorkspaceMemberRequest(0, Role.Member));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(AddWorkspaceMemberRequest.UserId));
    }

    [Fact]
    public void AddWorkspaceMemberRequestValidator_rejects_invalid_role()
    {
        var validator = new AddWorkspaceMemberRequestValidator();

        var result = validator.Validate(new AddWorkspaceMemberRequest(5, (Role)999));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(AddWorkspaceMemberRequest.Role));
    }

    [Fact]
    public void AddWorkspaceMemberRequestValidator_accepts_valid_request()
    {
        var validator = new AddWorkspaceMemberRequestValidator();

        var result = validator.Validate(new AddWorkspaceMemberRequest(5, Role.Member));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void UpdateWorkspaceMemberRoleRequestValidator_rejects_invalid_role()
    {
        var validator = new UpdateWorkspaceMemberRoleRequestValidator();

        var result = validator.Validate(new UpdateWorkspaceMemberRoleRequest((Role)999));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(UpdateWorkspaceMemberRoleRequest.Role));
    }

    [Fact]
    public void UpdateWorkspaceMemberRoleRequestValidator_accepts_valid_role()
    {
        var validator = new UpdateWorkspaceMemberRoleRequestValidator();

        var result = validator.Validate(new UpdateWorkspaceMemberRoleRequest(Role.Owner));

        Assert.True(result.IsValid);
    }
}
