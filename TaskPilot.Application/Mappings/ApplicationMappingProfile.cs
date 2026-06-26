using AutoMapper;
using TaskPilot.Application.Features.Auth.Dtos;
using TaskPilot.Application.Features.Comments.Dtos;
using TaskPilot.Application.Features.Labels.Dtos;
using TaskPilot.Application.Features.Notifications.Dtos;
using TaskPilot.Application.Features.Project.Dtos;
using TaskPilot.Application.Features.ProjectMembers.Dtos;
using TaskPilot.Application.Features.Tasks.Dtos;
using TaskPilot.Application.Features.Workspace.Dtos;
using TaskPilot.Application.Features.WorkspaceMembers.Dtos;
using TaskPilot.Domain.Entities;

namespace TaskPilot.Application.Mappings;

public sealed class ApplicationMappingProfile : Profile
{
    public ApplicationMappingProfile()
    {
        CreateMap<User, AuthUserResponse>();

        CreateMap<WorkSpace, WorkspaceResponse>();
        CreateMap<WorkSpace, WorkspaceListItemResponse>();
        CreateMap<WorkspaceMember, WorkspaceMemberResponse>()
            .ForCtorParam(
                nameof(WorkspaceMemberResponse.Email),
                options => options.MapFrom(member => member.User != null ? member.User.Email : string.Empty));

        CreateMap<Project, ProjectResponse>();
        CreateMap<Project, ProjectListItemResponse>();
        CreateMap<ProjectMember, ProjectMemberResponse>()
            .ForCtorParam(
                nameof(ProjectMemberResponse.Email),
                options => options.MapFrom(member => member.User != null ? member.User.Email : string.Empty));

        CreateMap<TaskItem, TaskResponse>();
        CreateMap<Comment, CommentResponse>();
        CreateMap<Label, LabelResponse>();
        CreateMap<Notification, NotificationResponse>();
    }
}
