using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using TaskPilot.Application.Features.ProjectMembers.Dtos;
using TaskPilot.Application.Features.WorkspaceMembers.Dtos;
using TaskPilot.Application.Mappings;
using TaskPilot.Domain.Entities;

namespace TaskPilot.Application.Tests;

public class ApplicationMappingProfileTests
{
    [Fact]
    public void ApplicationMappingProfile_configuration_is_valid()
    {
        var configuration = CreateConfiguration();

        configuration.AssertConfigurationIsValid();
    }

    [Fact]
    public void Maps_workspace_member_email_from_user_navigation()
    {
        var mapper = CreateConfiguration().CreateMapper();
        var member = new WorkspaceMember
        {
            UserId = 10,
            Role = Role.Manager,
            JoinedAt = new DateTime(2026, 6, 26, 10, 0, 0, DateTimeKind.Utc),
            User = new User { Id = 10, Email = "manager@example.com" }
        };

        var response = mapper.Map<WorkspaceMemberResponse>(member);

        Assert.Equal(10, response.UserId);
        Assert.Equal("manager@example.com", response.Email);
        Assert.Equal(Role.Manager, response.Role);
        Assert.Equal(member.JoinedAt, response.JoinedAt);
    }

    [Fact]
    public void Maps_project_member_email_from_user_navigation()
    {
        var mapper = CreateConfiguration().CreateMapper();
        var member = new ProjectMember
        {
            UserId = 11,
            Role = ProjectRole.ProjectManager,
            JoinedAt = new DateTime(2026, 6, 26, 10, 0, 0, DateTimeKind.Utc),
            User = new User { Id = 11, Email = "pm@example.com" }
        };

        var response = mapper.Map<ProjectMemberResponse>(member);

        Assert.Equal(11, response.UserId);
        Assert.Equal("pm@example.com", response.Email);
        Assert.Equal(ProjectRole.ProjectManager, response.Role);
        Assert.Equal(member.JoinedAt, response.JoinedAt);
    }

    private static MapperConfiguration CreateConfiguration()
    {
        return new MapperConfiguration(
            configuration =>
            {
                configuration.AddProfile<ApplicationMappingProfile>();
            },
            NullLoggerFactory.Instance);
    }
}
