using System.Security.Cryptography;
using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Quartz.Impl.AdoJobStore;
using TaskPilot.Application.Authorization.Abstractions;
using TaskPilot.Application.Authorization.Enums;
using TaskPilot.Application.Features.Reports;
using TaskPilot.Application.Features.Notifications.Services;
using TaskPilot.Application.Features.Semantic.Contracts;
using TaskPilot.Domain.AI;
using TaskPilot.Domain.AI.Semantic;

namespace TaskPilot.Infrastructure.Features.Reports;

public sealed class QuartzReportScheduler(ISchedulerFactory schedulerFactory) : IReportScheduler
{
    private static JobKey GetJobKey(int projectId) => new($"weekly-report-{projectId}", "taskpilot-reports");
    private static TriggerKey GetTriggerKey(int projectId) => new($"weekly-report-{projectId}", "taskpilot-reports");

    public bool IsValidSchedule(string cron) => CronExpression.IsValidExpression(cron);

    public async Task ScheduleAsync(
        int projectId,
        int userId,
        string cron,
        string timeZoneId,
        CancellationToken cancellationToken)
    {
        var scheduler = await schedulerFactory.GetScheduler(cancellationToken);
        var jobKey = GetJobKey(projectId);
        var job = JobBuilder.Create<WeeklyReportJob>()
            .WithIdentity(jobKey)
            .UsingJobData(WeeklyReportJob.ProjectIdKey, projectId.ToString(CultureInfo.InvariantCulture))
            .UsingJobData(WeeklyReportJob.UserIdKey, userId.ToString(CultureInfo.InvariantCulture))
            .StoreDurably()
            .Build();
        await scheduler.AddJob(job, true, true, cancellationToken);

        var trigger = TriggerBuilder.Create()
            .WithIdentity(GetTriggerKey(projectId))
            .ForJob(jobKey)
            .WithCronSchedule(cron, schedule => schedule
                .InTimeZone(TimeZoneInfo.FindSystemTimeZoneById(timeZoneId))
                .WithMisfireHandlingInstructionFireAndProceed())
            .Build();
        var triggerKey = GetTriggerKey(projectId);
        if (await scheduler.CheckExists(triggerKey, cancellationToken))
            await scheduler.RescheduleJob(triggerKey, trigger, cancellationToken);
        else
            await scheduler.ScheduleJob(trigger, cancellationToken);
    }

    public async Task RemoveAsync(int projectId, CancellationToken cancellationToken)
    {
        var scheduler = await schedulerFactory.GetScheduler(cancellationToken);
        await scheduler.DeleteJob(GetJobKey(projectId), cancellationToken);
    }
}

[DisallowConcurrentExecution]
public sealed class WeeklyReportJob(IWeeklyReportService reports) : IJob
{
    public const string ProjectIdKey = "ProjectId";
    public const string UserIdKey = "UserId";

    public async Task Execute(IJobExecutionContext context)
    {
        var projectId = ReadPositiveInt(context.MergedJobDataMap, ProjectIdKey);
        var userId = ReadPositiveInt(context.MergedJobDataMap, UserIdKey);
        var sourceEventId = DeterministicGuid(context.FireInstanceId);
        var correlationId = Guid.NewGuid();
        var result = await reports.CreateScheduledAsync(
            projectId,
            userId,
            new CreateWeeklyReportRequest(sourceEventId, DateOnly.FromDateTime(context.FireTimeUtc.UtcDateTime)),
            correlationId,
            sourceEventId,
            context.CancellationToken);
        if (result.IsFail)
            throw new JobExecutionException(string.Join("; ", result.ErrorMessages ?? ["Scheduled report failed."]));
    }

    private static int ReadPositiveInt(JobDataMap data, string key) =>
        int.TryParse(data.GetString(key), NumberStyles.None, CultureInfo.InvariantCulture, out var value) && value > 0
            ? value
            : throw new JobExecutionException($"Scheduled report job data '{key}' is invalid.");

    private static Guid DeterministicGuid(string value)
    {
        Span<byte> hash = stackalloc byte[32];
        SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value), hash);
        return new Guid(hash[..16]);
    }
}

[DisallowConcurrentExecution]
[PersistJobDataAfterExecution]
public sealed class SemanticBackfillJob(ISemanticBackfillService backfill) : IJob
{
    private const string SourceTypeKey = "SourceType";
    private const string LastSourceIdKey = "LastSourceId";
    private const string IsCompleteKey = "IsComplete";

    public async Task Execute(IJobExecutionContext context)
    {
        var data = context.JobDetail.JobDataMap;
        if (!Enum.TryParse<SemanticSourceType>(data.GetString(SourceTypeKey), true, out var sourceType) ||
            !Enum.IsDefined(sourceType))
            throw new JobExecutionException("Semantic backfill source type is invalid.");
        if (!int.TryParse(data.GetString(LastSourceIdKey), NumberStyles.None, CultureInfo.InvariantCulture, out var lastSourceId) ||
            lastSourceId < 0)
            throw new JobExecutionException("Semantic backfill checkpoint is invalid.");
        if (!bool.TryParse(data.GetString(IsCompleteKey), out var isComplete))
            throw new JobExecutionException("Semantic backfill completion state is invalid.");
        var checkpoint = new SemanticBackfillCheckpoint(
            sourceType,
            lastSourceId,
            isComplete);
        var next = await backfill.RunAsync(checkpoint, 100, context.CancellationToken);

        if (next.IsComplete && sourceType < SemanticSourceType.Note)
            next = new SemanticBackfillCheckpoint(sourceType + 1, 0, false);

        data[SourceTypeKey] = next.SourceType.ToString();
        data[LastSourceIdKey] = next.LastSourceId.ToString(CultureInfo.InvariantCulture);
        data[IsCompleteKey] = next.IsComplete.ToString(CultureInfo.InvariantCulture);
        if (next.IsComplete && next.SourceType == SemanticSourceType.Note)
            await context.Scheduler.UnscheduleJob(context.Trigger.Key, context.CancellationToken);
    }
}

[DisallowConcurrentExecution]
public sealed class DeadlineReminderJob(
    IDeadlineReminderService reminders,
    IConfiguration configuration) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        var hours = Math.Clamp(configuration.GetValue("Notifications:DeadlineReminderWindowHours", 24), 1, 168);
        await reminders.CreateDueSoonNotificationsAsync(TimeSpan.FromHours(hours), context.CancellationToken);
    }
}

public sealed class ReportAuthorizationPort(IAccessControlService accessControl) : IReportAuthorizationPort
{
    public async Task<AgentExecutionScope?> AuthorizeProjectAsync(
        int projectId,
        bool requireManage,
        CancellationToken cancellationToken)
    {
        var access = await accessControl.AuthorizeProjectAsync(
            projectId,
            requireManage ? ProjectAccessLevel.Manage : ProjectAccessLevel.Read,
            true,
            cancellationToken);
        return access.Failure is null
            ? new AgentExecutionScope(access.CurrentUserId, access.Workspace.Id, projectId, Guid.NewGuid(), Guid.NewGuid())
            : null;
    }
}

public static class ReportInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddReportInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PostgreSql")
                               ?? throw new InvalidOperationException("PostgreSql connection string is missing.");
        services.AddQuartz(quartz =>
        {
            quartz.SchedulerId = "AUTO";
            quartz.SchedulerName = "TaskPilot";
            quartz.UsePersistentStore(store =>
            {
                store.UseProperties = true;
                store.PerformSchemaValidation = true;
                store.UseSystemTextJsonSerializer();
                store.UseGenericDatabase<PostgreSQLDelegate>("Npgsql", provider =>
                {
                    provider.ConnectionString = connectionString;
                    provider.TablePrefix = "qrtz_";
                });
                store.UseClustering(cluster =>
                {
                    cluster.CheckinInterval = TimeSpan.FromSeconds(10);
                    cluster.CheckinMisfireThreshold = TimeSpan.FromSeconds(20);
                });
            });

            if (configuration.GetValue("Semantic:BackfillEnabled", false))
            {
                var jobKey = new JobKey("semantic-backfill", "taskpilot-maintenance");
                quartz.AddJob<SemanticBackfillJob>(options => options
                    .WithIdentity(jobKey)
                    .UsingJobData("SourceType", SemanticSourceType.Task.ToString())
                    .UsingJobData("LastSourceId", "0")
                    .UsingJobData("IsComplete", bool.FalseString)
                    .StoreDurably());
                quartz.AddTrigger(trigger => trigger
                    .WithIdentity("semantic-backfill", "taskpilot-maintenance")
                    .ForJob(jobKey)
                    .StartNow()
                    .WithSimpleSchedule(schedule => schedule
                        .WithInterval(TimeSpan.FromSeconds(5))
                        .RepeatForever()
                        .WithMisfireHandlingInstructionNextWithExistingCount()));
            }


            if (configuration.GetValue("Notifications:DeadlineRemindersEnabled", true))
            {
                var reminderJobKey = new JobKey("deadline-reminders", "taskpilot-notifications");
                quartz.AddJob<DeadlineReminderJob>(options => options
                    .WithIdentity(reminderJobKey)
                    .RequestRecovery()
                    .StoreDurably());
                quartz.AddTrigger(trigger => trigger
                    .WithIdentity("deadline-reminders", "taskpilot-notifications")
                    .ForJob(reminderJobKey)
                    .StartNow()
                    .WithSimpleSchedule(schedule => schedule
                        .WithInterval(TimeSpan.FromMinutes(Math.Clamp(
                            configuration.GetValue("Notifications:DeadlineReminderIntervalMinutes", 15),
                            1,
                            1_440)))
                        .RepeatForever()
                        .WithMisfireHandlingInstructionNextWithRemainingCount()));
            }
        });
        services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);
        services.AddScoped<IReportScheduler, QuartzReportScheduler>();
        services.AddScoped<IReportAuthorizationPort, ReportAuthorizationPort>();
        return services;
    }
}
